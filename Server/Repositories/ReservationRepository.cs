using Common.Models;
using Common;
using static Common.Protocol.StatusCode;

namespace Server.Repositories;

public class ReservationRepository
{
    private static readonly Lazy<ReservationRepository> _instance = new(() => new ReservationRepository());
    public static ReservationRepository Instance => _instance.Value;
    
    private readonly List<Reservation> _reservations = new();
    private readonly object _lock = new();
    
    private ReservationRepository(){}

    public void Add(Reservation reservation)
    {
        lock (_lock)
        { 
            _reservations.Add(reservation);
        }
    }

    public Reservation? Get(Guid id)
    {
        lock (_lock)
        {
            return _reservations.FirstOrDefault(r => r.Id == id);
        }
    }

    public List<Reservation> GetReservationsByUser(string userName)
    {
        lock (_lock)
        {
            return _reservations.Where(r => r.Username.Equals(userName, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }

    public int CountConfirmedByModel(string modelName)
    {
        lock (_lock)
        {
            return _reservations.Count(r => r.ModelName.Equals(modelName, StringComparison.OrdinalIgnoreCase) && r.Status == ReservationStatus.Confirmed);
        }
    }

    public bool HasActiveByModel(string modelName)
    {
        lock (_lock)
        {
            return _reservations.Any(r => r.ModelName.Equals(modelName, StringComparison.OrdinalIgnoreCase) && (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed));
        }
    }

    public void Resolve(Guid id, int initialStock)
    {
        lock (_lock)
        {
            Reservation? reservation = _reservations.FirstOrDefault(r => r.Id == id);
            if (reservation == null || reservation.Status != ReservationStatus.Pending)
                return; // ya la cancelaron mientras esperab, no hay nada que resolver

            int confirmed = _reservations.Count(r => r.ModelName.Equals(reservation.ModelName, StringComparison.OrdinalIgnoreCase) && r.Status == ReservationStatus.Confirmed);
           
            if (confirmed < initialStock) 
                reservation.Status = ReservationStatus.Confirmed;
            else 
                reservation.Status = ReservationStatus.Rejected;
            
            reservation.LastUpdatedDate = DateTime.Now;
        }
    }

    public void Cancel(Guid id, string userName)
    {
        lock (_lock)
        {
            Reservation reservation = _reservations.FirstOrDefault(r => r.Id == id) ?? throw new EdisonException(NotFound, "No reservation found with the given ID.");

            if (!reservation.Username.Equals(userName, StringComparison.OrdinalIgnoreCase))
                throw new EdisonException(Forbidden, "You can only cancel your own reservations.");

            if (reservation.Status == ReservationStatus.Rejected)
                throw new EdisonException(Conflict,"This reservation has already been rejected and cannot be canceled.");
            
            if (reservation.Status == ReservationStatus.Cancelled)
                throw new EdisonException(Conflict, "This reservation has already been cancelled.");

            reservation.Status = ReservationStatus.Cancelled;
            reservation.LastUpdatedDate = DateTime.Now;
        }
    }
}