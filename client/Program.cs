using System.Net;
using System.Net.Sockets;

namespace client;

class Program
{
    private static bool loggedIn = false;
    private static IPAddress ipAddress = new IPAddress(new byte[] { 127, 0, 0, 1 });
    private static IPEndPoint iPEndPoint = new IPEndPoint(ipAddress, 25500);
    private static Socket clientSocket = new Socket(
        ipAddress.AddressFamily,
        SocketType.Stream,
        ProtocolType.Tcp
    );

    static void Main(string[] args)
    {
        clientSocket.Connect(iPEndPoint);

        while (!loggedIn)
        {
            string message = Console.ReadLine()!;
            byte[] buffer = System.Text.Encoding.ASCII.GetBytes(message);

            clientSocket.Send(buffer);

            // ta emot meddelanden
            byte[] incoming = new byte[5000];
            int read = clientSocket.Receive(incoming);
            string serverMessage = System.Text.Encoding.UTF8.GetString(incoming, 0, read);
            Console.WriteLine(serverMessage);
            if (serverMessage == "Login Success!")
            {
                Thread ClientThread = new Thread(LoggedInClientThread);
                ClientThread.Start();
            }
        }
    }

    static void LoggedInClientThread()
    {
        while (true)
        {
            string message = Console.ReadLine()!;
            byte[] buffer = System.Text.Encoding.ASCII.GetBytes(message);

            clientSocket.Send(buffer);
        }
    }

    // TODO: skapa login metod -> login:<username>:<password>
}
