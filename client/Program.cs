﻿using System.Net;
using System.Net.Sockets;

namespace client;

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

        while (true)
        {
            Console.WriteLine("Possible commands:");
            Console.WriteLine("login <username> <password>");
            Console.WriteLine("new <username> <password>");
            
            string message = Console.ReadLine()!;

            // logik för commands
            string parsedMessage = parseInput(message);
            byte[] buffer = System.Text.Encoding.ASCII.GetBytes(parsedMessage);
            clientSocket.Send(buffer);

            // ta emot meddelanden
            byte[] incoming = new byte[5000];
            int read = clientSocket.Receive(incoming);
            string serverMessage = System.Text.Encoding.UTF8.GetString(incoming, 0, read);
            Console.Clear();
            Console.WriteLine(serverMessage);
            if (serverMessage == "Login Success!")
            {
                Console.Clear();
                Console.WriteLine(serverMessage);
                Thread ClientThread = new Thread(SendClientThread);
                ClientThread.Start();
                Thread ListeningThread = new Thread(ListeningClientThread);
                ListeningThread.Start();
                break;
            }
        }
    }

    static void SendClientThread()
    {
        while (true)
        {
            string message = Console.ReadLine()!;
            byte[] buffer = System.Text.Encoding.ASCII.GetBytes(message);
            clientSocket.Send(buffer);
            if (message == "logout")
            {
                break;
            }
        }
    }

    static void ListeningClientThread()
    {
        while (true)
        {
            byte[] incoming = new byte[5000];
            int read = clientSocket.Receive(incoming);
            string serverMessage = System.Text.Encoding.UTF8.GetString(incoming, 0, read);
            Console.WriteLine(serverMessage);
        }
    }

    static string parseInput(string input)
    {
        string parsedInput = input.ToLower().Replace(" ", ":");
        return parsedInput;
    }
}