using System.Net;
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
    //Använder inbyggt i .NET som avbryter trådar
    private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();



    static void Main(string[] args)
    {
        Thread ClientThread = new Thread(() => SendClientThread(cancellationTokenSource.Token));
        Thread ListeningThread = new Thread(() => ListeningClientThread(cancellationTokenSource.Token));
        
        try
        {
            ClientThread.Start();
            ListeningThread.Start();

            clientSocket.Connect(iPEndPoint);
            Console.WriteLine("Connected to server! please wait...");
            

            Thread.Sleep(2000);
            while (true)
            {
                Console.WriteLine("Please enter : login <username> <password>");
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
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        finally
        {
            // Stäng trådarna när de inte längre behövs
            ClientThread.Join();
            ListeningThread.Join();
        }
    }

    static void SendClientThread(CancellationToken cancellationToken) //tar in i parametern
    {
        while (!cancellationToken.IsCancellationRequested) // wait for client thread
        {
            string message = Console.ReadLine()!;
            byte[] buffer = System.Text.Encoding.ASCII.GetBytes(message);
            clientSocket.Send(buffer);
            if (message == "logout")
            {
                Console.WriteLine("Logging out.");
                Thread.Sleep(800);
                Console.WriteLine("Logging out..");
                Thread.Sleep(800);
                Console.WriteLine("Logging out...");
                Thread.Sleep(800);
                Console.Clear();
                clientSocket.Close();
                cancellationTokenSource.Cancel();
                Environment.Exit(0);
                break;
            }
        }
    }

    static void ListeningClientThread(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && clientSocket.Connected)
        {
            try
            {
                byte[] incoming = new byte[5000];
                int read = clientSocket.Receive(incoming);
                string serverMessage = System.Text.Encoding.UTF8.GetString(incoming, 0, read);
                Console.WriteLine(serverMessage);

                if (serverMessage == "logout")
                {
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                    cancellationTokenSource.Cancel();
                    break;
                }
            }
            catch (SocketException ex)
            {                
                Console.WriteLine($"SocketException: {ex.Message}");
                break;
            }
        }
    }


    static string parseInput(string input)
    {
        string parsedInput = input.ToLower().Replace(" ", ":");
        return parsedInput;
    }

}
