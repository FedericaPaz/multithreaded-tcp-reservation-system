using System.Net.Sockets;
using Common.Network;
using Common.Protocol;

namespace Client.UI;

public class ReservationMenu
{
    private readonly SocketHelper _socketHelper;
    private readonly string _username;

    public ReservationMenu(Socket socket, string username)
    {
        _socketHelper = new SocketHelper(socket);
        _username = username;
    }

    public void Show()
    {
        bool goBack = false;
        while (!goBack)
        {
            ConsoleUI.Title("EDISON - Reservation Menu");
            ConsoleUI.MenuOption("1", "Request reservation");
            ConsoleUI.MenuOption("2", "Cancel reservation");
            ConsoleUI.MenuOption("3", "Check my reservations");
            ConsoleUI.MenuOption("4", "Back");
            string option = ConsoleUI.Ask("Option");

            try
            {
                switch (option)
                {
                    case "1": RequestReservation(); break;
                    case "2": CancelReservation(); break;
                    case "3": CheckStatus(); break;
                    case "4": goBack = true; break;
                    default: ConsoleUI.Error("Invalid option."); break;
                }
            }
            catch (OperationCanceledException)
            {
                ConsoleUI.Info("Operation cancelled.");
            }
        }
    }

    private void RequestReservation()
    {
        ListAvailableModels();
        
        string modelName = ConsoleUI.AskWithCancel("Model Name to reserve");

        byte[] data = new FieldWriter().WriteString(_username).WriteString(modelName).ToArray();
        
        Frame request = new Frame {Command = (int)Command.RequestReservation, Data = data};
        FrameIO.Send(_socketHelper, request);
        Frame response = FrameIO.Receive(_socketHelper);
        
        FieldReader reader = new FieldReader(response.Data);
        StatusCode status = (StatusCode)reader.ReadInt();
        if (status != StatusCode.Ok)
        {
            ConsoleUI.ShowResponse(status, reader.ReadText());
            return;
        }
        string reservationId = reader.ReadText();
        string message = reader.ReadText();
        ConsoleUI.Success(message);
        ConsoleUI.Info($"Reservation ID(save to cancel later): {reservationId}");
    }

    private void ListAvailableModels() //no filtra para los que no hay stock, creo q en el menu de autos hay un metodos parecido podriamos abtstraerlos
    {
        byte[] data = new FieldWriter().WriteString("").WriteString("").WriteString("").ToArray();
        
        Frame request = new Frame{Command = (int)Command.SearchVehicles, Data = data};
        FrameIO.Send(_socketHelper, request);
        Frame response = FrameIO.Receive(_socketHelper);
        
        FieldReader reader = new FieldReader(response.Data);
        StatusCode status = (StatusCode)reader.ReadInt();
        if (status != StatusCode.Ok)
        {
            ConsoleUI.ShowResponse(status, reader.ReadText());
            return;
        }
        
        int count = reader.ReadInt();
        ConsoleUI.Title("Available models");
        if (count == 0)
        {
            ConsoleUI.Info($"No models registered yet.");
            return;
        }
        
        Console.WriteLine(" " + "MODEL".PadRight(22) + "PRICE".PadLeft(10) + "AVAILABLE".PadLeft(12));
        ConsoleUI.Separator();
        for (int i = 0; i < count; i++)
        {
            string modelName = reader.ReadText();
            string price = reader.ReadText();
            int available = reader.ReadInt();
            Console.WriteLine("  "  + modelName.PadRight(22) + ("$" + price).PadLeft(10) + available.ToString().PadLeft(12));
        }
    }
    
    private void CancelReservation()
    {
        string reservationId = ConsoleUI.AskWithCancel("Reservation ID");
        if (!Guid.TryParse(reservationId, out _))
        {
            ConsoleUI.Error("Invalid reservation ID.");
            return;
        }
        
        byte[] data = new FieldWriter().WriteString(_username).WriteString(reservationId).ToArray();
        
        Frame request = new Frame {Command = (int)Command.CancelReservation, Data = data};
        FrameIO.Send(_socketHelper, request);
        Frame response = FrameIO.Receive(_socketHelper);
        
        FieldReader reader = new FieldReader(response.Data);
        StatusCode status = (StatusCode)reader.ReadInt();
        string mesage = reader.ReadText();
        ConsoleUI.ShowResponse(status, mesage);
    }
    
    private void CheckStatus()
    {
        byte[] data = new FieldWriter().WriteString(_username).ToArray();

        Frame request = new Frame{Command = (int)Command.CheckReservationStatus, Data = data};
        FrameIO.Send(_socketHelper, request);
        Frame response = FrameIO.Receive(_socketHelper);
        
        FieldReader reader = new FieldReader(response.Data);
        StatusCode status = (StatusCode)reader.ReadInt();
        if (status != StatusCode.Ok)
        {
            ConsoleUI.ShowResponse(status, reader.ReadText());
            return;
        }
        
        int count = reader.ReadInt();
        ConsoleUI.Title("Your reservations");

        if (count == 0)
        {
            ConsoleUI.Info($"No reservations found.");
            return;
        }
        
        Console.WriteLine(" " + "MODEL".PadRight(18) + "STATUS".PadRight(12) + "CREATED".PadRight(21) + "UPDATED".PadRight(21));
        ConsoleUI.Separator();
        for (int i = 0; i < count; i++)
        {
            string id = reader.ReadText();
            string modelName = reader.ReadText();
            string createdAt = reader.ReadText();
            string reservationStatus = reader.ReadText();
            string updatedAt = reader.ReadText();
            
            Console.WriteLine(" " + modelName.PadRight(18) + reservationStatus.PadRight(12) + createdAt.PadRight(21) + updatedAt.PadRight(21));
            ConsoleUI.Info($" id: {id}");
        }
        ConsoleUI.Info($"{count} reservation(s) found.");
    }
    
}