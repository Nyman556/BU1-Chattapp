using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver;
using server;

namespace server;

public class Server
{
    // databas
    private MongoClient mongoClient;
    private IMongoDatabase database;
    public IMongoCollection<UserModel> userCollection;

    // serverSocket
    private Socket serverSocket;

    // lista med clienter
    public List<Client> clients;

    // Service för history
    public HistoryService historyService;

    // intern data
    private List<UserModel>? allUsers;
    public int MessageCount;

    public Server()
    {
        mongoClient = new MongoClient("mongodb://localhost:27017");
        database = mongoClient.GetDatabase("chattApp");
        userCollection = database.GetCollection<UserModel>("users");
        clients = new List<Client>();
        allUsers = new List<UserModel>();
        serverSocket = CreateServerSocket();
        MessageCount = 0;
        historyService = new HistoryService(this);
    }

    //


    private Socket CreateServerSocket()
    {
        IPAddress ipAddress = new IPAddress(new byte[] { 127, 0, 0, 1 });
        IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 25500);

        Socket socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

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
        var client = new Client(clientSocket, this);
        client.thisClient = client;

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
        string hashedPassword = EncryptPassword(password);

        var filter =
            Builders<UserModel>.Filter.Eq(u => u.Username, username)
            & Builders<UserModel>.Filter.Eq(u => u.Password, hashedPassword)
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

    private static string EncryptPassword(string password) 
    {
        // Något att tänka på. Krypterade lösenorden blir identiska om lösenorden innan kryptering är identiska. dvs "grg" får samma kryptering.
        using (SHA256 sha256Hash = SHA256.Create()) 
        {
            // konverterar lösenordet till en byte array via Encoding.UTF8.GetBytes
            byte[] hashData = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hashData.Length; i++) 
            {
                builder.Append(hashData[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }

    public void HandleLogout(string username)
    {
        var filter = Builders<UserModel>.Filter.Eq(u => u.Username, username);
        var update = Builders<UserModel>.Update.Set(v => v.LoggedIn, false);
        userCollection.UpdateOne(filter, update);

        // raderar användaren från listan vid utloggning/disconnect
        Client clientToRemove = clients.FirstOrDefault(client => client.username == username)!;
        clients.Remove(clientToRemove!);
    }

    public void CreateNewUser(string username, string password)
    {
        string hashedPassword = EncryptPassword(password);

        UserModel newUser = new UserModel
        {
            Username = username,
            Password = hashedPassword,
            LoggedIn = false
        };
        userCollection.InsertOne(newUser);
        // uppdaterar den globala userList med den nya användaren
        FetchUserData();
    }

    public void AddClient(Client thisClient)
    {
        clients.Add(thisClient);
    }

    public bool CheckTaken(string username)
    {
        var filter = Builders<UserModel>.Filter.Eq(u => u.Username, username);
        var user = userCollection.Find(filter).Any();

        return user;
    }

    public void HandlePrivateMessage(string username, string message, Client client)
    {
        string[] splitMessage = message.Split(' ');

        if (splitMessage.Length < 3)
        {
            Console.WriteLine("Invalid command. Please use the correct format");
            return;
        }

        // private <user> <message>

        string? receiverName = splitMessage[1];
        bool found = false;

        string? privateMessage = string.Join(' ', splitMessage[2..]);

        string? senderName = username;

        foreach (UserModel user in allUsers!)
        {
            if (receiverName == user.Username)
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            Console.WriteLine("User not found");
        }
        SendPrivateMessage(senderName, receiverName, privateMessage, client);
    }

    private void SendPrivateMessage(
        string senderName,
        string receiverName,
        string privateMessage,
        Client client
    )
    {
        historyService.SavePrivateLog(senderName, receiverName, privateMessage);
        client.PrivateNotification(receiverName, senderName, privateMessage);
    }

    private void ConsoleInputThread()
    {
        while (true)
        {
            Console.WriteLine("\nCommands available: userlist , clear , endserver :"); // lägg till mer commands efter hand
            string? consoleInput = Console.ReadLine();

            // logik för console Input
            if (consoleInput == "userlist")
            {
                FetchUserData();
                PrintUserList();
            }
            else if (consoleInput == "clear")
            {
                Console.Clear();
            }
            else if (consoleInput == "endserver")
            {
                Console.WriteLine("Server is shuting down...");
                Thread.Sleep(1000);
                Console.Clear();
                Environment.Exit(0);
                break;
            }

            Thread.Sleep(100);
        }
    }

    private void PrintUserList()
    {
        Console.WriteLine($"({allUsers!.Count}) Users in database:");
        foreach (UserModel user in allUsers)
        {
            Console.WriteLine("--------------------------");
            Console.WriteLine(
                $"Username: {user.Username}\nPassword: {user.Password}\nCurrently logged in: {(user.LoggedIn ? "Yes" : "No")}"
            );
        }
        Console.WriteLine("--------------------------");
        Console.WriteLine($"Messages sent since server started: {MessageCount}");
    }

    private List<UserModel> FetchUserData()
    {
        var filter = Builders<UserModel>.Filter.Empty;
        return allUsers = userCollection.Find(filter).ToList();
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
