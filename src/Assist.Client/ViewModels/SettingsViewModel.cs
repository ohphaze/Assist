using System.Diagnostics;
using System.IO;
using System.Reflection;
using Assist.Core.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Assist.Client.ViewModels;

public sealed partial class SettingsViewModel : ViewModelBase
{
    public string ApplicationVersion { get; } = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "dev";

    public string DataDirectory { get; } = AppDataPaths.GetDataDirectory();

    [RelayCommand]
    private void OpenDataDirectory()
    {
        if (!Directory.Exists(DataDirectory))
        {
            Directory.CreateDirectory(DataDirectory);
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = DataDirectory,
                UseShellExecute = true
            });
        }
        catch
        {
            // ignored intentionally
        }
    }
}
