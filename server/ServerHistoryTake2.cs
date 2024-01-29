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
public class LogMessages
{

    //skapar ett unikt ID för denna specifika log
    public ObjectId LogId { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; }

    // skapar en list av Log meddelande eftersom vi inte vet hur många användare som 
    //kommer vara kopplade till chatten



}


public class PrivateLog : LogMessages
{
    // söker med user name istället för ObjectId 
    public string UserName;

}

public class PublicLog : LogMessages
{

}

class HistoryService
{
    public MongoClient mongoClient;
    public IMongoDatabase database;
    public IMongoCollection<LogMessages> collection;

    public List<PrivateLog> PrivateMessages;
    public List<PublicLog> PublicMessages;


    public HistoryService()
    {
        this.mongoClient = new MongoClient("mongodb://localhost:27017/");
        this.database = this.mongoClient.GetDatabase("mongoTest");
        this.collection = this.database.GetCollection<LogMessages>("logMessage");

        this.PublicMessages = new List<PublicLog>();
        this.PrivateMessages = new List<PrivateLog>();
    }



    public void SplitMessage(string message)
    {

        List<string> splitMessage = message.Split(' ').ToList();
        if (splitMessage != null)
        {
            string PrivateOrePublic = splitMessage[0].ToLower();

            if (PrivateOrePublic == "public")
            {

                splitMessage.Remove(splitMessage[0]);
                string joinedMessage = string.Join(" ", splitMessage);
                SavePublicMessage(joinedMessage);

            }
            else if (PrivateOrePublic == "private")
            {
                //ev ha med ordet public för att göra det tydligt
                //   splitMessage.Remove(splitMessage[0]);
                string joinedMessage = string.Join(" ", splitMessage);
                SavePrivateMessage(joinedMessage);
            }
        }
    }

    public void SavePublicMessage(string message)
    {
        var log = new PublicLog { Message = message, Timestamp = GetTimeStamp("W. Europe Standard Time") };

        ; if (this.PublicMessages.Count <= 29)
        {

            this.PublicMessages.Add(log);

        }
        else if (this.PublicMessages.Count > 29)
        {
            this.PublicMessages.Remove(this.PublicMessages[0]);
            this.PublicMessages.Add(log);

        }


    }

    public void SavePrivateMessage(string message)
    {
        var log = new PrivateLog { Message = message, Timestamp = GetTimeStamp("W. Europe Standard Time") };
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

    public DateTime GetTimeStamp(string timeZone)
    {
        DateTime timeUtc = DateTime.UtcNow;

        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        DateTime timeDate = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, zone);
        return timeDate;


    }
    public savePrivateLogToDataBase()
    {
        //spara listan till databasen 
        this.collection.InsertMany(PrivateMessages);
    }
    public savePublicLogToDataBase()
    {
        //spara listan till databasen 
        this.collection.InsertMany(PublicMessages);
    }
    public void GetPrivateLog(string Username)
    {
        var filter = Builders<PrivateLog>.Filter.Eq(LogMessages => LogMessages.UserName, username);
        var logMessage = this.collection.Find(filter).ToList();
        return logMessage;
    }
    public void GetPublicLog()
    {
        var filter = Builders<PublicLog>.Filter.Empty;
        var logMessage = this.collection.Find(filter).ToList();
        return logMessage;
    }
}