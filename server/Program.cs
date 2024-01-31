

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



        var historyService = new HistoryService();

        MongoClient mongoClient = new MongoClient("mongodb://localhost:27017");
        IMongoDatabase database = mongoClient.GetDatabase("mongoTest");

         var usersCollection = database.GetCollection<UserModel>("users");

        // historyService.saveNewUser(usersCollection, "Berit", "ggg");
        for(int i  =0; i < 35; i++){
           historyService.SaveMessage("public hej där!" + i, "Berit"); 
        }
         

        // historyService.SaveMessage("private hej Berit!", "Frank");

        // historyService.SaveMessage("private vad händer jao!", "Berit");
        // historyService.UpdatePrivetLog(usersCollection, "Berit");
        // Console.WriteLine("update done!");

        //historyService.GetPrivateLog(usersCollection, "Frank");

        //List<PrivateLog> privetMessageList = historyService.GetPrivateList();
        //historyService.saveNewUser(usersCollection, "Greger", "grs");


        // var publicLogs = historyService.GetPublicLog();
        // var privateLogs = historyService.GetPrivateLog(usersCollection, "Frank");
        // var beritsLog= historyService.GetPrivateLog(usersCollection, "Berit");


        // Console.WriteLine("Public Logs:");
        // foreach (var log in publicLogs)
        // {
        //     Console.WriteLine($"{log.Timestamp}: {log.Message}");
        // }

//         Console.WriteLine("\nPrivate Logs:");
//         foreach (var log in privateLogs)
//         {
//             Console.WriteLine($"{log.Timestamp}: {log.UserName} - {log.Message}");
//         }

//   var usersCollection = database.GetCollection<UserModel>("users");

//     var privateLogs = historyService.GetPrivateLog(usersCollection, "Fredrik");

    // Console.WriteLine("\nPrivate Logs:");
    // if (privateLogs != null)
    // {
    //     foreach (var log in privateLogs)
    //     {
    //         Console.WriteLine($"{log.Timestamp}:  {log.Message}");
    //     }
    // }
    // else
    // {
    //     Console.WriteLine("No private logs found for the specified user.");
    // }
    //  Console.WriteLine("\nberits Logs:");
    // if (beritsLog != null)
    // {
    //     foreach (var log in beritsLog)
    //     {
    //         Console.WriteLine($"{log.Timestamp}:  {log.Message}");
    //     }
    // }
    // else
    // {
    //     Console.WriteLine("No private logs found for the specified user.");
    // }
        // historyService.UpdatePrivetLog(usersCollection, "Fredrik");


        //  UserModel newUser = new UserModel {Username = "Frank", Password = "bbg"};


        //  foreach(var logs in privetMessageList){
        //      newUser.log.Add(logs); 
        //  } 
        //  usersCollection.InsertOne(newUser);



       


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

    public ObjectId _id { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public List<PrivateLog> Log { get; set; } = new List<PrivateLog>();

}