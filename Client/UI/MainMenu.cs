using System.Net.Sockets;
using Common.Network;
using Common.Protocol;

namespace Client.UI;

public class MainMenu
{
    private readonly SocketHelper _socketHelper;
    private readonly string _username;
    private readonly VehicleMenu _vehicleMenu;
    private readonly ReservationMenu _reservationMenu;

    public MainMenu(Socket socket, string username)
    {
        _socketHelper = new SocketHelper(socket);
        _username = username;
        _vehicleMenu = new VehicleMenu(socket, username);
        _reservationMenu = new ReservationMenu(socket, username);
    }

    public void Show()
    {
        bool logout = false;

        while (!logout)
        {
            ConsoleUI.Title("EDISON - Main Menu");
            ConsoleUI.Info($"Logged in as: {_username}");
            ConsoleUI.MenuOption("1", "Vehicles");
            ConsoleUI.MenuOption("2", "Reservations");
            ConsoleUI.MenuOption("3", "View activity history");
            ConsoleUI.MenuOption("4", "Logout");
            string option = ConsoleUI.Ask("Option");

            switch (option)
            {
                case "1":
                    _vehicleMenu.Show();
                    break;
                case "2":
                    _reservationMenu.Show();
                    break;
                case "3":
                    ShowHistory();
                    break;
                case "4":
                    Logout();
                    logout = true;
                    break;
                default:
                    ConsoleUI.Error("Invalid option.");
                    break;
            }
        }
    }
    
    private void Logout()
    {
        byte[] data = new FieldWriter().WriteString(_username).ToArray();
        
        var request = new Frame{Command = (int)Command.Logout, Data = data};
        FrameIO.Send(_socketHelper, request);
        Frame response = FrameIO.Receive(_socketHelper);
        
        var reader = new FieldReader(response.Data);
        StatusCode status = (StatusCode)reader.ReadInt();
        string message = reader.ReadText();
        ConsoleUI.ShowResponse(status, message);
    }

    private void ShowHistory()
    {
        byte[] data = new FieldWriter().WriteString(_username).ToArray();
        
        Frame request = new Frame{ Command = (int)Command.ActivityHistory, Data = data };
        FrameIO.Send(_socketHelper, request);
        Frame response = FrameIO.Receive(_socketHelper);
        
        FieldReader reader = new FieldReader(response.Data);
        StatusCode status = (StatusCode)reader.ReadInt();
        if (status != StatusCode.Ok)
        {
            ConsoleUI.ShowResponse(status,reader.ReadText());
            return;
        }
        
        int count = reader.ReadInt();
        ConsoleUI.Title("Your activity history");

        if (count == 0)
        {
            ConsoleUI.Info("No activity recorded yet.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            string type = reader.ReadText();
            string date = reader.ReadText();
            string details = reader.ReadText();
            Console.WriteLine($" [{date}] {type}: {details}");
        }
        
        if(count == 1)
            ConsoleUI.Info($"{count} event found.");
        else
            ConsoleUI.Info($"{count} events found.");
    }
}