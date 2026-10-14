namespace Common.Models;

public class Reservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = "";
    public string ModelName { get; set; } = "";
    public DateTime CreationDate { get; set; } = DateTime.Now;
    public DateTime LastUpdatedDate { get; set; } = DateTime.Now;
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
}