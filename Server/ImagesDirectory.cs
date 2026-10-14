using Common.Config;

namespace Server;

public static class ImagesDirectory
{
    public static readonly string Path = Initialize();

    private static string Initialize()
    {
        string path = new SettingsManager().ReadSetting("ImagesDirectory");
        Directory.CreateDirectory(path);
        return path;
    }
}