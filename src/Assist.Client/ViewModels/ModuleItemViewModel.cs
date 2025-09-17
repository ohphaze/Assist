using System;
using Assist.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Assist.Client.ViewModels;

public sealed partial class ModuleItemViewModel : ObservableObject
{
    public ModuleItemViewModel(ModuleDefinition definition)
    {
        Id = definition.Id;
        Name = definition.Name;
        Description = definition.Description;
        Category = definition.Category;
        IsEnabled = definition.IsEnabled;
    }

    public Guid Id { get; }

    public string Name { get; }

    public string? Description { get; }

    public ModuleCategory Category { get; }

    [ObservableProperty]
    private bool _isEnabled;
}
