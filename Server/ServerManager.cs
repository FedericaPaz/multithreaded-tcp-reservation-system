using System.Net;
using System.Net.Sockets;

namespace Server;

public class ServerManager
{
    private readonly Socket _listener;
    private readonly List<Thread> _clientsThreads = new();
    private readonly List<ClientHandler> _activeHandlers = new();
    private readonly object _lock = new();
    private bool _isRunning;

    public ServerManager(string ip, int port)
    {
        _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listener.Bind(new IPEndPoint(IPAddress.Parse(ip), port));
    }

    public void Start()
    {
        _listener.Listen(10);
        _isRunning = true;
        Console.WriteLine("Edison server initialized. Write 'exit' and press Enter to exit.");

        while (_isRunning)
        {
            try
            {
                Socket clientSocket = _listener.Accept();
                Console.WriteLine("Client connected: " + clientSocket.RemoteEndPoint.ToString());
                
                var handler = new ClientHandler(clientSocket);

                Thread thread = new Thread(() =>
                {
                    handler.Accept();
                    
                    lock (_lock)
                    {
                        _activeHandlers.Remove(handler);
                    }
                });

                lock (_lock)
                {
                    _clientsThreads.Add(thread);
                    _activeHandlers.Add(handler);
                }
                
                thread.Start();
            }
            catch (SocketException)
            {
                break;
            }
        }
    }

    public void Stop()
    {
        _isRunning = false;

        lock (_lock)
        {
            foreach (ClientHandler handler in _activeHandlers)
            {
                handler.Disconnect();
            }
        }
        
        _listener.Close();

        List<Thread> threads;
        lock (_lock)
        {
            threads = new List<Thread>(_clientsThreads);
        }
        
        foreach (Thread thread in threads)
        {
            thread.Join();
        }
        
        Console.WriteLine($"Server has been stopped.");
    }
}