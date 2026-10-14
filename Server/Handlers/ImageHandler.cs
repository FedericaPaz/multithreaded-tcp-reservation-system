using Common;
using Common.File;
using Common.Models;
using Common.Protocol;
using Server.Repositories;
using static Common.Protocol.StatusCode;

namespace Server.Handlers;

public class ImageHandler
{
    private static readonly FileHandler _fileHandler = new();
    private static readonly FileStreamHandler _fileStreamHandler = new();

    public static Frame UploadImageChunk(Frame request)
    {
        var reader = new FieldReader(request.Data);
        string username = reader.ReadText();
        string modelName = reader.ReadText();
        string originalFileName = Path.GetFileName(reader.ReadText());
        long totalSize = reader.ReadLong();
        long offset = reader.ReadLong();
        byte[] chunk = reader.ReadBinary();

        Vehicle vehicle = VehicleRepository.Instance.GetVehicle(modelName) ?? throw new EdisonException(NotFound,$"Vehicle {modelName} does not exist.");

        if (!vehicle.CreatorUser.Equals(username, StringComparison.OrdinalIgnoreCase))
            throw new EdisonException(Unauthorized,$"User {username} is not authorized to modify vehicle {modelName}.");

        // el servidor eliuge el nombre con el que se guarda
        string storedName = BuildStoredName(vehicle.ModelName, originalFileName);
        string path = Path.Combine(ImagesDirectory.Path, storedName);
        bool isFirstChunk = offset == 0;
        _fileStreamHandler.Write(path, chunk, isFirstChunk);

        bool isComplete = offset + chunk.Length >= totalSize;
        if (isComplete)
        {
            string? previousImage = vehicle.ImageName;
            VehicleRepository.Instance.SetImage(vehicle.ModelName, storedName);

            // si antes tenía otra imagen con otra extensión (auto.jpg -> auto.png), se borra la vieja
            if (!string.IsNullOrEmpty(previousImage) && previousImage != storedName)
                _fileHandler.DeleteFile(Path.Combine(ImagesDirectory.Path, previousImage));
        }

        byte[] data = new FieldWriter()
            .WriteInt((int)Ok)
            .WriteString(isComplete ? "Image upload complete." : "Chunk received.")
            .ToArray();

        return new Frame { Command = request.Command, Data = data };
    }
    
    private static string BuildStoredName(string modelName, string originalFileName)
    {
        string name = modelName + Path.GetExtension(originalFileName);
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '_');
        }
        return name;
    }
    
    //descargar la imagen de un vehículo, de a pedazos. El cliente empieza pidiendo
    //ffset = 0 y sigue pidiendo el siguiente pedazo hasta cubrir el totalSize devuelto
    //en la primera respuesta.
    
    public static Frame DownloadImageChunk(Frame request)
    {
        var reader = new FieldReader(request.Data);
        string modelName = reader.ReadText();
        long offset = reader.ReadLong();

        Vehicle vehicle = VehicleRepository.Instance.GetVehicle(modelName)
                          ?? throw new EdisonException(NotFound, $"Vehicle {modelName} does not exist.");

        if (string.IsNullOrEmpty(vehicle.ImageName))
            throw new EdisonException(NotFound,$"Vehicle {modelName} has no image.");

        string path = Path.Combine(ImagesDirectory.Path, vehicle.ImageName);
        long totalSize = _fileHandler.GetFileSize(path);

        int chunkLength = (int)Math.Min(FileTransferProtocol.MaxChunkSize, totalSize - offset);
        byte[] chunk = _fileStreamHandler.Read(path, offset, chunkLength);

        byte[] data = new FieldWriter()
            .WriteInt((int)Ok)
            .WriteString(vehicle.ImageName)
            .WriteLong(totalSize)
            .WriteBytes(chunk)
            .ToArray();

        return new Frame { Command = request.Command, Data = data };
    }
}