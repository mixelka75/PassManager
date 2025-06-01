using ReactiveUI;

namespace PasswordManager.ViewModels
{
    public class ViewModelBase : ReactiveObject, IActivatableViewModel
    {
        public ViewModelActivator Activator { get; } = new ViewModelActivator();
    }
}