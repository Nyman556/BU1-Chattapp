using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;

namespace server;

public class LogMessages
{
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; }
}

public class PrivateLog : LogMessages { }

public class PublicLog : LogMessages
{
    public ObjectId _id { get; set; }
}

public class HistoryService
{
    public Server chatServer;
    public MongoClient mongoClient;
    public IMongoDatabase database;
    public IMongoCollection<PrivateLog> PrivCollection;

    public IMongoCollection<PublicLog> PubCollection;

    public List<PrivateLog> PrivateMessages { get; set; }

    public HistoryService(Server server)
    {
        this.chatServer = server;
        this.mongoClient = new MongoClient("mongodb://localhost:27017/");
        this.database = this.mongoClient.GetDatabase("chattApp");
        this.PrivCollection = this.database.GetCollection<PrivateLog>("PrivateMessage");
        this.PubCollection = this.database.GetCollection<PublicLog>("PublicMessage");

        this.PrivateMessages = new List<PrivateLog>();
    }

   

    public void SavePublicMessage(string username, string message)
    {
        var log = new PublicLog
        {
            Message = "public message from: " + username + ": " + message,
            Timestamp = GetTimeStamp("Central European Standard Time")
        };
        var PublicMessages = GetPublicLog();
        if (PublicMessages.Count <= 29)
        {
            this.PubCollection.InsertOne(log);
        }
        else if (PublicMessages.Count > 29)
        {
            DeleteFirstLogMessage();
            this.PubCollection.InsertOne(log);
        }
    }

    public void DeleteFirstLogMessage()
    {
        var filter = Builders<PublicLog>.Filter.Empty;
        var sort = Builders<PublicLog>.Sort.Ascending(entry => entry.Timestamp);

        var firstLogMessages = PubCollection.Find(filter).Sort(sort).FirstOrDefault();

        if (firstLogMessages != null)
        {
            var deleteFilter = Builders<PublicLog>.Filter.Eq(
                message => message.Timestamp,
                firstLogMessages.Timestamp
            );
            PubCollection.DeleteOne(deleteFilter);
        }
    }

public void SavePrivateLog(string senderName, string receiverName, string message)
{
    UserModel? receiver = chatServer.userCollection.AsQueryable().Where(r => r.Username == receiverName!).FirstOrDefault();

    if (receiver != null)
    {
        if (receiver.Log == null)
        {
            receiver.Log = new List<PrivateLog>();
        }

        var newMessage = new PrivateLog
        {
            Message = "Private message from: " + senderName + ": " + message,
            Timestamp = GetTimeStamp("Central European Standard Time")
        };

        receiver.Log.Insert(0, newMessage);

        if (receiver.Log.Count > 29)
        {
            receiver.Log.RemoveRange(29, receiver.Log.Count - 29);
        }

        var update = Builders<UserModel>.Update.Set(
            u => u.Log,
            receiver.Log
        );
        var filter = Builders<UserModel>.Filter.Eq(u => u.Username, receiver.Username);

        chatServer.userCollection.UpdateOne(filter, update);
    }
    else
    {
        // Kollar om receiverName existerar
        Console.WriteLine($"Receiver '{receiverName}' not found.");
    }
}


    public List<PublicLog> GetPublicLog()
    {
        var filter = Builders<PublicLog>.Filter.Empty;
        var logMessage = this.PubCollection.Find(filter).ToList();
        return logMessage;
    }

    public List<PrivateLog> GetPrivateLog(
       
        string username
    )
    {
        var filter = Builders<UserModel>.Filter.Eq(Log => Log.Username, username);
        var user = chatServer.userCollection.Find(filter).FirstOrDefault();

        if (user != null)
        {
            return user.Log!;
        }

        return new List<PrivateLog>();
    }

   

    public DateTime GetTimeStamp(string timeZone)
    {
    DateTime timeUtc = DateTime.UtcNow;

    TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
    DateTime timeDate = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, zone);

  
    timeDate = timeDate.AddHours(1);

    return timeDate;
    
    }
}
