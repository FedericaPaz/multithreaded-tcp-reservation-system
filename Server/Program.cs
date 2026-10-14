using Common.Config;
using Common.Network;


namespace Server;

class Program
{
    static void Main(string[] args)
    {
        SettingsManager settings = new SettingsManager();
        string ip = settings.ReadSetting("ServerIp");
        int puerto = settings.ReadIntSetting("ServerPort");

        if (settings.ReadSetting("LoadSampleData") == "true")
            SampleData.Load();
        
        ServerManager server = new ServerManager(ip, puerto);
        Thread serverThread = new Thread(() => server.Start()); //se crea el hilo del listener
        serverThread.Start();
        
        string? command = "";
        while (command != "exit" && command != null)
        {
            command = Console.ReadLine()?.ToLower();
        }

        server.Stop();
        serverThread.Join();
    }
}

