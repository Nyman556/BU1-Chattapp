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

public abstract class LogMessages
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
    public MongoClient mongoClient;
    public IMongoDatabase database;
    public IMongoCollection<PrivateLog> PrivCollection;

    public IMongoCollection<PublicLog> PubCollection;

    public List<PrivateLog> PrivateMessages { get; set; }

    public HistoryService()
    {
        this.mongoClient = new MongoClient("mongodb://localhost:27017/");
        this.database = this.mongoClient.GetDatabase("chattApp");
        this.PrivCollection = this.database.GetCollection<PrivateLog>("PrivateMessage");
        this.PubCollection = this.database.GetCollection<PublicLog>("PublicMessage");

        this.PrivateMessages = new List<PrivateLog>();
    }

    public void SaveMessage(string username, string message)
    {
        List<string> splitMessage = message.Split(' ').ToList();
        if (splitMessage != null)
        {
            string PrivateOrPublic = splitMessage[0].ToLower();

            if (PrivateOrPublic == "public")
            {
                splitMessage.Remove(splitMessage[0]);
                string joinedMessage = string.Join(" ", splitMessage);
                SavePublicMessage(joinedMessage, username);
            }
            else if (PrivateOrPublic == "private")
            {
                //ev ha med ordet public för att göra det tydligt
                //   splitMessage.Remove(splitMessage[0]);
                string joined = string.Join(" ", splitMessage);
                SavePrivateMessage(joined, username);
            }
        }
    }

    public void SavePublicMessage(string username, string message)
    {
        var log = new PublicLog
        {
            Message = username + ": " + message,
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

    public void SavePrivateMessage(string username, string message)
    {
        var log = new PrivateLog
        {
            Message = username + ": " + message,
            Timestamp = GetTimeStamp("Central European Standard Time")
        };
        if (this.PrivateMessages.Count <= 29)
        {
            this.PrivateMessages.Add(log);
        }
        else if (this.PrivateMessages.Count > 29)
        {
            this.PrivateMessages.Remove(this.PrivateMessages[0]);
            this.PrivateMessages.Add(log);
        }
    }

    public void saveNewUser(
        IMongoCollection<UserModel> userCollection,
        string UserName,
        string password
    )
    {
        UserModel newUser = new UserModel { Username = UserName, Password = password };

        foreach (var logs in this.PrivateMessages)
        {
            newUser.Log.Add(logs);
        }
        userCollection.InsertOne(newUser);
    }

    public List<PublicLog> GetPublicLog()
    {
        var filter = Builders<PublicLog>.Filter.Empty;
        var logMessage = this.PubCollection.Find(filter).ToList();
        return logMessage;
    }

    public List<PrivateLog> GetPrivateLog(
        IMongoCollection<UserModel> userCollection,
        string username
    )
    {
        var filter = Builders<UserModel>.Filter.Eq(Log => Log.Username, username);
        var user = userCollection.Find(filter).FirstOrDefault();

        if (user != null)
        {
            return user.Log!;
        }

        return new List<PrivateLog>();
    }

    public void UpdatePrivetLog(IMongoCollection<UserModel> userCollection, string UserName)
    {
        var filter = Builders<UserModel>.Filter.Eq(User => User.Username, UserName);
        var update = Builders<UserModel>.Update.Set(User => User.Log, this.PrivateMessages);

        // Perform the update on the list of objects that match the filter
        var result = userCollection.UpdateMany(filter, update);
    }

    public DateTime GetTimeStamp(string timeZone)
    {
        DateTime timeUtc = DateTime.UtcNow;

        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        DateTime timeDate = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, zone);
        return timeDate;
    }
}
