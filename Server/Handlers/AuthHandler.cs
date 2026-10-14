using Common;
using Common.Models;
using Common.Protocol;
using Server.Repositories;
using static Common.Protocol.StatusCode;

namespace Server.Handlers;

public class AuthHandler
{
    public static Frame Register(Frame frame)
    {
        var reader = new FieldReader(frame.Data);
        string userName = reader.ReadText();
        string password = reader.ReadText();
        
        UserRepository.Instance.Add(new User
        {
            Username = userName,
            Password = password,
        });
        
        return GetOkResponse(frame.Command, "User successfully registered.");
    }

    public static Frame Login(Frame frame)
    {
        var reader = new FieldReader(frame.Data);
        string userName = reader.ReadText();
        string password = reader.ReadText();

        User? user = UserRepository.Instance.Get(userName);
        if (user == null || password != user.Password)
            throw new EdisonException(Unauthorized, "Invalid username or password.");

        HistoryRepository.Instance.Register(new HistoryEvent
        {
            Username = user.Username,
            Type = EventType.Login,
            Detail = "Logged in."
        });

        return GetOkResponse(frame.Command, $"Welcome, {user.Username}!");
    }

    public static Frame Logout(Frame frame)
    {
        return GetOkResponse(frame.Command, "User successfully logged out.");
    }
    
    public static Frame GetOkResponse(int command, string message)
    {
        byte[] data = new FieldWriter()
            .WriteInt((int)StatusCode.Ok)
            .WriteString(message)
            .ToArray();

        return new Frame {Command = command, Data = data};
    }
}