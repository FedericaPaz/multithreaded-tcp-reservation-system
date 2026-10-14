using System.Net.Sockets;
using Common.Network;
using Common.Protocol;

namespace Client.UI;

public class LoginMenu
{
    private readonly SocketHelper _socketHelper;

    public string LoggedInUser { get; set; } = "";
    
    public LoginMenu(Socket socket)
    {
        _socketHelper = new SocketHelper(socket);
    }

    public bool Show()
    {
        bool isLoggedIn = false;
        bool exit = false;

        while (!isLoggedIn && !exit)
        {
            ConsoleUI.Title("EDISON - Login Menu");
            ConsoleUI.MenuOption("1", "Register");
            ConsoleUI.MenuOption("2", "Login");
            ConsoleUI.MenuOption("3", "Exit");
            string option = ConsoleUI.Ask("Option");

            switch (option)
            {
                case "1":
                    SendRegisterOrLogin(Command.Register);
                    break;
                case "2":
                    isLoggedIn = SendRegisterOrLogin(Command.Login);
                    break;
                case "3":
                    exit =  true;
                    break;
                default:
                    ConsoleUI.Error("Invalid option");
                    break;
            }
        }
        return isLoggedIn;
    }

    private bool SendRegisterOrLogin(Command command)
    {
        //Console.WriteLine("Username: ");
        //string username = Console.ReadLine() ?? "";
        //Console.WriteLine("Password: ");
        //string password = Console.ReadLine() ?? "";
        
        string username = ConsoleUI.Ask("Username");
        string password = ConsoleUI.AskPassword("Password");
        
        byte[] data = new FieldWriter()
            .WriteString(username)
            .WriteString(password)
            .ToArray();

        var request = new Frame { Command = (int)command, Data = data };
        FrameIO.Send(_socketHelper, request);
        
        Frame response = FrameIO.Receive(_socketHelper);

        var reader = new FieldReader(response.Data);
        StatusCode status = (StatusCode)reader.ReadInt();
        string message = reader.ReadText();
        
        ConsoleUI.ShowResponse(status, message);

        if (status == StatusCode.Ok && command == Command.Login)
            LoggedInUser = username;
        
        return status == StatusCode.Ok;
    }
}