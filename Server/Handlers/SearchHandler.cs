using System.Globalization;
using Common;
using Common.Models;
using Common.Protocol;
using Server.Repositories;
using static Common.Protocol.StatusCode;

namespace Server.Handlers;

public class SearchHandler
{
    public static Frame Search(Frame request)
    {
        var reader = new FieldReader(request.Data);
        string text = reader.ReadText();
        string maxPriceText = reader.ReadText();
        string deliverBeforeDateText = reader.ReadText();
        
        string? filterText = string.IsNullOrEmpty(text) ? null : text;
        double? filterMaxPrice = string.IsNullOrEmpty(maxPriceText) ? null : double.Parse(maxPriceText, CultureInfo.InvariantCulture);
        
        DateTime? deliveryDeadline = null;
        if (!string.IsNullOrEmpty(deliverBeforeDateText))
        {
            if (!DateTime.TryParseExact(deliverBeforeDateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
                throw new EdisonException(BadRequest, "Invalid date format. Use yyyy-MM-dd.");
            deliveryDeadline = parsed;
        }
        
        List<Vehicle> results = VehicleRepository.Instance.Search(filterText, filterMaxPrice, deliveryDeadline);

        return BuildVehicleListResponse(request.Command, results);
    }

    public static Frame GetDetails(Frame request)
    {
        var reader = new FieldReader(request.Data);
        string modelName = reader.ReadText();
        
        Vehicle vehicle = VehicleRepository.Instance.GetVehicle(modelName) ?? throw new EdisonException(NotFound, $"Vehicle {modelName} does not exist.");
        int confirmed = ReservationRepository.Instance.CountConfirmedByModel(modelName);
        
        byte[] data = new FieldWriter()                 
            .WriteInt((int)Ok)
            .WriteString(vehicle.ModelName)
            .WriteString(vehicle.Description)
            .WriteString(vehicle.Price.ToString(CultureInfo.InvariantCulture))
            .WriteString(vehicle.AverageDeliveryDate.ToString("yyyy-MM-dd"))
            .WriteInt(vehicle.InitialStock)
            .WriteInt(vehicle.CalculateAvailability(confirmed))
            .WriteInt(confirmed)
            .WriteString(vehicle.ImageName ?? "")
            .ToArray();
            
        return new Frame {Command = request.Command, Data = data};
    }

    public static Frame ListByCreator(Frame request)
    {
        FieldReader reader = new FieldReader(request.Data);
        string username = reader.ReadText();
        
        List<Vehicle> vehicles = VehicleRepository.Instance.GetByCreator(username);
        
        return BuildVehicleListResponse(request.Command, vehicles);
    }
    
    public static Frame ListWithImage(Frame request)
    {
        List<Vehicle> vehicles = VehicleRepository.Instance.GetWithImage();
        return BuildVehicleListResponse(request.Command, vehicles);
    }
 
    private static Frame BuildVehicleListResponse(int command, List<Vehicle> vehicles)
    {
        FieldWriter writer = new FieldWriter().WriteInt((int)Ok).WriteInt(vehicles.Count);
        foreach (Vehicle vehicle in vehicles)
        {
            int confirmed = ReservationRepository.Instance.CountConfirmedByModel(vehicle.ModelName);
            writer.WriteString(vehicle.ModelName)
                .WriteString(vehicle.Price.ToString(CultureInfo.InvariantCulture))
                .WriteInt(vehicle.CalculateAvailability(confirmed));
        }
        return new Frame { Command = command, Data = writer.ToArray() };
    }
}