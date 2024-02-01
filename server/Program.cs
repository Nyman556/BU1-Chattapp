﻿using System;
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
    class Server
    {
        // databas
        private MongoClient mongoClient;
        private IMongoDatabase database;
        private IMongoCollection<UserModel> userCollection;

        // serverSocket
        private Socket serverSocket;

        // lista med clienter
        private List<Client> clients;

        // intern data
        private List<UserModel>? allUsers;

        public Server()
        {
            mongoClient = new MongoClient("mongodb://localhost:27017");
            database = mongoClient.GetDatabase("mongoTest");
            userCollection = database.GetCollection<UserModel>("users");
            clients = new List<Client>();
            allUsers = new List<UserModel>();
            serverSocket = CreateServerSocket();
        }

        private Socket CreateServerSocket()
        {
            IPAddress ipAddress = new IPAddress(new byte[] { 127, 0, 0, 1 });
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 25500);

            Socket socket = new Socket(
                ipAddress.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp
            );

            socket.Bind(ipEndPoint);
            socket.Listen(5);

            return socket;
        }

        public void Start()
        {
            Initialize();

            // startar tråd för att hantera inputs från server-konsollen.
            Thread consoleThread = new Thread(ConsoleInputThread);
            consoleThread.Start();

            // Server loop
            while (true)
            {
                if (serverSocket.Poll(0, SelectMode.SelectRead))
                {
                    AcceptNewClients();
                }
            }
        }

        private void Initialize()
        {
            // uppstarts-logik
            FetchUserData();

            // updaterar LoggedIn på samtliga användare till false
            // detta istället för att hantera samma sak vid Ctrl+c/användare stänger ner programmet/programmet stänger av sig pga ett fel
            var updateAll = Builders<UserModel>.Update.Set(u => u.LoggedIn, false);
            database
                ?.GetCollection<UserModel>("users")
                .UpdateMany(Builders<UserModel>.Filter.Empty, updateAll);
        }

        private void AcceptNewClients()
        {
            Socket clientSocket = serverSocket.Accept();
            Console.WriteLine("A client has connected!");
            var client = new Client(clientSocket, this);
            clients.Add(client);

            Thread clientTread = new Thread(client.Start);
            clientTread.Start();
        }

        public bool validateMessage(string message, byte length)
        {
            string[] splitMessage = message.Split(':');
            return splitMessage.Length == length;
        }

        public bool ValidateCredentials(string username, string password)
        {
            var filter =
                Builders<UserModel>.Filter.Eq(u => u.Username, username)
                & Builders<UserModel>.Filter.Eq(u => u.Password, password)
                // Se till att användaren inte redan är inloggad
                & Builders<UserModel>.Filter.Eq(u => u.LoggedIn, false);
            var user = userCollection.Find(filter).FirstOrDefault();

            if (user != null)
            {
                // updaterar databasen med att användaren är inloggad
                var update = Builders<UserModel>.Update.Set(v => v.LoggedIn, true);
                userCollection.UpdateOne(filter, update);
            }
            return user != null;
        }

        public void HandleLogout(string username)
        {
            var filter = Builders<UserModel>.Filter.Eq(u => u.Username, username);
            var update = Builders<UserModel>.Update.Set(v => v.LoggedIn, false);
            userCollection.UpdateOne(filter, update);
        }

        public void CreateNewUser(string username, string password)
        {
            UserModel newUser = new UserModel
            {
                Username = username,
                Password = password,
                LoggedIn = false
            };
            userCollection.InsertOne(newUser);
            // uppdaterar den globala userList med den nya användaren
            FetchUserData();
        }

        public bool CheckTaken(string username)
        {
            var filter = Builders<UserModel>.Filter.Eq(u => u.Username, username);
            var user = userCollection.Find(filter).Any();

            return user;
        }

        private void ConsoleInputThread()
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

        private void PrintAllUsers()
        {
            Console.WriteLine($"({allUsers!.Count}) Users in database:");

            foreach (UserModel user in allUsers)
            {
                Console.WriteLine("--------------------------");
                Console.WriteLine(
                    $"Username: {user.Username}\nPassword: {user.Password}\nCurrently logged in: {(user.LoggedIn ? "Yes" : "No")}"
                );
            }
        }

        private List<UserModel> FetchUserData()
        {
            var filter = Builders<UserModel>.Filter.Empty;
            return allUsers = userCollection.Find(filter).ToList();
        }
    }

    class Client
    {
        private Socket clientSocket;
        private Server chatServer;
        private string? username;
        private bool _LoggedIn = false;

        public Client(Socket socket, Server server)
        {
            clientSocket = socket;
            chatServer = server;
        }

        public void Start()
        {
            HandleLogin();
            if (_LoggedIn)
            {
                HandleMessages();
            }
        }

        public void HandleMessages()
        {
            try
            {
                while (_LoggedIn)
                {
                    byte[] incoming = new byte[5000];
                    int read = clientSocket.Receive(incoming);
                    string message = System.Text.Encoding.UTF8.GetString(incoming, 0, read);

                    if (string.IsNullOrEmpty(message))
                    {
                        break;
                    }

                    if (message == "logout")
                    {
                        if (username != null)
                        {
                            // TODO: fixa så att detta hanteras på samma sätt som socketExceptionen nedanför
                            HandleLogout(username);
                            Console.WriteLine($"User {username} logged out.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"{username}: {message}");
                    }
                }
                return;
            }
            catch (SocketException)
            {
                Console.WriteLine($"User {username} disconnected.");
            }
            finally
            {
                _LoggedIn = false;
                HandleLogout(username!);
                clientSocket.Close();
            }
        }

        private void HandleLogin()
        { try
            {
           while (!_LoggedIn)
            {
                byte[] incoming = new byte[5000];
                int read = clientSocket.Receive(incoming);
                string message = System.Text.Encoding.UTF8.GetString(incoming, 0, read);
                if (chatServer.validateMessage(message, 3))
                {
                    if (message.StartsWith("login"))
                    {
                        string[] credentials = message.Substring(6).Split(':');
                        username = credentials[0];
                        string password = credentials[1];

                        if (chatServer.ValidateCredentials(username, password))
                        {
                            clientSocket.Send(System.Text.Encoding.UTF8.GetBytes("Login Success!"));
                            Console.WriteLine($"{username} logged in!");
                            _LoggedIn = true;
                        }
                        else
                        {
                            clientSocket.Send(System.Text.Encoding.UTF8.GetBytes("Login Failed!"));
                        }
                    }
                    else if (message.StartsWith("new"))
                    {
                        string[] newUserData = message.Substring(4).Split(':');
                        string newUsername = newUserData[0];
                        string newPassword = newUserData[1];

                        if (CheckTaken(newUsername))
                        {
                            clientSocket.Send(
                                System.Text.Encoding.UTF8.GetBytes("username already taken!")
                            );
                        }
                        else
                        {
                            CreateNewUser(newUsername, newPassword);
                            clientSocket.Send(
                                System.Text.Encoding.UTF8.GetBytes("new user created!")
                            );
                        }
                    }
                }
                else
                {
                    clientSocket.Send(System.Text.Encoding.UTF8.GetBytes("Wrong format!"));
                }
            }
            }
            catch (SocketException)
            {
                Console.WriteLine($"Client disconnected.");
                clientSocket.Close();
            }
        }

        private void HandleLogout(string username)
        {
            chatServer.HandleLogout(username);
        }

        private void CreateNewUser(string username, string password)
        {
            chatServer.CreateNewUser(username, password);
        }

        private bool CheckTaken(string username)
        {
            bool taken = chatServer.CheckTaken(username);
            return taken;
        }
    }

    public class UserModel
    {
        public ObjectId _id { get; set; }
        public bool LoggedIn { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public List<PrivateLog>? Log { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var server = new Server();
            server.Start();
        }
    }
}
