using System.Net;
using System.Net.Sockets;

using ServerTests;

namespace server;

class Program
{
    static void Main(string[] args)
    {
        List<Socket> sockets = new List<Socket>();
        IPAddress ipAddress = new IPAddress(new byte[] { 127, 0, 0, 1 });
        IPEndPoint iPEndPoint = new IPEndPoint(ipAddress, 25500);

        Socket serverSocket = new Socket(
            ipAddress.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp
        );

        serverSocket.Bind(iPEndPoint);
        serverSocket.Listen(5);

        while (true)
        {
            if (serverSocket.Poll(0, SelectMode.SelectRead))
            {
                Socket client = serverSocket.Accept();
                Console.WriteLine("A client has connected!");
                sockets.Add(client);
            }

            foreach (Socket client in sockets)
            {
                // Blockar inte koden om det inte finns något att läsa.
                if (client.Poll(0, SelectMode.SelectRead))
                {
                    byte[] incoming = new byte[5000];
                    int read = client.Receive(incoming);
                    string message = System.Text.Encoding.UTF8.GetString(incoming, 0, read);
                    Console.WriteLine("From a client: " + message);
                }
            }
        }
    }
}
