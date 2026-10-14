using static Common.Protocol.StatusCode;

namespace Common.File;

public class FileHandler
{
    public bool FileExists(string path) => System.IO.File.Exists(path);

    public string GetFileName(string path)
    {
        if (!FileExists(path))
            throw new EdisonException(NotFound, "File does not exist");
        return new FileInfo(path).Name;
    }

    public long GetFileSize(string path)
    {
        if (!FileExists(path))
            throw new EdisonException(NotFound,"File does not exist");
        return new FileInfo(path).Length;
    }
    
    public void DeleteFile(string path)
    {
        if (FileExists(path))
            System.IO.File.Delete(path);
    }
}