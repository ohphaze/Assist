using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Assist.Client.Services;

public sealed class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null)
        {
            return null;
        }

        var viewModelType = data.GetType();
        var assembly = viewModelType.Assembly;
        var viewModelName = viewModelType.FullName;
        if (viewModelName is null)
        {
            return new TextBlock { Text = "View not found" };
        }

        Control? view = null;
        var candidateNames = new[]
        {
            viewModelName.Replace("ViewModels", "Views.Pages").Replace("ViewModel", "View"),
            viewModelName.Replace("ViewModel", "View")
        };

        foreach (var name in candidateNames)
        {
            var type = assembly.GetType(name);
            if (type is null)
            {
                continue;
            }

            view = Activator.CreateInstance(type) as Control;
            if (view is not null)
            {
                break;
            }
        }

        return view ?? new TextBlock { Text = $"Could not locate view for {viewModelName}" };
    }

    public bool Match(object? data) => data is not null;
}
