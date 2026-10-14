using System.Net.Sockets;

namespace Common.Network;

public class SocketHelper
{
    private readonly Socket _socket;
    
    public SocketHelper(Socket socket)
    {
        _socket = socket;
    }

    public void Send(byte[] data)
    {
        /*
        Send() puede mandar menos bytes de los pedidos en una sola
        llamada, por eso repetimos desde donde quedó (en offset) hasta mandar todo el arreglo completo
        */
        
        int offset = 0;
        while (offset < data.Length)
        {
            // me aparece un parametro mas SocketFlags, PREGUNTAR EN CLASE, sin eso se me rompe
            int sent = _socket.Send(data, offset, data.Length - offset, SocketFlags.None);
            if (sent == 0)
                throw new SocketException(); // la conexión se cerro del otro lado
            offset += sent;
        }
    }

    public byte[] Receive(int byteCount)
    {
        byte[] buffer = new byte[byteCount];
        int offset = 0;
        while (offset < byteCount)
        {
            /*
            pasa lo mismo que en el caso de arriba, Receive() puede traer menos bytes de los pedidos,
            hay que seguir pidiendo  hasta llenar el buffer completo
            */
            
            int received = _socket.Receive(buffer, offset, byteCount - offset, SocketFlags.None);
            if (received == 0)
                throw new SocketException();
            offset += received;
        }
        return buffer;
    }
}