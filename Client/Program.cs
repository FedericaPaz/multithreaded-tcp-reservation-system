using System.Net;
using System.Net.Sockets;
using System.Text;
using Client.UI;
using Common.Config;
using Common.Network;

namespace Client;

class Program
{
    static void Main(string[] args)
    {
        var settings = new SettingsManager();
        string ip = settings.ReadSetting("ServerIp");
        int port = settings.ReadIntSetting("ServerPort");

        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            socket.Connect(new IPEndPoint(IPAddress.Parse(ip), port));
            Console.WriteLine("Connected to Edison server!");

            var loginMenu = new LoginMenu(socket);
            bool running = true;

            while (running)
            {
                bool loggedIn = loginMenu.Show();

                if (loggedIn)
                {
                    var mainMenu = new MainMenu(socket, loginMenu.LoggedInUser);
                    mainMenu.Show();
                }
                else
                {
                    running = false; 
                }
            }
        }
        catch (SocketException)
        {
            Console.WriteLine("Lost connection to server.");
        } 
        catch (Exception ex)
        {
           Console.WriteLine($"Unexpected error: {ex.Message}"); 
        }
        finally
        {
            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException)
            {
            }
            socket.Close();
        }
    }
}