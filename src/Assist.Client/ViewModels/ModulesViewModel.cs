using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Assist.Core.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Assist.Client.ViewModels;

public sealed partial class ModulesViewModel : ViewModelBase
{
    private readonly IModuleService _moduleService;

    public ModulesViewModel(IModuleService moduleService)
    {
        _moduleService = moduleService;
    }

    public ObservableCollection<ModuleItemViewModel> Modules { get; } = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _error;

    public override async Task OnNavigatedToAsync()
    {
        await base.OnNavigatedToAsync();
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            Error = null;
            Modules.Clear();

            var modules = await _moduleService.GetModulesAsync().ConfigureAwait(false);
            foreach (var module in modules)
            {
                Modules.Add(new ModuleItemViewModel(module));
            }
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ToggleModuleAsync(ModuleItemViewModel module)
    {
        var desiredState = module.IsEnabled;
        try
        {
            await _moduleService.SetModuleStateAsync(module.Id, desiredState).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            module.IsEnabled = !desiredState;
        }
    }
}
