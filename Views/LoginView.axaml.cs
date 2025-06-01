using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PasswordManager.Views
{
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}