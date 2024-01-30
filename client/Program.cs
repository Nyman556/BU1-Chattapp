using System.Net;
using System.Net.Sockets;

namespace client;

class Program
{
    static void Main(string[] args)
    {
        IPAddress ipAddress = new IPAddress(new byte[] { 127, 0, 0, 1 });
        IPEndPoint iPEndPoint = new IPEndPoint(ipAddress, 25500);

        Socket clientSocket = new Socket(
            ipAddress.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp
        );

        clientSocket.Connect(iPEndPoint);
        Console.WriteLine("Connected to server!");

        while (true)
        {
            string message = Console.ReadLine()!;
            byte[] buffer = System.Text.Encoding.ASCII.GetBytes(message);

            clientSocket.Send(buffer);
        }
    }
}
