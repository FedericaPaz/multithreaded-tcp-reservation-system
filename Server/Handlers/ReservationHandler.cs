using Common;
using Common.Protocol;
using Common.Models;
using Server.Repositories;
using static Common.Protocol.StatusCode;

namespace Server.Handlers;

public class ReservationHandler
{
    public static Frame RequestReservation(Frame request)
    {
        var reader = new FieldReader(request.Data);
        string username = reader.ReadText();
        string modelName = reader.ReadText();

        Vehicle vehicle = VehicleRepository.Instance.GetVehicle(modelName) ??
                          throw new EdisonException(NotFound, $"Vehicle {modelName} does not exist.");

        var reservation = new Reservation
        {
            Username = username,
            ModelName = vehicle.ModelName
        };
        
        ReservationRepository.Instance.Add(reservation);
        
        HistoryRepository.Instance.Register(new HistoryEvent
        {
            Username = username,
            Type = EventType.ReservationRequested,
            Detail = $"Requested reservation {reservation.Id} for {vehicle.ModelName}"
        });
        
        ReservationScheduler.Schedule(reservation);

        byte[] data = new FieldWriter().WriteInt((int)Ok).WriteString(reservation.Id.ToString())
            .WriteString($"Reservation request received for {vehicle.ModelName}. It will be processed shortly.")
            .ToArray();
        
        return new Frame{Command = request.Command, Data = data};
    }

    public static Frame CancelReservation(Frame request)
    {
        FieldReader reader = new FieldReader(request.Data);
        string username = reader.ReadText();
        Guid reservationId = Guid.Parse(reader.ReadText());
        
        ReservationRepository.Instance.Cancel(reservationId, username);
        
        HistoryRepository.Instance.Register(new HistoryEvent
        {
            Username = username,
            Type = EventType.ReservationCancelled,
            Detail = $"Cancelled reservation {reservationId}"
        });

        byte[] data = new FieldWriter().WriteInt((int)Ok).WriteString("Reservation successfully cancelled").ToArray();
        
        return new Frame{Command = request.Command, Data = data};
    }

    public static Frame CheckReservationStatus(Frame request)
    {
        FieldReader reader = new FieldReader(request.Data);
        string username = reader.ReadText();

        List<Reservation> reservations = ReservationRepository.Instance.GetReservationsByUser(username);

        FieldWriter writer = new FieldWriter().WriteInt((int)Ok).WriteString(reservations.Count.ToString());

        foreach (Reservation reservation in reservations)
        {
            writer.WriteString(reservation.Id.ToString()).WriteString(reservation.ModelName)
                .WriteString(reservation.CreationDate.ToString("yyyy-MM-dd HH:mm:ss"))
                .WriteString(reservation.Status.ToString())
                .WriteString(reservation.LastUpdatedDate.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        
        return new Frame{Command = request.Command, Data = writer.ToArray()};
    }
}