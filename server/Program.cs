﻿using System;
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

            // updaterar LoggedIn på samtliga användare till false vid uppstart av servern
            // detta istället för att hantera samma sak vid Ctrl+c/användare stänger ner programmet/programmet stänger av sig pga ett fel
            var updateAll = Builders<UserModel>.Update.Set(u => u.LoggedIn, false);
            database
                .GetCollection<UserModel>("users")
                .UpdateMany(Builders<UserModel>.Filter.Empty, updateAll);

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

                                // TODO: Sätt LoggedIn = true
                            
                            }
                            else
                            {
                                client.Send(System.Text.Encoding.UTF8.GetBytes("Login Failed!"));

                                // TODO: Hantera responsen på klientsidan så att den läser detta meddelande.
                            }
                        } else if (message.StartsWith("new:")) {
                            string[] newUserData = message.Substring(4).Split(':');
                            string newUsername = newUserData[0];
                            string newPassword = newUserData[1];

                                if (CheckTaken(newUsername)){

                                    client.Send(System.Text.Encoding.UTF8.GetBytes("username already taken!"));
                                } 
                                else
                                {
                                    CreateNewUser(newUsername, newPassword);
                                    client.Send(System.Text.Encoding.UTF8.GetBytes("new user created!"));
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
            string user = username;
            Console.WriteLine($"User {user} logged in!");

            foreach (Socket otherClient in sockets.Where(c => c != client))
            {
                try
                {
                    otherClient.Send(System.Text.Encoding.UTF8.GetBytes($"User {user} logged in!"));
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
                if (message == "logout")
                {
                    handleLogout(user);
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
            var user = database.GetCollection<UserModel>("users").Find(filter).FirstOrDefault();

            if (user != null)
            {
                // updaterar databasen med att användaren är inloggad
                var update = Builders<UserModel>.Update.Set(v => v.LoggedIn, true);
                // TODO: gör collectionen global > uppdatera nästa rad
                database.GetCollection<UserModel>("users").UpdateOne(filter, update);
            }

            return user != null;
        }

        static void handleLogout(string username)
        {
            var filter = Builders<UserModel>.Filter.Eq(u => u.Username, username);
            var update = Builders<UserModel>.Update.Set(v => v.LoggedIn, false);
            database.GetCollection<UserModel>("users").UpdateOne(filter, update);

            Console.WriteLine($"User {username} logged out.");
        }


        // Metod för att skapa ny användare.
        static void CreateNewUser(string username, string password) {
            var usersCollection = database.GetCollection<UserModel>("users");
            UserModel newUser = new UserModel {Username = username, Password = password, LoggedIn = false};
            usersCollection.InsertOne(newUser);
        }

        static bool CheckTaken(string username) {
            var filter = Builders<UserModel>.Filter.Eq(u => u.Username, username);
            var user = database.GetCollection<UserModel>("users").Find(filter).Any();
          
          return user;

        }



    }

    class UserModel
    {
        public ObjectId _id { get; set; }
        public bool LoggedIn { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}

   