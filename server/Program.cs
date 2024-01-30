using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using MongoDB.Bson;
using MongoDB.Driver;

namespace server
{
    class Program
    {

        // Man ska kunna skriva tex "private <user> <message> för att skicka privata meddelanden
        static void Main(string[] args)
        {
            IPAddress ipAddress = new IPAddress(new byte[] { 127, 0, 0, 1 });
            IPEndPoint iPEndPoint = new IPEndPoint(ipAddress, 25500);

            Socket serverSocket = new Socket(
                ipAddress.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp
            );

            serverSocket.Bind(iPEndPoint);
            serverSocket.Listen(5);

            MongoClient mongoClient = new MongoClient("mongodb://localhost:27017");
            IMongoDatabase database = mongoClient.GetDatabase("mongoTest");

            var usersCollection = database.GetCollection<UserModel>("users");

            List<Socket> sockets = new List<Socket>();

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
                    // Blockar inte koden om det inte finns något att läsa.
                    if (client.Poll(0, SelectMode.SelectRead))
                    {
                        byte[] incoming = new byte[5000];
                        int read = client.Receive(incoming);
                        string message = System.Text.Encoding.UTF8.GetString(incoming, 0, read);
                        Console.WriteLine("From a client: " + message);
                    }
                }

                SendMessageCommand(usersCollection);
            }
        }

        static void SendMessageCommand(IMongoCollection<UserModel> usersCollection) 
        {                                                // Kommando <mottagare> <meddelandet>
            Console.WriteLine("To send messages privately: 'private <user> <message>'");
            string? command = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(command)) 
            {
                Console.WriteLine("Invalid command. Please try again.");
                return;
            }

            string[] splitParts = command.Split(' ');

            if (splitParts.Length < 3 || splitParts[0] != "private") 
            {
                Console.WriteLine("Invalid command. Please use the format previously displayed.");
                return;
            }

            string? receiverName = splitParts[1];

            UserModel ? receiver = FindUser(usersCollection, receiverName);
            if (receiver == null) 
            {
                Console.WriteLine($"User: '{receiverName}' not found.");
                return;
            }

            string? message = string.Join(' ', splitParts[2..]);

            // Ändra till den som är "inloggad" istället för hårdkodad "Gud" som sender.
            string? senderName = "Server";
            UserModel ? sender = FindUser(usersCollection, senderName);
            if (sender == null) 
            {
                Console.WriteLine($"User: '{senderName}' not found.");
                return;
            }

            SendMessage(sender, receiver, message);
            Console.WriteLine($"Message sent from {senderName}");
        }
        

        static UserModel ? FindUser(IMongoCollection<UserModel> usersCollection, string? username) 
        {
            var filter = Builders<UserModel>.Filter.Eq(u => u.Username, username);
            return usersCollection.Find(filter).FirstOrDefault();
        }

        static void SendMessage(UserModel sender, UserModel receiver, string message)
        {
            receiver.MessageHistory.Add($"From {sender.Username}: '{message}'");

            var updateReceiver = Builders<UserModel>.Update.Set(u => u.MessageHistory, receiver.MessageHistory);
            var filterReceiver = Builders<UserModel>.Filter.Eq(u => u.Username, receiver.Username);

            var mongoClient = new MongoClient("mongodb://localhost:27017");
            var usersCollection = mongoClient.GetDatabase("mongoTest").GetCollection<UserModel>("users");

            usersCollection.UpdateOne(filterReceiver, updateReceiver);
        }
    }

    class UserModel
    {
        public ObjectId _id { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public List<string> MessageHistory { get; set; } = new List<string>();
    }
}