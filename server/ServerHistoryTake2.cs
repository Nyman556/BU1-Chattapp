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


public class PrivateLog : LogMessages {

//public ObjectId UserId;

}

public class PublicLog : LogMessages {

}

class HistoryService {
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

   

    public void SplitMessage(string message){

        List<string> splitMessage = message.Split(' ').ToList();

        string PrivateOrePublic = splitMessage[0].ToLower();

            if(PrivateOrePublic == "public"){

                splitMessage.Remove(splitMessage[0]);
                string joinedMessage = string.Join(" ", splitMessage);
                SavePublicMessage(joinedMessage);

            }else if(PrivateOrePublic == "private"){

                  splitMessage.Remove(splitMessage[0]);
                string joinedMessage = string.Join(" ", splitMessage);
                SavePrivateMessage(joinedMessage);
            }
        }

    public void SavePublicMessage(string message){
         var log = new PublicLog { Message = message, Timestamp = GetSwedishTime()};

;        if (this.PublicMessages.Count <= 29)
        {
            
            this.PublicMessages.Add(log);

        }
        else if (this.PublicMessages.Count > 29)
        {
            this.PublicMessages.Remove(this.PublicMessages[0]);
            this.PublicMessages.Add(log);

        }
       
        
    }

    public void SavePrivateMessage(string message){
          var log = new PrivateLog { Message = message, Timestamp = GetSwedishTime()};
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

    public DateTime GetSwedishTime()
    {
        DateTime timeUtc = DateTime.UtcNow;

        TimeZoneInfo estZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        DateTime estTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, estZone);
        return estTime;


    }
}