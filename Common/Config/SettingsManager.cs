using System.Configuration;
using static Common.Protocol.StatusCode;

namespace Common.Config;

// Reads values from the App.config of whichever project uses it (Server or
// Client). This is what lets IP, port, etc. be configured from the outside
// without recompiling.
public class SettingsManager
{
    public string ReadSetting(string key)
    {
        string? value = ConfigurationManager.AppSettings[key];
        if (string.IsNullOrEmpty(value))
            throw new EdisonException(NotFound, $"No configuration found for key '{key}'.");
        return value;
    }

    public int ReadIntSetting(string key)
    {
        return int.Parse(ReadSetting(key));
    }
}
