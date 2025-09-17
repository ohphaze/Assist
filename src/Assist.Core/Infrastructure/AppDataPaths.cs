using System;
using System.IO;

namespace Assist.Core.Infrastructure;

public static class AppDataPaths
{
    private const string ApplicationFolder = "Assist";

    public static string GetDataDirectory()
    {
        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(basePath))
        {
            basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        if (string.IsNullOrWhiteSpace(basePath))
        {
            basePath = AppContext.BaseDirectory;
        }

        var directory = Path.Combine(basePath, ApplicationFolder);
        Directory.CreateDirectory(directory);
        return directory;
    }

    public static string GetFilePath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be null or whitespace", nameof(fileName));
        }

        var directory = GetDataDirectory();
        return Path.Combine(directory, fileName);
    }
}
