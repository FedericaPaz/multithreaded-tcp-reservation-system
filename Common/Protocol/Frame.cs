using System.Text;

namespace Common.Protocol;

public class Frame
{
    /*
     Trama: el formato de cada mensaje del protocolo CM2(2) + largo(4) + datos(variable)
     Resuelve el problema de message framing, TCP es un chorro continuio de bytes sin limite de mensajes,
     sin un formato acordado, quien lo recibe no sabe donde termina un mensaje, donde empieza el que sigue,
     ni que operacion es.
     CMD es el numero de la operacion, es el mismo numero en el pedido y en la respuesta para poder relacionarlos.
     El largo es cuantos bytes mide datos, asi quien recibe sabe cuanto tiene que leer.
     Datos es el contenido de la operacion, que se arma con el archivo EscritorDeCampos y leido con LectorDeCampos.
     Eliminamos el campo HEADER, la letra daba como sugerencia uno para distinguir pedido de repuesta, pero el profe dijo
     en clase que no hace falta, ya que el servidor siempre sabe que lo que recibe es un pedido, y el cliente siempre sabe
     que recibe una respuesta .
     Los campos viajan como texto (protocolo orientado a caracteres), incluido largo: "0009" en lugar de 4 bytes binarios de un 
     Int32. El maximo de datos por mensaje son 9999 bytes.
     HeaderToBytes() arma el cmd+largo como bytes para mandar. 
     FrmaeIO.Send() los manda en 2 llamadas seoarada, primero encabezado y despues largo.
     FromHeader() hace la primera mitad de la lectura, parsea los 6 bytes fijos para saber cuando mas falta leer. 
     El contenido real de datos se completa en FrameIO.Receive()
     */
    
    // estos son los datos que se piden en el obli 
    public const int CmdLength = 2;
    public const int PayloadLengthWidth = 4;
    public const int HeaderLength = CmdLength + PayloadLengthWidth;
    
    public int Command { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
    
    public byte[] HeaderToBytes()
    {
        if (Data.Length > 9999)
            throw new Exception($"Message data is {Data.Length} bytes, which exceeds the protocol's maximum of 9999 bytes.");

        string cmdText = Command.ToString().PadLeft(CmdLength, '0');
        string lengthText = Data.Length.ToString().PadLeft(PayloadLengthWidth, '0');

        return Encoding.ASCII.GetBytes(cmdText + lengthText);
    }
    
    public static Frame FromHeader(byte[] header)
    {
        if(header.Length != HeaderLength)
            throw new ArgumentException($"Header must be {HeaderLength} bytes long.", nameof(header));

        int command = int.Parse(Encoding.ASCII.GetString(header, 0, CmdLength));
        int dataLength = int.Parse(Encoding.ASCII.GetString(header, CmdLength, PayloadLengthWidth));
        
        return new Frame {Command = command, Data = new byte[dataLength]};
    }
}