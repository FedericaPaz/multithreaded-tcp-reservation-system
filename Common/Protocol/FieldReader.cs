using System.Text;

namespace Common.Protocol;

/*
    lee los subcampos de datos en el mismo orden que se escriben.
    Mantiene un puntero _pos que avanza cada vez que se lee un
    campo. Si del otro lado se escribió con WriteText(usuario).WriteText(contraseña), acá hay que leer en ese
    mismo orden: ReadText() (usuario) y después ReadText() (contraseña)
 */

public class FieldReader
{
    private const int _prefixLength = 4;
    private readonly byte[] _data;
    private int _pos; 

    public FieldReader(byte[] datos)
    {
        _data = datos;
        _pos = 0;
    }

    public string ReadText()
    {
        return Encoding.UTF8.GetString(ReadBinary()); 
    }

    public int ReadInt()
    {
        return int.Parse(ReadText());
    }

    public byte[] ReadBinary()
    {
        int length = ReadPrefixLength();
        byte[] value = new byte[length];
        for (int i = 0; i < length; i++)
        {
            value[i] = _data[_pos++];
        }
        return value;
    }

    private int ReadPrefixLength()
    {
        string lengthText = Encoding.ASCII.GetString(_data, _pos, _prefixLength);
        _pos += _prefixLength;
        return int.Parse(lengthText);
    }
    
    public long ReadLong() => long.Parse(ReadText());
}