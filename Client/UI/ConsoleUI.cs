using System.Globalization;
using Common.Protocol;

namespace Client.UI;

public class ConsoleUI
{
    private const int Width = 50;
    private const string DateFormat = "yyyy-MM-dd";
    public const string CancelKeyword = "c";

    public static void Title(string text)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', Width));
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  " + text.ToUpper());
        Console.ResetColor();
        Console.WriteLine(new string('=', Width));
    }

    public static void MenuOption(string number, string text)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"  [{number}] ");
        Console.ResetColor();
        Console.WriteLine(text);
    }
    
    public static void Separator() => Console.WriteLine(new string('-', Width));
    
    // muestra la respuesta del servidor: verde si salio bien, rojo si fue error
    public static void ShowResponse(StatusCode status, string message)
    {
        if (status == StatusCode.Ok)
            Success(message);
        else
            WriteColored($"  [{(int)status}] {status}: {message}", ConsoleColor.Red);
    }

    public static void Success(string message) => WriteColored($"  [OK] {message}", ConsoleColor.Green);
    
    public static void Error(string message) => WriteColored($"  [ERROR] {message}", ConsoleColor.Red);
    
    public static void Info(string message) => WriteColored($"  {message}", ConsoleColor.DarkGray);

    private static void WriteColored(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }
    
    public static string Ask(string label)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write($"  {label}: ");
        Console.ResetColor();
        return Console.ReadLine() ?? "";
    }
    
    public static string AskWithCancel(string label)
    {
        WritePrompt(label, $" ['{CancelKeyword}' to cancel]");
        string input = Console.ReadLine() ?? "";

        if (input.Trim().Equals(CancelKeyword, StringComparison.OrdinalIgnoreCase))
            throw new OperationCanceledException();

        return input;
    }
    
    private static void WritePrompt(string label, string hint)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"  {label}");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(hint);
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(": ");
        Console.ResetColor();
    }

    // contraseña sin mostrar lo que se escribe
    public static string AskPassword(string label)
    {
        Console.Write($"  {label}: ");
        string password = "";
        bool finished = false;

        while (!finished)
        {
            ConsoleKeyInfo key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
                finished = true;
            else if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0)
                    password = password.Substring(0, password.Length - 1);
            }
            else
                password += key.KeyChar;
        }
        Console.WriteLine();
        return password;
    }
    
    public static string AskRequired(string label) =>
        AskValidated(label, input => !string.IsNullOrWhiteSpace(input), "This field is required.");
    
    public static string AskPrice(string label)
    {
        while (true)
        {
            string input = AskWithCancel(label);
            if (!double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out double price))
                Error("Invalid price. Use a number with a dot for decimals, for example 24990.50.");
            else if (price < 0)
                Error("The price must be greater than or equal to 0.");
            else
                return input;
        }
    }
    
    public static string AskStock(string label)
    {
        string input = "";
        bool valid = false;
        while (!valid)
        {
            input = AskWithCancel(label);
            valid = int.TryParse(input, out int value) && value > 0;
            if (!valid)
                Error("Invalid stock. Must be a positive whole number.");
        }
        return input;
    }

    public static string AskDate(string label)
    {
        while (true)
        {
            string input = AskWithCancel($"{label} ({DateFormat})");
            if (!TryParseDate(input, out DateTime date))
                Error($"Invalid date. Use the format {DateFormat} (e.g. 2026-12-31).");
            else if (date < DateTime.Today)
                Error("The delivery date can't be in the past.");
            else
                return input;
        }
    }
    
    // fecha opcional para filtros: vacia o con formato valido (puede ser pasada)
    public static string AskOptionalDate(string label)
    {
        while (true)
        {
            string input = AskWithCancel(label);
            if (string.IsNullOrWhiteSpace(input) || TryParseDate(input, out _))
                return input;
            Error($"Invalid date. Use the format {DateFormat} (e.g. 2026-12-31), or leave it empty to skip.");
        }
    }
    
    // pide un dato hasta que isValid lo acepte. Enter vacio cancela y devuelve null.
    public static string AskUntilValid(string label, Func<string, bool> isValid, string errorMessage)
    {
        while (true)
        {
            string input = AskWithCancel(label);
            if (isValid(input))
                return input;
            Error(errorMessage);
        }
    }
    
    // bucle comun: repite la pregunta hasta que el dato sea valido
    private static string AskValidated(string label, Func<string, bool> isValid, string errorMessage)
    {
        while (true)
        {
            string input = AskWithCancel(label);
            if (isValid(input))
                return input;
            Error(errorMessage);
        }
    }

    private static bool TryParseDate(string input, out DateTime date) =>
        DateTime.TryParseExact(input, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}