using System.Globalization;
using System.Xml;
using Common;
using Common.Models;
using Common.Protocol;
using Server.Repositories;
using Common.File;
using static Common.Protocol.StatusCode;

namespace Server.Handlers;

public static class VehicleHandler
{
    public static Frame RegisterModel(Frame request)
    {
        var reader = new FieldReader(request.Data);
        string createdBy = reader.ReadText();
        string modelName = reader.ReadText();
        string description = reader.ReadText();
        double price = double.Parse(reader.ReadText(), CultureInfo.InvariantCulture);
        DateTime averageDeliveryDate = DateTime.ParseExact(reader.ReadText(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
        int initialStock = reader.ReadInt();
        
        VehicleRepository.Instance.AddVehicle(new Vehicle
        {
            ModelName =  modelName,
            Description = description,
            Price = price,
            AverageDeliveryDate = averageDeliveryDate,
            InitialStock = initialStock,
            CreatorUser = createdBy
        } );
        
        RegisterEvent(createdBy, EventType.VehicleCreated, $"Created model '{modelName}'.");

        return GetOkResponse(request.Command, $"Vehicle {modelName} successfully registered.");
    }
    
    public static Frame UpdateModel(Frame request)
    {
        var reader = new FieldReader(request.Data);
        string createdBy = reader.ReadText();
        string modelName = reader.ReadText();
        string description = reader.ReadText();
        double price = double.Parse(reader.ReadText(), CultureInfo.InvariantCulture);
        DateTime averageDeliveryDate = DateTime.ParseExact(reader.ReadText(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
        int newStock = reader.ReadInt();
        
        Vehicle existing = VehicleRepository.Instance.GetVehicle(modelName) ?? throw new EdisonException(NotFound, $"Vehicle {modelName} does not exist.");
        
        if (!existing.CreatorUser.Equals(createdBy, StringComparison.OrdinalIgnoreCase))
            throw new EdisonException(Unauthorized, $"User {createdBy} is not authorized to modify vehicle {modelName}.");
        
        int confirmedReservations = ReservationRepository.Instance.CountConfirmedByModel(modelName);

        if (existing.CalculateAvailability(confirmedReservations) <= 0)
            throw new EdisonException(Conflict,$"Vehicle {modelName} has no available units, so it can no longer be modified.");
        
        VehicleRepository.Instance.ModifyVehicle(new Vehicle
        {
            ModelName = modelName,
            Description = description,
            Price = price,
            AverageDeliveryDate = averageDeliveryDate,
            InitialStock = newStock,
            CreatorUser = existing.CreatorUser,
        }, confirmedReservations);
        
        RegisterEvent(existing.CreatorUser, EventType.VehicleModified, $"Modified model '{modelName}'.");

        return GetOkResponse(request.Command, $"Vehicle {modelName} successfully updated.");
    }

    public static Frame DeleteModel(Frame request)
    {
        var reader = new FieldReader(request.Data);
        string requestingUser = reader.ReadText();
        string modelName = reader.ReadText();
        
        Vehicle existing = VehicleRepository.Instance.GetVehicle(modelName)  ?? throw new EdisonException(NotFound,"No vehicle exists with that name.");
        
        if (!existing.CreatorUser.Equals(requestingUser, StringComparison.OrdinalIgnoreCase))
            throw new EdisonException(Unauthorized,$"User {requestingUser} is not authorized to delete vehicle {modelName}.");
        
        if(ReservationRepository.Instance.HasActiveByModel(modelName))
            throw new EdisonException(Conflict, $"Vehicle {modelName} has active reservations and cannot be deleted.");
        
        VehicleRepository.Instance.RemoveVehicle(modelName);
        
        if (!string.IsNullOrEmpty(existing.ImageName))
        {
            string imagePath = Path.Combine(ImagesDirectory.Path, existing.ImageName);
            new FileHandler().DeleteFile(imagePath);
        }
        
        RegisterEvent(existing.CreatorUser, EventType.VehicleDeleted, $"Deleted model '{modelName}'.");

        return GetOkResponse(request.Command, $"Vehicle {modelName} successfully deleted.");
    }
    
    private static Frame GetOkResponse(int command, string message)
    {
        byte[] data = new FieldWriter()
            .WriteInt((int)StatusCode.Ok)
            .WriteString(message)
            .ToArray();

        return new Frame {Command = command, Data = data};
    }
    
    private static void RegisterEvent(string username, EventType type, string detail)
    {
        HistoryRepository.Instance.Register(new HistoryEvent
        {
            Username = username,
            Type = type,
            Detail = detail
        });
    }
}