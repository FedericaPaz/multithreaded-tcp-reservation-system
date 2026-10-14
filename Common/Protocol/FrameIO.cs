using Common.Network;

namespace Common.Protocol;

/*
    FrameIO envia/recive una trama completa, usando SocketHelper (el send/receive con offset que garantiza
    que los bytes lleguen completos).
    Send() manda el encabezado y datos en 2 llamadas separadas a SocketHelper. 
    Receive() lee en 2 pasos porque no sabe de antemano cuando mide el mensaje completo. Primero lee los 6
    bytes fijos del encabezado, y con el valor de largo ya conocifo, pide exactamente esa csntidad de bytes mas.
 */

public class FrameIO
{
    public static void Send(SocketHelper socketHelper, Frame frame)
    {
        socketHelper.Send(frame.HeaderToBytes());
        socketHelper.Send(frame.Data);
    }

    public static Frame Receive(SocketHelper socketHelper)
    {
        byte[] header = socketHelper.Receive(Frame.HeaderLength);
        Frame frame = Frame.FromHeader(header);
        
        if(frame.Data.Length > 0)
        {
            frame.Data = socketHelper.Receive(frame.Data.Length);
        }
        return frame;
    }
}