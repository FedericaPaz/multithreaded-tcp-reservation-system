using static Common.Protocol.StatusCode;

namespace Common.File;

public class FileStreamHandler
{
    private readonly FileHandler _fileHandler;

    public FileStreamHandler()
    {
        _fileHandler = new FileHandler();
    }

    public byte[] Read(string path, long offset, int length)
    {
        if(!_fileHandler.FileExists(path))
            throw new EdisonException(NotFound, "File not found");
        
        var data = new byte[length];

        FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
        try
        {
            fileStream.Seek(offset, SeekOrigin.Begin);
            int bytesRead = 0;
            while (bytesRead < length)
            {
                int read = fileStream.Read(data, bytesRead, length - bytesRead);
                if (read == 0)
                    throw new EdisonException(Conflict, "Errror reading file");
                bytesRead += read;
            }

            return data;
        }
        finally
        {
            fileStream.Close();
        }
    }

    //el isFisrtChunk es el offset=0
    public void Write(string path, byte[] data, bool isFirstChunk)
    {
        FileMode fileMode = isFirstChunk ? FileMode.Create : FileMode.Append;
        FileStream fileStream = new FileStream(path, fileMode, FileAccess.Write);
        try
        {
            fileStream.Write(data, 0, data.Length);
        }
        finally
        {
            fileStream.Close();
        }
    }
}