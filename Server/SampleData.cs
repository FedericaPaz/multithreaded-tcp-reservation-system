using Common.Models;
using Server.Repositories;

namespace Server;

public class SampleData
{
    public static void Load()
    {
        LoadUsers();
        LoadVehicles();
        LoadReservations();
        LoadHistory();
        Console.WriteLine("Sample data loaded.");
    }

    private static void LoadUsers()
    {
        string[] usernames = { "ana", "luis", "sofia" };
        foreach (string username in usernames)
        {
            UserRepository.Instance.Add(new User { Username = username, Password = "1234" });
        }
    }

    private static void LoadVehicles()
    {
        AddVehicle("Volt S", "Compact city sedan", 24990.0, 30, 5, "ana");
        AddVehicle("Volt X", "Family SUV", 39990.0, 60, 2, "ana");
        AddVehicle("Nova City", "Cheap urban hatchback", 18500.0, 20, 10, "luis");
        AddVehicle("Nova Max", "Long range luxury car", 31000.0, 90, 1, "luis");
        AddVehicle("Ñandú Eléctrico", "Model with special characters", 27000.0, 45, 3, "luis");
        AddVehicle("Sin Reservas", "Model without reservations", 15000.0, 15, 4, "ana");
    }

    private static void AddVehicle(string model, string description, double price, int daysToDelivery, int stock, string creator)
    {
        VehicleRepository.Instance.AddVehicle(new Vehicle
        {
            ModelName = model,
            Description = description,
            Price = price,
            AverageDeliveryDate = DateTime.Today.AddDays(daysToDelivery),
            InitialStock = stock,
            CreatorUser = creator
        });
    }

    private static void LoadReservations()
    {
        AddReservation("sofia", "Volt S", ReservationStatus.Confirmed);
        AddReservation("luis", "Volt S", ReservationStatus.Confirmed);
        AddReservation("sofia", "Nova Max", ReservationStatus.Confirmed);
        AddReservation("luis", "Volt X", ReservationStatus.Cancelled);
        AddReservation("ana", "Nova Max", ReservationStatus.Rejected);
    }

    private static void AddReservation(string username, string model, ReservationStatus status)
    {
        ReservationRepository.Instance.Add(new Reservation
        {
            Username = username,
            ModelName = model,
            Status = status,
            CreationDate = DateTime.Now.AddMinutes(-30),
            LastUpdatedDate = DateTime.Now.AddMinutes(-20)
        });
    }

    private static void LoadHistory()
    {
        AddEvent("ana", EventType.VehicleCreated, "Created model 'Volt S'.");
        AddEvent("ana", EventType.VehicleCreated, "Created model 'Volt X'.");
        AddEvent("luis", EventType.VehicleCreated, "Created model 'Nova City'.");
        AddEvent("sofia", EventType.Login, "Logged in.");
        AddEvent("sofia", EventType.ReservationRequested, "Requested reservation for 'Volt S'.");
        AddEvent("sofia", EventType.Purchase, "Reservation for 'Volt S' was confirmed.");
    }

    private static void AddEvent(string username, EventType type, string detail)
    {
        HistoryRepository.Instance.Register(new HistoryEvent
        {
            Username = username,
            Type = type,
            Detail = detail,
            Date = DateTime.Now.AddMinutes(-40)
        });
    }
}