using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver;

namespace server
{
    class Program
    {
        private static MongoClient? mongoClient;
        private static IMongoDatabase? database;
        //private static List<Socket>? sockets;
        
        //Clienter
        static List<Socket> sockets = new List<Socket>();
        //Server
        static Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //static Socket serverSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);


    static void Main(string[] args)
        { //mongoClient för servern och database för namnet i compass
            mongoClient = new MongoClient("mongodb://localhost:27017");
            database = mongoClient.GetDatabase("mongoTest");
        var usersCollection = database.GetCollection<UserModel>("users");
        UserModel newUser = new UserModel {Username = " gus ", Password = " grg "};
        usersCollection.InsertOne(newUser);
        // Hämta eller skapa en samling för användare
        
        var filter = Builders<UserModel>.Filter.Empty;
        List<UserModel> allUsers = usersCollection.Find(filter).ToList();
        
        

        foreach (UserModel user in allUsers)
        {
            Console.WriteLine(user.Username);
        }

        SetupServer();
        AcceptClients();
    }

    static void SetupServer()
    {
        //IPAddress ipAddress = IPAddress.Loopback; //lättare att läsa
        IPAddress ipAddress = new IPAddress(new byte[] { 127, 0, 0, 1 });
        IPEndPoint iPEndPoint = new IPEndPoint(ipAddress, 25500);

            //Socket serverSocket = new Socket(
            //    ipAddress.AddressFamily,
            //    SocketType.Stream,
            //    ProtocolType.Tcp
            //);

            serverSocket.Bind(iPEndPoint);
            serverSocket.Listen(5);

            // tråd för att hantera inputs från server-konsollen.
            Thread consoleThread = new Thread(ConsoleInputThread);
            consoleThread.Start();

            // updaterar LoggedIn på samtliga användare till false vid uppstart av servern
            // detta istället för att hantera samma sak vid Ctrl+c/användare stänger ner programmet/programmet stänger av sig pga ett fel
            var updateAll = Builders<UserModel>.Update.Set(u => u.LoggedIn, false);
            database?
                .GetCollection<UserModel>("users")
                .UpdateMany(Builders<UserModel>.Filter.Empty, updateAll);

            Console.WriteLine("Server is running on a dedicated Linux " + iPEndPoint + ", With 100% Power.");
    }
    static void AcceptClients()
    {
        while (true)
            {
                if (serverSocket.Poll(0, SelectMode.SelectRead))
                {
                    Socket client = serverSocket.Accept();
                    //Console.WriteLine("A client has connected!" );
                    sockets.Add(client);
                    foreach (Socket otherClient in sockets.Where(c => c != client))
                    {
                    BroadcastNotification(" A new client has connected! from " + client.RemoteEndPoint);                                        
                    }
                    //client.Send(System.Text.Encoding.UTF8.GetBytes(" Connection established, Welcome to the awesome server, powered by Linux! "));
                    BroadcastNotification(" Connection established, Welcome to the awesome server, powered by Linux! ");
                try
                {
                    byte[] incoming = new byte[5000];
                    int read = client.Receive(incoming);

                    if (read == 0)
                    {
                        BroadcastNotification("A client has disconnected" + client.RemoteEndPoint);
                    }
                    else
                    {
                        string message = System.Text.Encoding.UTF8.GetString(incoming, 0, read);
                        Console.WriteLine("From Client " + client.RemoteEndPoint + "\n" + message );
                    }                
                } catch(SocketException ex)
                {
                    Console.WriteLine("Exception"+ ex);
                }
                }
                foreach (Socket client in sockets)
                {
                    if (client.Poll(0, SelectMode.SelectRead))
                    {
                        byte[] incoming = new byte[5000];
                        int read = client.Receive(incoming);
                        string message = System.Text.Encoding.UTF8.GetString(incoming, 0, read);

                        if (message.StartsWith("login: "))
                        {
                            string[] credentials = message.Substring(6).Split(':');
                            string username = credentials[0];
                            string password = credentials[1];

                            if (ValidateCredentials(username, password))
                            {
                                client.Send(System.Text.Encoding.UTF8.GetBytes("Login Success!"));

                                Thread clientThread = new Thread(
                                    () => HandleLoggedInClient(client, username)
                                );
                                clientThread.Start();

                                // TODO: Sätt LoggedIn = true
                            }
                            else
                            { // TODO: FELHANTERINGS METOD
                                //client.Send(System.Text.Encoding.UTF8.GetBytes("Login Failed!"));
                                handleInlogException(client, username, password);
                                // TODO: Hantera responsen på klientsidan så att den läser detta meddelande.
                            }
                        }
                    }
                }
            }
        }

        static void ConsoleInputThread()
        {
            while (true)
            {
                
                Console.WriteLine("\nCommands available: userlist , endserver :"); // lägg till mer commands efter hand
                string? consoleInput = Console.ReadLine();

                // logik för console Input
                if (consoleInput == "userlist")
                {
                    PrintAllUsers();
                }
                else if (consoleInput == "endserver") 
                {
                    Console.WriteLine("Server is shuting down...");
                    Thread.Sleep(1000);
                    Console.Clear();
                    Environment.Exit(0);
                    break;
                }
                
                // Låt tråden sova en kort stund för att undvika onödig processorkonsumtion
                Thread.Sleep(100);
            }
        }

        static void HandleLoggedInClient(Socket client, string username)
        {
            string user = username;
            Console.WriteLine($"User {user} logged in!");

            foreach (Socket otherClient in sockets.Where(c => c != client))
            {
                try
                {
                    otherClient.Send(System.Text.Encoding.UTF8.GetBytes($"User {user} logged in! from " + otherClient.RemoteEndPoint));
                }
                catch (SocketException)
                {
                    
                    Console.WriteLine
                    (
                        $"Failed to send login message to {otherClient.RemoteEndPoint}"
                    );
                }
                
            }

            while (true)
            {
                byte[] incoming = new byte[5000];
                int read = client.Receive(incoming);
                string message = System.Text.Encoding.UTF8.GetString(incoming, 0, read);
                if (message == "logout")
                {
                    handleLogout(user);
                    Console.WriteLine($"User {user} logged out! from {client.RemoteEndPoint} ");
                    Console.WriteLine("Server is shuting down..."); // test
                    Thread.Sleep(1000);
                    Console.Clear();
                    Environment.Exit(0); // test
                    break;
                }
                else
                {
                    Console.WriteLine($"{username}: {message}");
                }
            }
        }

        static void PrintAllUsers()
        {
            var filter = Builders<UserModel>.Filter.Empty;
            List<UserModel> allUsers = database!
                .GetCollection<UserModel>("users")
                .Find(filter)
                .ToList();

            Console.WriteLine($"({allUsers.Count}) Users in database:");

            foreach (UserModel? user in allUsers)
            {
                if (user != null)
                {
                    Console.WriteLine("--------------------------");
                    Console.WriteLine(
                        $"Username: {user.Username}\nPassword: {user.Password}\nCurrently logged in: {(user.LoggedIn ? "Yes" : "No")}"
                    );
                }
            }
        }

        static bool ValidateCredentials(string username, string password)
        {
            var filter =
                Builders<UserModel>.Filter.Eq(u => u.Username, username)
                & Builders<UserModel>.Filter.Eq(u => u.Password, password)
                // Se till att användaren inte redan är inloggad
                & Builders<UserModel>.Filter.Eq(u => u.LoggedIn, false);
            // TODO: gör collectionen global > uppdatera nästa rad
            var user = database?.GetCollection<UserModel>("users").Find(filter).FirstOrDefault();

            if (user != null)
            {
                // updaterar databasen med att användaren är inloggad
                var update = Builders<UserModel>.Update.Set(v => v.LoggedIn, true);
                // TODO: gör collectionen global > uppdatera nästa rad
                database?.GetCollection<UserModel>("users").UpdateOne(filter, update);
            }

            return user != null;
        }

        static void handleLogout(string username)
        {
            var filter = Builders<UserModel>.Filter.Eq(u => u.Username, username);
            var update = Builders<UserModel>.Update.Set(v => v.LoggedIn, false);
            database?.GetCollection<UserModel>("users").UpdateOne(filter, update);

            Console.WriteLine($"User {username} logged out.");
        }
    
    static void BroadcastNotification(string message)
    {
        foreach (Socket client in sockets)
        {
            byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
            client.Send(messageBytes);
        }
    }

       public static void handleInlogException(Socket client, string username, string password) 
    {
        Console.WriteLine($"Login failed for user {username} with password {password}");
        client.Send(System.Text.Encoding.UTF8.GetBytes("Login Failed!"));
    }
    
    }
   }
              

    class UserModel
    {
        public ObjectId _id { get; set; }
        public bool LoggedIn { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

