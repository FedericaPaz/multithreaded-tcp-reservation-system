using Common.Protocol;

namespace Common;

public class EdisonException : Exception
{
    public StatusCode StatusCode { get; }

    public EdisonException(StatusCode statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}