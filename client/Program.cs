using System.Net;
using System.Net.Sockets;

namespace client;

class Program
{
    static void Main(string[] args)
    {
        List<User> activeUsers = new List<User>();
        //skicka meddelandet till aktiva användare
        SendMessageToActiveUsers(message);

        private static void SendMessageToActiveUsers(string message)
        {
            byte[] buffer = System.Text.Encoding.ASCII.GetBytes(message);

            foreach (User user in activeUsers)
            {
                user.Socket.Send(buffer);
            }
        }
    class User
    {
        public Socket Socket { get; }

        public User(Socket socket)
        {
            Socket = socket;
        }
    }

    IPAddress ipAddress = new IPAddress(new byte[] { 127, 0, 0, 1 });
    IPEndPoint iPEndPoint = new IPEndPoint(ipAddress, 25500);

    Socket clientSocket = new Socket(
        ipAddress.AddressFamily,
        SocketType.Stream,
        ProtocolType.Tcp
    );

    clientSocket.Connect(iPEndPoint);

        while (true)
        {
            string message = Console.ReadLine()!;
    byte[] buffer = System.Text.Encoding.ASCII.GetBytes(message);

    clientSocket.Send(buffer);
        }
    }
}
