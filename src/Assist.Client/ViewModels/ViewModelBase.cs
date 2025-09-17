using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Assist.Client.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    public virtual Task OnNavigatedToAsync() => Task.CompletedTask;

    public virtual Task OnNavigatedFromAsync() => Task.CompletedTask;
}
