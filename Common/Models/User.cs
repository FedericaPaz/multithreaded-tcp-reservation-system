using static Common.Protocol.StatusCode;

namespace Common.Models;

public class User
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";

    public void ValidateForRegistration()
    {
        if (string.IsNullOrEmpty(Username))
            throw new EdisonException(BadRequest, "Username is required");
        if (Username.Length > 50)
            throw new EdisonException(BadRequest, "Username can't be longer than 50 characters.");
        if (string.IsNullOrEmpty(Password))
            throw new EdisonException(BadRequest, "Password is required");
    }
}