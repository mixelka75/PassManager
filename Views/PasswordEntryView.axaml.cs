using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PasswordManager.Views
{
    public partial class PasswordEntryView : Window
    {
        public PasswordEntryView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}