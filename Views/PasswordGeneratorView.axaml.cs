using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PasswordManager.Views
{
    public partial class PasswordGeneratorView : Window
    {
        public PasswordGeneratorView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}