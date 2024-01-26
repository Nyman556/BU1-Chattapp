using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver;

namespace server
{
    class Program
    {
        private static MongoClient? mongoClient;
        private static IMongoDatabase? database;
        private static List<Socket>? sockets;

        static void Main(string[] args)
        {
            mongoClient = new MongoClient("mongodb://localhost:27017");
            database = mongoClient.GetDatabase("mongoTest");

            // Hämta eller skapa en samling för användare
            var usersCollection = database.GetCollection<UserModel>("users");

            var filter = Builders<UserModel>.Filter.Empty;
            List<UserModel> allUsers = usersCollection.Find(filter).ToList();

            sockets = new List<Socket>();
            IPAddress ipAddress = new IPAddress(new byte[] { 127, 0, 0, 1 });
            IPEndPoint iPEndPoint = new IPEndPoint(ipAddress, 25500);

            Socket serverSocket = new Socket(
                ipAddress.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp
            );

            serverSocket.Bind(iPEndPoint);
            serverSocket.Listen(5);

            // tråd för att hantera inputs från server-konsollen.
            Thread consoleThread = new Thread(ConsoleInputThread);
            consoleThread.Start();

            while (true)
            {
                if (serverSocket.Poll(0, SelectMode.SelectRead))
                {
                    Socket client = serverSocket.Accept();
                    Console.WriteLine("A client has connected!");
                    sockets.Add(client);
                }

                foreach (Socket client in sockets)
                {
                    if (client.Poll(0, SelectMode.SelectRead))
                    {
                        byte[] incoming = new byte[5000];
                        int read = client.Receive(incoming);
                        string message = System.Text.Encoding.UTF8.GetString(incoming, 0, read);

                        if (message.StartsWith("login:"))
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
                            }
                            else
                            {
                                client.Send(System.Text.Encoding.UTF8.GetBytes("Login Failed!"));
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
                Console.WriteLine("Commands available: userlist"); // lägg till mer commands efter hand
                string? consoleInput = Console.ReadLine();

                // logik för console Input
                if (consoleInput == "userlist")
                {
                    PrintAllUsers();
                }
                // Låt tråden sova en kort stund för att undvika onödig processorkonsumtion
                Thread.Sleep(100);
            }
        }

        static void HandleLoggedInClient(Socket client, string username)
        {
            Console.WriteLine($"User {username} logged in!");

            foreach (Socket otherClient in sockets.Where(c => c != client))
            {
                try
                {
                    otherClient.Send(
                        System.Text.Encoding.UTF8.GetBytes($"User {username} logged in!")
                    );
                }
                catch (SocketException)
                {
                    Console.WriteLine(
                        $"Failed to send login message to {otherClient.RemoteEndPoint}"
                    );
                }
            }

            while (true)
            {
                byte[] incoming = new byte[5000];
                int read = client.Receive(incoming);
                string message = System.Text.Encoding.UTF8.GetString(incoming, 0, read);

                Console.WriteLine($"{username}: {message}");
            }
        }

        static void PrintAllUsers()
        {
            var filter = Builders<UserModel>.Filter.Empty;
            List<UserModel> allUsers = database
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
                        $"Username: {user.Username}\nPassword: {user.Password}\nCurrently logged in: {(user.loggedIn ? "Yes" : "No")}"
                    );
                }
            }
        }

        static bool ValidateCredentials(string username, string password)
        {
            var filter =
                Builders<UserModel>.Filter.Eq(u => u.Username, username)
                & Builders<UserModel>.Filter.Eq(u => u.Password, password);
            var user = database.GetCollection<UserModel>("users").Find(filter).FirstOrDefault();

            return user != null;
        }
    }

    class UserModel
    {
        public ObjectId _id { get; set; }
        public bool loggedIn { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
