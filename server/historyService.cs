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

// en metod som sparar 30 meddelanden i en log kopplat till en user med ett unikt ID

    public void SaveLogMessages(string message, ObjectId user_Id)
    {
        var list = GetLog();
        if (list.Count <= 29)
        {
            var LogMessages = new LogMessages { Message = message, Timestamp = GetSwedishTime(), UserId = user_Id };
            this.collection.InsertOne(LogMessages);
        }
        else
        {
            //om det är 29 meddelande i logen så tas det första meddelandet bort innan ett nytt sparas
            DeleteFirstLogMessage();
            var LogMessages = new LogMessages { Message = message, Timestamp = GetSwedishTime(), UserId = user_Id };
            this.collection.InsertOne(LogMessages);
        }

    }





//en metod som skriver ut alla meddelanden i en log 
    public List<LogMessages> GetLog(ObjectId user_id)
    {
        var filter = Builders<LogMessages>.Filter.Eq(id => id.userId , user_id);
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
            var deleteFilter = Builders<LogMessages>.Filter.Eq(entry => entry.Id, firstLogMessages.Id);
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


    public ObjectId Id { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; }
    // Användarens ID som loggen är kopplad till
    public ObjectId UserId { get; set; }
}
