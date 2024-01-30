using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace client
{
    class Program
    {
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
            Console.WriteLine("Connected to server!");

            bool loggedIn = false;
            string serverMessage;

            while (true)
            {
                // ta emot meddelanden
                if (clientSocket.Poll(0, SelectMode.SelectRead))
                {
                    serverMessage = ReceiveMessage();
                    Console.Clear();
                    Console.WriteLine(serverMessage);
                }

                Console.WriteLine("Possible commands: login <username> <password>");
                string message = Console.ReadLine()!;

                // logik för commands
                string parsedMessage = ParseInput(message);
                byte[] buffer = Encoding.UTF8.GetBytes(parsedMessage);
                clientSocket.Send(buffer);

                // ta emot meddelanden
                serverMessage = ReceiveMessage();
                Console.Clear();
                Console.WriteLine(serverMessage);

                if (!loggedIn && serverMessage == "Login Success!")
                {
                    Console.Clear();
                    loggedIn = true;
                }

                if (loggedIn)
                {
                    Console.WriteLine("type 'logout' to exit.");
                    while (true)
                    {
                        Console.Write("Message: ");
                        string userMessage = Console.ReadLine()!;
                        if (userMessage == "logout")
                        {
                            byte[] logoutBuffer = Encoding.UTF8.GetBytes(userMessage);
                            clientSocket.Send(logoutBuffer);
                            break;
                        }

                        byte[] userMessageBuffer = Encoding.UTF8.GetBytes(userMessage);
                        clientSocket.Send(userMessageBuffer);
                    }
                }
            }
            clientSocket.Close();
        }

        static string ReceiveMessage()
        {
            byte[] incoming = new byte[5000];
            int read = clientSocket.Receive(incoming);
            return Encoding.UTF8.GetString(incoming, 0, read);
        }

        static string ParseInput(string input)
        {
            return input.ToLower().Replace(" ", ":");
        }
    }
}
