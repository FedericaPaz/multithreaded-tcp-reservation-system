using System.Net.Sockets;
using System.Text;
using Common;
using Common.Network;
using Common.Protocol;
using Server.Handlers;
using Server.Repositories;
using static Common.Protocol.StatusCode;

namespace Server;

/*
Gestiona 1 cliente conectado en su propio hilo creado por ServerManager. Mientras el cliente esta conectado, este objeto entra en un bucle de repeticion de mensajes.
Si el cliente se desconecta o pasa un problema el hilo termina por si solo, sin afectar al resto de los clientes.
*/

public class ClientHandler
{
    private readonly Socket _socket;
    private readonly SocketHelper _socketHelper;
    private bool _connected; 
    private string? _loggedUser;

    public ClientHandler(Socket socket)
    {
        _socket = socket;
        _socketHelper = new SocketHelper(socket);
        _connected = true;
    }
    
    public void Accept()
    {
        try
        {
            while (_connected)
            {
                Frame request = FrameIO.Receive(_socketHelper);
                Frame response = Process(request);
                FrameIO.Send(_socketHelper, response);
            }
        }
        catch (SocketException)
        {
            // pasa cuando el cliente cierra la conexion de golpe o cuando Connect() cierra el socket desde afuera 
            Console.WriteLine("Client disconnected.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error handling client: {ex.Message}");
        }
        finally
        {
            _connected = false;
            try { _socket.Close(); } catch {  }
        }
    }
    
    public void Disconnect()
    {
        _connected = false;
        try { _socket.Shutdown(SocketShutdown.Both); } catch {}
        try { _socket.Close(); } catch {  }
    }

    private Frame Process(Frame request)
    {
        try
        {
            Authorize(request);
            
            return (Command)request.Command switch
            {
                Command.Register => AuthHandler.Register(request),
                Command.Login => HandleLogin(request),
                Command.Logout => HandleLogout(request),
                Command.AddModel => VehicleHandler.RegisterModel(request),
                Command.UpdateModel => VehicleHandler.UpdateModel(request),
                Command.DeleteModel => VehicleHandler.DeleteModel(request),
                Command.SearchVehicles => SearchHandler.Search(request),
                Command.GetVehicleDetails => SearchHandler.GetDetails(request),
                Command.RequestReservation => ReservationHandler.RequestReservation(request),
                Command.CancelReservation => ReservationHandler.CancelReservation(request),
                Command.CheckReservationStatus => ReservationHandler.CheckReservationStatus(request),
                Command.UploadImage => ImageHandler.UploadImageChunk(request),
                Command.DownloadImage => ImageHandler.DownloadImageChunk(request),
                Command.ActivityHistory => HistoryHandler.GetHistory(request),
                Command.ListMyVehicles => SearchHandler.ListByCreator(request),
                Command.ListVehiclesWithImage => SearchHandler.ListWithImage(request),
                _ => throw new EdisonException(BadRequest, $"Command {request.Command} not supported.")
            };
        }
        catch (EdisonException ex)
        {
            return ErrorResponse(request.Command, ex.StatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            return ErrorResponse(request.Command, StatusCode.InternalError, ex.Message);
        }
    }
    
    /*
Solo Register y Login se aceptan sin haber iniciado sesion. Para el resto, ademas, si el pedido
trae el nombre de usuario como primer campo, tiene que ser el mismo con el que se logueo esta
conexion: el servidor no confia en el nombre que manda el cliente.
_loggedUser lo toca solo el hilo de este cliente, por eso no necesita lock.
*/
    private void Authorize(Frame request)
    {
        Command command = (Command)request.Command;
        if (command == Command.Register || command == Command.Login)
            return;

        if (_loggedUser == null)
            throw new EdisonException(Unauthorized, "You must log in first.");

        if (CarriesUsername(command))
        {
            string requestUser = new FieldReader(request.Data).ReadText();
            if (!requestUser.Equals(_loggedUser, StringComparison.OrdinalIgnoreCase))
                throw new EdisonException(Forbidden,"The user in the request does not match the logged in user.");
        }
    }

// comandos cuyo primer campo es el usuario que hace la operacion
    private static bool CarriesUsername(Command command)
    {
        return command == Command.Logout
               || command == Command.AddModel
               || command == Command.UpdateModel
               || command == Command.DeleteModel
               || command == Command.RequestReservation
               || command == Command.UploadImage
               || command == Command.CancelReservation
               || command == Command.CheckReservationStatus
               || command == Command.ActivityHistory
               || command == Command.ListMyVehicles;
    }

    private Frame HandleLogin(Frame request)
    {
        Frame response = AuthHandler.Login(request);

        // si login no tiro excepcion, el usuario existe, se guarda con el nombre real no el que se tipeo
        string typedName = new FieldReader(request.Data).ReadText();
        _loggedUser = UserRepository.Instance.Get(typedName)?.Username;
        return response;
    }

    private Frame HandleLogout(Frame request)
    {
        Frame response = AuthHandler.Logout(request);
        _loggedUser = null;
        return response;
    }

    private Frame ErrorResponse(int command, StatusCode statusCode, string message)
    {
        byte[] data = new FieldWriter()
            .WriteInt((int)statusCode)
            .WriteString(message)
            .ToArray();
        
        return new Frame {Command = command, Data = data};
    }
}
