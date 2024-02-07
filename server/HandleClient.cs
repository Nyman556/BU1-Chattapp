using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver;
using server;

namespace server;

public class Client
{
    private Socket clientSocket;
    public Server chatServer;
    public string? username;
    private bool _LoggedIn = false;
    public Client? thisClient;

    public Client(Socket socket, Server server)
    {
        clientSocket = socket;
        chatServer = server;
    }

    public void Start()
    {
        HandleLogin();
        if (_LoggedIn)
        {
            chatServer.AddClient(thisClient!);
            HandleMessages();
        }
    }

    public void HandleMessages()
    {
        try
        {
            while (_LoggedIn)
            {
                byte[] incoming = new byte[5000];
                int read = clientSocket.Receive(incoming);
                string message = System.Text.Encoding.UTF8.GetString(incoming, 0, read);
                if (string.IsNullOrEmpty(message))
                {
                    break;
                }

                if (message == "logout")
                {
                    if (username != null)
                    {
                        string _message = $"User {username} logged out.";
                        HandleLogout(username, _message);
                    }
                }
                else if (message.StartsWith("private"))
                {
                    HandlePrivateMessage(username!, message);
                }
                else
                {
                    SendGlobalMessage(username!, message);
                }
            }
        }
        catch (SocketException)
        {
            string _message = $"User {username} disconnected.";
            _LoggedIn = false;
            HandleLogout(username!, _message);
            clientSocket.Close();
        }
    }

    private void HandleLogin()
    {
        try
        {
            while (!_LoggedIn)
            {
                byte[] incoming = new byte[5000];
                int read = clientSocket.Receive(incoming);
                string message = System.Text.Encoding.UTF8.GetString(incoming, 0, read);
                if (chatServer.validateMessage(message, 3))
                {
                    if (message.StartsWith("login"))
                    {
                        string[] credentials = message.Substring(6).Split(':');
                        username = credentials[0];
                        string password = credentials[1];

                        if (chatServer.ValidateCredentials(username, password))
                        {
                            clientSocket.Send(System.Text.Encoding.UTF8.GetBytes("Login Success!"));
                            _LoggedIn = true;
                            string _message = $"{username} logged in!";
                            Console.WriteLine(_message);
                            Notification(_message);
                            var history = chatServer.historyService.GetPublicLog();
                            foreach (var logmessage in history)
                            {
                                clientSocket.Send(
                                    System.Text.Encoding.UTF8.GetBytes(
                                        $"{logmessage.Message} | {logmessage.Timestamp}"
                                    )
                                );
                                Thread.Sleep(100);
                            }

                            // För private messages
                            var privateLogs = chatServer.historyService.GetPrivateLog(username!);
                            if (privateLogs != null)
                            {
                                foreach (var privateMessage in privateLogs)
                                {
                                    clientSocket.Send(
                                        System.Text.Encoding.UTF8.GetBytes(
                                            $"Private Log: {privateMessage.Message} || {privateMessage.Timestamp}"
                                        )
                                    );
                                    Thread.Sleep(100);
                                }
                            }
                        }
                        else
                        {
                            clientSocket.Send(System.Text.Encoding.UTF8.GetBytes("Login Failed!"));
                        }
                    }
                    else if (message.StartsWith("new"))
                    {
                        string[] newUserData = message.Substring(4).Split(':');
                        string newUsername = newUserData[0];
                        string newPassword = newUserData[1];

                        if (CheckTaken(newUsername))
                        {
                            clientSocket.Send(
                                System.Text.Encoding.UTF8.GetBytes("username already taken!")
                            );
                        }
                        else
                        {
                            CreateNewUser(newUsername, newPassword);
                            clientSocket.Send(
                                System.Text.Encoding.UTF8.GetBytes("new user created!")
                            );
                        }
                    }
                }
                else
                {
                    clientSocket.Send(System.Text.Encoding.UTF8.GetBytes("Wrong format!"));
                }
            }
        }
        catch (SocketException)
        {
            clientSocket.Close();
        }
    }

    private void SendGlobalMessage(string username, string message)
    {
        string _message = $"{username}: {message}";
        Notification(_message);
        chatServer.historyService.SavePublicMessage(username!, message);
        chatServer.MessageCount++;
    }

    private void Notification(string message)
    {
        foreach (Client client in chatServer.clients)
        {
            if (client != thisClient)
            {
                client.clientSocket.Send(System.Text.Encoding.UTF8.GetBytes(message));
            }
        }
    }

    // Ny metod här för att se att det bara skickas till receiver
    public void PrivateNotification(string receiverName, string senderName, string message)
    {
        foreach (Client client in chatServer.clients)
        {
            // Kollar användaren
            if (client.username == receiverName)
            {
                client.clientSocket.Send(
                    System.Text.Encoding.UTF8.GetBytes($"Private {senderName} : {message}")
                );
            }
        }
    }

    private void HandleLogout(string username, string message)
    {
        chatServer.HandleLogout(username);
        Console.WriteLine(message);
        Notification(message);
    }

    private void HandlePrivateMessage(string username, string message)
    {
        chatServer.HandlePrivateMessage(username, message, this);
    }

    private void CreateNewUser(string username, string password)
    {
        chatServer.CreateNewUser(username, password);
    }

    private bool CheckTaken(string username)
    {
        bool taken = chatServer.CheckTaken(username);
        return taken;
    }
}
