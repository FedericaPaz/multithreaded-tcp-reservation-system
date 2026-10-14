namespace Common.File;

public static class FileTransferProtocol
{
    public const int MaxChunkSize = 9000; //posibles bytes de sobrecarga

    public static long CalculateChunkCount(long fileSize)
    {
        if (fileSize == 0) return 1; //un archivo vacio necesita al menos la ultima parte
        long chunks = fileSize / MaxChunkSize;
        return chunks *  MaxChunkSize == fileSize ? chunks : chunks+1; 
    }
}