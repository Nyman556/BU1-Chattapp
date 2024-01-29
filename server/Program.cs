using System.Net;
using System.Net.Sockets;
using MongoDB.Bson;
using MongoDB.Driver;
using ServerTests;

namespace server;

class Program
{
    static void Main(string[] args)
    {


   // Skapa en instans av HistoryService
        var historyService = new HistoryService();
        
        // Testa SplitMessage-metoden
        historyService.SplitMessage("public This is a public message");
        historyService.SplitMessage("private Joel This is a private message");
             historyService.SplitMessage("public This is a public message");
        historyService.SplitMessage("private Philip This is a private message");
             historyService.SplitMessage("public This is a public message");
        historyService.SplitMessage("private Hej där!");
            historyService.SplitMessage("private lalalalalalal");
                historyService.SplitMessage("private fint sjunget :)");


        Console.WriteLine("we try to split");
        // Testa att spara loggar i databasen
        historyService.savePublicLogToDataBase();
        historyService.savePrivateLogToDataBase();
        Console.WriteLine("we try saving to data base");
        // Testa att hämta loggar från databasen
        var publicLogs = historyService.GetPublicLog();
        var privateLogs = historyService.GetPrivateLog("John");

        // Skriv ut resultaten
       Console.WriteLine("Public Logs:"); 
        foreach (var log in publicLogs)
        {
            Console.WriteLine($"{log.Timestamp}: {log.Message}");
        }

        Console.WriteLine("\nPrivate Logs:")
        foreach (var log in privateLogs)
        {
            Console.WriteLine($"{log.Timestamp}: {log.UserName} - {log.Message}");
        }

        // MongoClient mongoClient = new MongoClient("mongodb://localhost:27017");
        // IMongoDatabase database = mongoClient.GetDatabase("mongoTest");

        // // // Hämta eller skapa en samling för användare
       
        //  var usersCollection = database.GetCollection<UserModel>("users");

        // var filter = Builders<UserModel>.Filter.Empty;
        // List<UserModel> allUsers = usersCollection.Find(filter).ToList();
        
       
        
        // foreach (UserModel user in allUsers)
        // {
        
        //     Console.WriteLine(user.Username);
        //      Console.WriteLine(user._id);
           
        // }
        //  foreach (ObjectId id in IDList)
        
        //       Console.WriteLine(id);

        //      //Data.SaveLogMessages("hej jag heter Gustav Adolf!", id);
        // }


       
        // List<Socket> sockets = new List<Socket>();
        // IPAddress ipAddress = new IPAddress(new byte[] { 127, 0, 0, 1 });
        // IPEndPoint iPEndPoint = new IPEndPoint(ipAddress, 25500);

        // Socket serverSocket = new Socket(
        //     ipAddress.AddressFamily,
        //     SocketType.Stream,
        //     ProtocolType.Tcp
        // );

        // serverSocket.Bind(iPEndPoint);
        // serverSocket.Listen(5);

        
        // while (true)
        // {
        //     if (serverSocket.Poll(0, SelectMode.SelectRead))
        //     {
        //         Socket client = serverSocket.Accept();
        //         Console.WriteLine("A client has connected!");
        //         sockets.Add(client);
        //     }

        //     foreach (Socket client in sockets)
        //     {
        //         // Blockar inte koden om det inte finns något att läsa.
        //         if (client.Poll(0, SelectMode.SelectRead))
        //         {
        //             byte[] incoming = new byte[5000];
        //             int read = client.Receive(incoming);
        //             string message = System.Text.Encoding.UTF8.GetString(incoming, 0, read);
        //             Console.WriteLine("From a client: " + message);
        //         }
        //     }
        // }
        
       
    }
}

class UserModel
{
  
    public ObjectId? _id { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
   
}


//  UserModel newUser = new UserModel {Username = "Gustav-Adolf", Password = "GRG"};
        // usersCollection.InsertOne(newUser);

//skapar en ny användare som kan spara alla sina chattkonversationer i en logMessages. 
//LogMessages i sin tur är kopplade till UserId 
        class NewUser{
            List<LogMessages> LogMessage;
            public NewUser(){
                LogMessage = new List<LogMessages>();
            }

          
        }