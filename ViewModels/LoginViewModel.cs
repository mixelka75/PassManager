using PasswordManager.Models;
using ReactiveUI;
using System;
using System.Reactive;
using PasswordManager.Views;
using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace PasswordManager.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private string _masterPassword = string.Empty;
        private string _confirmPassword = string.Empty;
        private bool _isNewDatabase = false;
        
        public string MasterPassword
        {
            get => _masterPassword;
            set => this.RaiseAndSetIfChanged(ref _masterPassword, value);
        }
        
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => this.RaiseAndSetIfChanged(ref _confirmPassword, value);
        }
        
        public bool IsNewDatabase
        {
            get => _isNewDatabase;
            set => this.RaiseAndSetIfChanged(ref _isNewDatabase, value);
        }
        
        public ReactiveCommand<Unit, Unit> LoginCommand { get; }
        public ReactiveCommand<Unit, Unit> ToggleNewDatabaseCommand { get; }
        
        public LoginViewModel()
        {
            ToggleNewDatabaseCommand = ReactiveCommand.Create(() => 
            { 
                IsNewDatabase = !IsNewDatabase;
            });
            
            LoginCommand = ReactiveCommand.CreateFromTask(LoginAsync);
            
            var saltPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "PasswordManager", 
                "passwords.db.salt");
            
            IsNewDatabase = !System.IO.File.Exists(saltPath);
            Console.WriteLine($"Salt file exists: {System.IO.File.Exists(saltPath)}, IsNewDatabase: {IsNewDatabase}");
        }
        
        private async Task LoginAsync()
        {
            if (string.IsNullOrEmpty(MasterPassword))
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Ошибка", 
                    "Мастер-пароль не может быть пустым", 
                    ButtonEnum.Ok, 
                    Icon.Error);
                await box.ShowAsync();
                return;
            }
            
            if (IsNewDatabase && MasterPassword != ConfirmPassword)
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Ошибка", 
                    "Пароли не совпадают", 
                    ButtonEnum.Ok, 
                    Icon.Error);
                await box.ShowAsync();
                return;
            }
            
            try
            {
                Console.WriteLine($"Attempting login with password length: {MasterPassword.Length}");
                
                var db = await Task.Run(() => 
                {
                    var database = new PasswordDatabase();
                    var success = database.Initialize(MasterPassword);
                    Console.WriteLine($"Database initialization result: {success}");
                    return success ? database : null;
                });
                
                if (db != null)
                {
                    Console.WriteLine("Database initialized successfully, opening main window");
                    var mainViewModel = new MainWindowViewModel(db);
                    var mainWindow = new MainWindow
                    {
                        DataContext = mainViewModel
                    };
                    
                    if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        var currentWindow = desktop.MainWindow;
                        desktop.MainWindow = mainWindow;
                        mainWindow.Show();
                        currentWindow?.Close();
                    }
                }
                else
                {
                    Console.WriteLine("Database initialization failed");
                    var box = MessageBoxManager.GetMessageBoxStandard(
                        "Ошибка", 
                        "Неверный мастер-пароль", 
                        ButtonEnum.Ok, 
                        Icon.Error);
                    await box.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Ошибка", 
                    $"Произошла ошибка при инициализации базы данных: {ex.Message}", 
                    ButtonEnum.Ok, 
                    Icon.Error);
                await box.ShowAsync();
            }
        }
    }
}