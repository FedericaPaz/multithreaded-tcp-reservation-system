using System.Text;

namespace Common.Protocol;

/*
    FieldWriter arma el contenido de datos cuando una operacion necesita mandar mas de un dato
    (como por ejemplo, usuario+contra en un login).
    Cada subcampo se escribe a mando con su propio largo-prefijo antes del contenido: [4 digitos de largo, texto][el contenido].
    
    FieldWriter("ana") agrega "0003ana" (4 caracteres de largo + 3 de contenido).
 */

public class FieldWriter
{
    private int _prefixLength = 4;
    private readonly List<byte> _bytes = new();

    public FieldWriter WriteString(string value)
    {
        return WriteBytes(Encoding.UTF8.GetBytes(value));
    }
    
    public FieldWriter WriteInt(int value)
    {
        return WriteString(value.ToString());
    }
    
    public FieldWriter WriteBytes(byte[] content)
    {
        string lengthText = content.Length.ToString().PadLeft(_prefixLength, '0');
        _bytes.AddRange(Encoding.ASCII.GetBytes(lengthText));
        _bytes.AddRange(content);
        return this;
    }
    
    public byte[] ToArray() => _bytes.ToArray();
    
    public FieldWriter WriteLong(long value) => WriteString(value.ToString());
}