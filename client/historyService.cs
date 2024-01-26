using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

using System.Collections.Generic;
using System.Globalization;

namespace client;
//tror denna fil inte behövs
//lite oklart om vi ska hantera något av detta från User sidan eller om servern sparar alla data ihop med användarna som dom i sin tur kan hämta från servern när dom loggar in. 

public class HistoryLog
{


    public void SaveLogMessages(string message, objectId user_Id )
    {
        var list = GetLog();
        if (list.Count <= 29)
        {
            var LogMessages = new LogMessages { Message = message, Timestamp = GetSwedishTime(), UserId =user_Id };
            this.collection.InsertOne(LogMessages);
        }
        else
        {
            DeleteFirstLogMessage();
            var LogMessages = new LogMessages { Message = message, Timestamp = GetSwedishTime(), UserId =user_Id };
            this.collection.InsertOne(LogMessages);
        }

    }






    public List<LogMessages> GetLog()
    {
        var filter = Builders<LogMessages>.Filter.Empty;
        var logEntries = this.collection.Find(filter).ToList();
        return logEntries;
    }

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
