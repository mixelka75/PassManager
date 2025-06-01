using PasswordManager.Models;
using ReactiveUI;
using System;
using System.Reactive;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using System.Threading.Tasks;

namespace PasswordManager.ViewModels
{
    public class PasswordEntryViewModel : ViewModelBase
    {
        private readonly PasswordEntry _entry;
        private readonly PasswordDatabase? _database;
        private readonly MainWindowViewModel? _mainViewModel;
        private readonly bool _isNew;
        
        private string _title;
        private string _username;
        private string _password;
        private string _url;
        private string _notes;
        private string _category;
        
        public string Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }
        
        public string Username
        {
            get => _username;
            set => this.RaiseAndSetIfChanged(ref _username, value);
        }
        
        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }
        
        public string Url
        {
            get => _url;
            set => this.RaiseAndSetIfChanged(ref _url, value);
        }
        
        public string Notes
        {
            get => _notes;
            set => this.RaiseAndSetIfChanged(ref _notes, value);
        }
        
        public string Category
        {
            get => _category;
            set => this.RaiseAndSetIfChanged(ref _category, value);
        }
        
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }
        public ReactiveCommand<Unit, Unit> GeneratePasswordCommand { get; }
        
        public PasswordEntryViewModel()
        {
            _entry = new PasswordEntry 
            { 
                Title = "Example Entry",
                Username = "user@example.com",
                Password = "********",
                Url = "https://example.com",
                Category = "Web",
                Notes = "Example notes"
            };
            _database = null;
            _mainViewModel = null;
            _isNew = true;
            
            _title = _entry.Title;
            _username = _entry.Username;
            _password = _entry.Password;
            _url = _entry.Url;
            _notes = _entry.Notes;
            _category = _entry.Category;
            
            SaveCommand = ReactiveCommand.Create(() => { });
            CancelCommand = ReactiveCommand.Create(() => { });
            GeneratePasswordCommand = ReactiveCommand.Create(() => { });
        }
        
        public PasswordEntryViewModel(PasswordEntry entry, PasswordDatabase database, MainWindowViewModel mainViewModel, bool isNew)
        {
            _entry = entry ?? throw new ArgumentNullException(nameof(entry));
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _isNew = isNew;
            
            _title = entry.Title ?? "";
            _username = entry.Username ?? "";
            _password = entry.Password ?? "";
            _url = entry.Url ?? "";
            _notes = entry.Notes ?? "";
            _category = entry.Category ?? "";
            
            SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync);
            CancelCommand = ReactiveCommand.Create(Cancel);
            GeneratePasswordCommand = ReactiveCommand.Create(GeneratePassword);
        }
        
        private async Task SaveAsync()
        {
            if (_database == null || _mainViewModel == null)
                return;
                
            if (string.IsNullOrWhiteSpace(Title))
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Ошибка", 
                    "Название не может быть пустым", 
                    ButtonEnum.Ok, 
                    Icon.Error);
                await box.ShowAsync();
                return;
            }
            
            try
            {
                await Task.Run(() => 
                {
                    _entry.Title = Title.Trim();
                    _entry.Username = Username?.Trim() ?? "";
                    _entry.Password = Password ?? "";
                    _entry.Url = Url?.Trim() ?? "";
                    _entry.Notes = Notes?.Trim() ?? "";
                    _entry.Category = Category?.Trim() ?? "";
                    _entry.Modified = DateTime.Now;
                    
                    if (_isNew)
                    {
                        _entry.Created = DateTime.Now;
                        _database.AddEntry(_entry);
                    }
                    else
                    {
                        _database.UpdateEntry(_entry);
                    }
                });
                
                _mainViewModel.RefreshData();
                CloseWindow();
            }
            catch (Exception ex)
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Ошибка", 
                    $"Не удалось сохранить запись: {ex.Message}", 
                    ButtonEnum.Ok, 
                    Icon.Error);
                await box.ShowAsync();
            }
        }
        
        private void Cancel()
        {
            CloseWindow();
        }
        
        private void GeneratePassword()
        {
            var viewModel = new PasswordGeneratorViewModel();
            viewModel.PasswordGenerated += (sender, password) =>
            {
                Password = password;
            };
            
            var window = new Views.PasswordGeneratorView
            {
                DataContext = viewModel
            };
            
            if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow != null)
                {
                    window.ShowDialog(desktop.MainWindow);
                }
            }
        }
        
        private void CloseWindow()
        {
            if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var windows = desktop.Windows;
                foreach (var window in windows)
                {
                    if (window.DataContext == this)
                    {
                        window.Close();
                        break;
                    }
                }
            }
        }
    }
}