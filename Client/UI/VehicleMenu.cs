using System.Net.Sockets;
using Common.File;
using Common.Network;
using Common.Protocol;

namespace Client.UI;

public class VehicleMenu
{
    private readonly SocketHelper _socketHelper;
    private readonly string _userName;

    public VehicleMenu(Socket socket, string userName)
    {
        _socketHelper = new SocketHelper(socket);
        _userName = userName;
    }

    public void Show()
    {
        bool goBack = false;
        while (!goBack)
        {
            ConsoleUI.Title("EDISON - Vehicle Menu");
            ConsoleUI.MenuOption("1", "Add vehicle");
            ConsoleUI.MenuOption("2", "Modify vehicle");
            ConsoleUI.MenuOption("3", "Delete vehicle");
            ConsoleUI.MenuOption("4", "Search vehicle");
            ConsoleUI.MenuOption("5", "View vehicle details");
            ConsoleUI.MenuOption("6", "Upload image");
            ConsoleUI.MenuOption("7", "Download image");
            ConsoleUI.MenuOption("8", "Back");
            string option = ConsoleUI.Ask("Option");

            try
            {
                switch (option)
                {
                    case "1": Add(); break;
                    case "2": Modify(); break;
                    case "3": Delete(); break;
                    case "4": Search(); break;
                    case "5": ViewDetails(); break;
                    case "6": UploadImage(); break;
                    case "7": DownloadImage(); break;
                    case "8": goBack = true; break;
                    default: ConsoleUI.Error("Invalid option."); break;
                }
            }
            catch (OperationCanceledException)
            {
                ConsoleUI.Info("Operation cancelled.");
            }
        }
    }

    private void Add()
    {
        string vehicleModel = ConsoleUI.AskUntilValid("Enter vehicle model",
            name => !string.IsNullOrWhiteSpace(name) && !ModelExists(name),
            "The name is empty or that model already exists. Try another name.");
        
        string vehicleDescription = ConsoleUI.AskWithCancel("Enter vehicle description");
        string vehiclePrice = ConsoleUI.AskPrice("Enter vehicle price");
        string deliveryDate = ConsoleUI.AskDate("Expected delivery date");
        string initialStock = ConsoleUI.AskStock("Enter initial stock");
        
        byte[] data = new FieldWriter()
            .WriteString(_userName)
            .WriteString(vehicleModel)
            .WriteString(vehicleDescription)
            .WriteString(vehiclePrice)
            .WriteString(deliveryDate)
            .WriteString(initialStock)
            .ToArray();
        
        SendAndShowSimpleResponse(Command.AddModel, data);
    }

    private void Modify()
    {
        if (!ShowMyVehicles())return;
        
        string nameModel = ConsoleUI.AskWithCancel("Name of model to modify");
        string descriptionModel = ConsoleUI.AskWithCancel("New description");
        string priceModel = ConsoleUI.AskPrice("New price");
        string deliveryDate = ConsoleUI.AskDate("Expected delivery date");
        string stockModel = ConsoleUI.AskStock("New stock");
        
        byte[] data = new FieldWriter()
            .WriteString(_userName)
            .WriteString(nameModel)
            .WriteString(descriptionModel)
            .WriteString(priceModel)
            .WriteString(deliveryDate)
            .WriteString(stockModel)
            .ToArray();
        
        SendAndShowSimpleResponse(Command.UpdateModel, data);
    }

    private void Delete()
    {
        if (!ShowMyVehicles(namesOnly: true))return;
        
        string nameModel = ConsoleUI.AskWithCancel("Name of model to delete");
        
        byte[] data = new FieldWriter()
            .WriteString(_userName)
            .WriteString(nameModel)
            .ToArray();
        
        SendAndShowSimpleResponse(Command.DeleteModel, data);
    }

    private void Search()
    {
        string text = ConsoleUI.AskWithCancel("Text to search (Enter to skip)");
        string price = ConsoleUI.AskWithCancel("Maximum price (Enter to skip)");
        string deliveryDate = ConsoleUI.AskOptionalDate("Deliver before: yyyy-MM-dd (Enter to skip)");
        
        byte[] data = new FieldWriter()
            .WriteString(text)
            .WriteString(price)
            .WriteString(deliveryDate)
            .ToArray();
        
        var request = new Frame { Command = (int)Command.SearchVehicles, Data = data };
        FrameIO.Send(_socketHelper, request);
        Frame response = FrameIO.Receive(_socketHelper);
        
        var reader = new FieldReader(response.Data);
        StatusCode status = (StatusCode)reader.ReadInt();
        if (status != StatusCode.Ok)
        {
            ConsoleUI.ShowResponse(status, reader.ReadText());
            return;
        }

        int amount = reader.ReadInt();
        ConsoleUI.Title("Search results");
        PrintVehicleTable(reader, amount);
        ConsoleUI.Info($"{amount} vehicle(s) found.");
    }
    
    private void ViewDetails()
    {
        if (!ShowAllVehicles(namesOnly: true)) return;
        
        string modelName = ConsoleUI.AskWithCancel("Enter model name");
        
        byte[] data = new FieldWriter().WriteString(modelName).ToArray();
        Frame request = new Frame { Command = (int)Command.GetVehicleDetails, Data = data };
        FrameIO.Send(_socketHelper, request);
        Frame response = FrameIO.Receive(_socketHelper);
        
        var reader = new FieldReader(response.Data);
        StatusCode status = (StatusCode)reader.ReadInt();
        if (status != StatusCode.Ok)
        {
            ConsoleUI.ShowResponse(status, reader.ReadText());
            return;
        }

        string model = reader.ReadText();
        string description = reader.ReadText();
        string price = reader.ReadText();
        string deliveryDate = reader.ReadText();
        int initialStock = reader.ReadInt();
        int available = reader.ReadInt();
        int reserved = reader.ReadInt();
        string image = reader.ReadText();

        ConsoleUI.Title("Vehicle details");
        Console.WriteLine("  " + "FIELD".PadRight(20) + "VALUE");
        ConsoleUI.Separator();
        Console.WriteLine("  " + "Model".PadRight(20) + model);
        Console.WriteLine("  " + "Description".PadRight(20) + description);
        Console.WriteLine("  " + "Price".PadRight(20) + ("$" + price));
        Console.WriteLine("  " + "Delivery date".PadRight(20) + deliveryDate);
        Console.WriteLine("  " + "Initial stock".PadRight(20) + initialStock);
        Console.WriteLine("  " + "Available".PadRight(20) + available);
        Console.WriteLine("  " + "Reserved".PadRight(20) + reserved);
        Console.WriteLine("  " + "Image".PadRight(20) + (string.IsNullOrEmpty(image) ? "(without image)" : image));
    }
    
    private void UploadImage()
    {
        if (!ShowMyVehicles(namesOnly: true))return;
        
        string modelName = ConsoleUI.AskWithCancel("Model name to attach the image to");
        string localPath = ConsoleUI.AskWithCancel("Full path of the image on your machine");
        
        var fileHandler = new FileHandler();
        if (!fileHandler.FileExists(localPath))
        {
            ConsoleUI.Error($"File {localPath} does not exist.");
            return;
        }
        
        string fileName = fileHandler.GetFileName(localPath);
        long totalSize = fileHandler.GetFileSize(localPath);
        var fileStreamHandler = new FileStreamHandler();
        long offset = 0;
        bool finished = false;

        while (!finished)
        {
            int chunkLength = (int)Math.Min(FileTransferProtocol.MaxChunkSize, totalSize - offset);
            byte[] chunk = fileStreamHandler.Read(localPath, offset, chunkLength);

            byte[] data = new FieldWriter()
                .WriteString(_userName)
                .WriteString(modelName)
                .WriteString(fileName)
                .WriteLong(totalSize)
                .WriteLong(offset)
                .WriteBytes(chunk)
                .ToArray();
            
            var request = new Frame { Command = (int)Command.UploadImage, Data = data };
            FrameIO.Send(_socketHelper, request);
            Frame response = FrameIO.Receive(_socketHelper);
            
            var reader = new FieldReader(response.Data);
            StatusCode status = (StatusCode)reader.ReadInt();
            string message = reader.ReadText();
            if (status != StatusCode.Ok)
            {
                ConsoleUI.ShowResponse(status, message);
                return;
            }

            offset += chunkLength;
            finished = offset >= totalSize;
        }
        ConsoleUI.Success("Image uploaded.");
    }
    
    private void DownloadImage()
    {
        if (!ShowVehiclesWithImage()) return;
        
        string modelName = ConsoleUI.AskWithCancel("Model name whose image you want to download");
        if (string.IsNullOrWhiteSpace(modelName))
            return;

        string destinationFolder = ConsoleUI.AskWithCancel("Destination folder on your machine");

        if (!Directory.Exists(destinationFolder))
        {
            ConsoleUI.Error($"Folder {destinationFolder} does not exist.");
            return;
        }

        var fileStreamHandler = new FileStreamHandler();
        long offset = 0;
        string? localPath = null;
        bool finished = false;

        while (!finished)
        {
            byte[] data = new FieldWriter()
                .WriteString(modelName)
                .WriteLong(offset)
                .ToArray();

            var request = new Frame { Command = (int)Command.DownloadImage, Data = data };
            FrameIO.Send(_socketHelper, request);
            Frame response = FrameIO.Receive(_socketHelper);

            var reader = new FieldReader(response.Data);
            StatusCode status = (StatusCode)reader.ReadInt();
            if (status != StatusCode.Ok)
            {
                ConsoleUI.ShowResponse(status, reader.ReadText());
                return;
            }

            string fileName = reader.ReadText();
            long totalSize = reader.ReadLong();
            byte[] chunk = reader.ReadBinary();

            // si el servidor no manda nada pero todavia falta se corta, si no, pediria lo mismo para siempre
            if (chunk.Length == 0 && offset < totalSize)
            {
                ConsoleUI.Error("The server sent an empty chunk before the file was complete.");
                return;
            }

            localPath ??= Path.Combine(destinationFolder, fileName);
            fileStreamHandler.Write(localPath, chunk, isFirstChunk: offset == 0);

            offset += chunk.Length;
            finished = offset >= totalSize;
        }

        ConsoleUI.Success($"Image downloaded to: {localPath}");
    }

    private void SendAndShowSimpleResponse(Command command, byte[] data)
    {
        var request = new Frame { Command = (int)command, Data = data };
        FrameIO.Send(_socketHelper, request);
        Frame response = FrameIO.Receive(_socketHelper);
        
        FieldReader reader = new FieldReader(response.Data);
        StatusCode status = (StatusCode)reader.ReadInt();
        string message = reader.ReadText();
        ConsoleUI.ShowResponse(status, message);
    }
    
    private bool ModelExists(string modelName)
    {
        byte[] data = new FieldWriter().WriteString(modelName).ToArray();
        FrameIO.Send(_socketHelper, new Frame { Command = (int)Command.GetVehicleDetails, Data = data });
        Frame response = FrameIO.Receive(_socketHelper);

        StatusCode status = (StatusCode)new FieldReader(response.Data).ReadInt();
        return status == StatusCode.Ok;
    }
    
    // pide una lista de vehiculos al servidor y la muestra. Devuelve false si no hay ninguno.
    private bool ShowVehicleList(Command command, byte[] data, string title, string emptyMessage, bool namesOnly)
    {
        FrameIO.Send(_socketHelper, new Frame { Command = (int)command, Data = data });
        Frame response = FrameIO.Receive(_socketHelper);

        var reader = new FieldReader(response.Data);
        StatusCode status = (StatusCode)reader.ReadInt();
        if (status != StatusCode.Ok)
        {
            ConsoleUI.ShowResponse(status, reader.ReadText());
            return false;
        }

        int count = reader.ReadInt();
        if (count == 0)
        {
            ConsoleUI.Info(emptyMessage);
            return false;
        }

        ConsoleUI.Title(title);
        if (namesOnly)
            PrintVehicleNames(reader, count);
        else
            PrintVehicleTable(reader, count);
        return true;
    }

    private bool ShowMyVehicles(bool namesOnly = false) =>
        ShowVehicleList(Command.ListMyVehicles, new FieldWriter().WriteString(_userName).ToArray(),
            "Your vehicles", "You haven't created any vehicles yet.", namesOnly);

    private bool ShowAllVehicles(bool namesOnly = false) =>
        ShowVehicleList(Command.SearchVehicles, new FieldWriter().WriteString("").WriteString("").WriteString("").ToArray(),
            "All vehicles", "There are no vehicles registered yet.", namesOnly);

    private bool ShowVehiclesWithImage() =>
        ShowVehicleList(Command.ListVehiclesWithImage, Array.Empty<byte>(),
            "Vehicles with image", "No vehicle has an image yet.", namesOnly: true);
    
    private static void PrintVehicleNames(FieldReader reader, int count)
    {
        for (int i = 0; i < count; i++)
        {
            string modelName = reader.ReadText();
            reader.ReadText(); 
            reader.ReadInt();  
            Console.WriteLine($"  - {modelName}");
        }
    }
    
    // lee [cantidad] y por cada vehiculo [modelo][precio][disponibles], y los muestra en tabla
    private static void PrintVehicleTable(FieldReader reader, int count)
    {
        Console.WriteLine("  " + "MODEL".PadRight(22) + "PRICE".PadLeft(10) + "AVAILABLE".PadLeft(12));
        ConsoleUI.Separator();
        for (int i = 0; i < count; i++)
        {
            string modelName = reader.ReadText();
            string price = reader.ReadText();
            int available = reader.ReadInt();
            Console.WriteLine("  " + modelName.PadRight(22) + ("$" + price).PadLeft(10) + available.ToString().PadLeft(12));
        }
    }
}