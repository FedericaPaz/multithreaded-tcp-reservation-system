using Common;
using Common.Models;
using static Common.Protocol.StatusCode;

namespace Server.Repositories;

public class VehicleRepository
{
    private static readonly Lazy<VehicleRepository> _instance = new(() => new VehicleRepository());
    public static VehicleRepository Instance => _instance.Value;

    private readonly List<Vehicle> _vehicles = new();
    private readonly object _lock = new();
    
    private VehicleRepository() {}

    public bool Exists(string modelName)
    {
        lock (_lock)
        {
            return _vehicles.Any(v => v.ModelName.Equals(modelName, StringComparison.OrdinalIgnoreCase));
        }
    }

    public Vehicle? GetVehicle(string modelName)
    {
        lock (_lock)
        {
            return _vehicles.FirstOrDefault(v => v.ModelName.Equals(modelName, StringComparison.OrdinalIgnoreCase));
        }
    }

    public List<Vehicle> GetAllVehicles()
    {
        lock (_lock)
        {
            return new List<Vehicle>(_vehicles);
        }
    }

    public void AddVehicle(Vehicle vehicle)
    {
        lock (_lock)
        {
            vehicle.ValidForRegistration();
            if(Exists(vehicle.ModelName))
                throw new EdisonException(Conflict, $"Vehicle {vehicle.ModelName} already exists.");
            _vehicles.Add(vehicle);
        }
    }

    public void RemoveVehicle(string modelName)
    {
        lock (_lock)
        {
            _vehicles.RemoveAll(v => v.ModelName.Equals(modelName , StringComparison.OrdinalIgnoreCase));
        }
    }
    
    public void ModifyVehicle(Vehicle updatedVehicle, int confirmedReservations)
    {
        lock (_lock)
        {
            Vehicle existing = GetVehicle(updatedVehicle.ModelName) 
                               ?? throw new EdisonException(NotFound, "No vehicle exists with that name.");
           
            if (existing == null)
                throw new EdisonException(NotFound,"No vehicle exists with that name.");

            updatedVehicle.ValidForRegistration();
            
            if (updatedVehicle.InitialStock < confirmedReservations)
                throw new EdisonException(Conflict,"The new stock can't be lower than the number of already confirmed reservations.");

            existing.Description = updatedVehicle.Description;
            existing.Price = updatedVehicle.Price;
            existing.AverageDeliveryDate = updatedVehicle.AverageDeliveryDate;
            existing.InitialStock = updatedVehicle.InitialStock;
            //existing.ImageName = updatedVehicle.ImageName;
        }
    }
    
    public List<Vehicle> Search(string? text = null, double? maxPrice = null, DateTime? deliveryBefore = null)
    {
        lock (_lock)
        {
            return _vehicles.Where(v => (text == null || v.ModelName.Contains(text, StringComparison.OrdinalIgnoreCase) || (v.Description?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false))
                && (maxPrice == null || v.Price <= maxPrice) 
                && (deliveryBefore == null || v.AverageDeliveryDate <= deliveryBefore)).ToList();
        }
    }
    
    public void SetImage(string modelName, string imageName)
    {
        lock (_lock)
        {
            Vehicle existing = GetVehicle(modelName) ?? throw new EdisonException(NotFound,"No vehicle exists with that name.");
            existing.ImageName = imageName;
        }
    }

    public List<Vehicle> GetByCreator(string username)
    {
        lock (_lock)
        {
            return _vehicles.Where(v => v.CreatorUser.Equals(username, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }
    
    public List<Vehicle> GetWithImage()
    {
        lock (_lock)
        {
            return _vehicles.Where(v => !string.IsNullOrEmpty(v.ImageName)).ToList();
        }
    }
}