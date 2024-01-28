using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Globalization;

namespace server;


public class HistoryLog
{
    private MongoClient mongoClient;
    private IMongoDatabase database;
    private IMongoCollection<LogMessages> collection;

    public HistoryLog()
    {

        this.mongoClient = new MongoClient("mongodb://localhost:27017/mongoTest");
        this.database = this.mongoClient.GetDatabase("mongoTest");
        this.collection = this.database.GetCollection<LogMessages>("logMessage");
    }
// en metod som sparar 30 meddelanden i en log kopplat till en user med ett unikt ID


    public void SaveLogMessages(string message, List<ObjectId> user_Id)
    {
        var list = GetLog(user_Id);
        if (list.Count <= 29)
        {
            LogMessages LogMessages = new LogMessages { Message = message, Timestamp = GetSwedishTime(), UserId = user_Id };
            this.collection.InsertOne(LogMessages);
        }
        else
        {
            //om det är 29 meddelande i logen så tas det första meddelandet bort innan ett nytt sparas
            DeleteFirstLogMessage();
            LogMessages LogMessages = new LogMessages { Message = message, Timestamp = GetSwedishTime(), UserId = user_Id };
            this.collection.InsertOne(LogMessages);
        }

    }





//en metod som skriver ut alla meddelanden i en log 
    public List<LogMessages> GetLog(List<ObjectId> user_Id)
    {
        var filter = Builders<LogMessages>.Filter.Eq("UserId", user_Id);
        var logMessage = this.collection.Find(filter).ToList();
        return logMessage;
    }

//en metod som tar bort första meddelandet i en log 
    public void DeleteFirstLogMessage()
    {
        var filter = Builders<LogMessages>.Filter.Empty;
        var sort = Builders<LogMessages>.Sort.Ascending(entry => entry.Timestamp);

        var firstLogMessages = this.collection.Find(filter).Sort(sort).FirstOrDefault();

        if (firstLogMessages != null)
        {
            var deleteFilter = Builders<LogMessages>.Filter.Eq(message => message.LogId, firstLogMessages.LogId);
            this.collection.DeleteOne(deleteFilter);
        }
    }

    private DateTime GetSwedishTime()
    {
        DateTime timeUtc = DateTime.UtcNow;

        TimeZoneInfo estZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        DateTime estTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, estZone);
        return estTime;


    }

}

public class LogMessages
{

//skapar ett unikt ID för denna specifika log
    public ObjectId LogId { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; }

    // skapar en list av Log meddelande eftersom vi inte vet hur många användare som 
    //kommer vara kopplade till chatten
    public List<ObjectId> UserId { get; set; }

  public void NewUser(){
                this.UserId = new List<ObjectId>();
            }

}
