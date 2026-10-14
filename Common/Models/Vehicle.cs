using static Common.Protocol.StatusCode;

namespace Common.Models;

public class Vehicle
{
    public string ModelName { get; set; } = "";
    public string? Description { get; set; } = "";
    public double Price { get; set; }
    public DateTime AverageDeliveryDate { get; set; }
    public int InitialStock { get; set; }
    public string? ImageName { get; set; } 
    public string CreatorUser { get; set; } = "";

    public void ValidForRegistration()
    {
        if (string.IsNullOrWhiteSpace(ModelName))
            throw new EdisonException(BadRequest, "The name model is required.");
        if (ModelName.Length > 100)
            throw new EdisonException(BadRequest, "The model name can't be longer than 100 characters.");
        if (Price < 0)
            throw new EdisonException(BadRequest, "The price must be a positive number.");
        if (InitialStock <= 0)
            throw new EdisonException(BadRequest, "The stock must be a positive number.");
        if (AverageDeliveryDate < DateTime.Today)
            throw new EdisonException(BadRequest, "The delivery date can't be in the past.");
    }

    public int CalculateAvailability(int confirmedReservations)
    {
        return InitialStock -  confirmedReservations;
    }
}