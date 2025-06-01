using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PasswordManager.ViewModels;
using PasswordManager.Views;
using ReactiveUI;
using Splat;
using Avalonia.ReactiveUI;
using Avalonia.Threading;

namespace PasswordManager
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            
            RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
            Locator.CurrentMutable.RegisterConstant(new AvaloniaActivationForViewFetcher(), typeof(IActivationForViewFetcher));
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new LoginView
                {
                    DataContext = new LoginViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}