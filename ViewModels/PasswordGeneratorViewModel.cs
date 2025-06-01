using ReactiveUI;
using System;
using System.Reactive;
using PasswordManager.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using System.Threading.Tasks;

namespace PasswordManager.ViewModels
{
    public class PasswordGeneratorViewModel : ViewModelBase
    {
        private int _passwordLength = 12;
        private bool _useUppercase = true;
        private bool _useLowercase = true;
        private bool _useNumbers = true;
        private bool _useSpecial = true;
        private string _generatedPassword = "";
        
        public int PasswordLength
        {
            get => _passwordLength;
            set => this.RaiseAndSetIfChanged(ref _passwordLength, value);
        }
        
        public bool UseUppercase
        {
            get => _useUppercase;
            set => this.RaiseAndSetIfChanged(ref _useUppercase, value);
        }
        
        public bool UseLowercase
        {
            get => _useLowercase;
            set => this.RaiseAndSetIfChanged(ref _useLowercase, value);
        }
        
        public bool UseNumbers
        {
            get => _useNumbers;
            set => this.RaiseAndSetIfChanged(ref _useNumbers, value);
        }
        
        public bool UseSpecial
        {
            get => _useSpecial;
            set => this.RaiseAndSetIfChanged(ref _useSpecial, value);
        }
        
        public string GeneratedPassword
        {
            get => _generatedPassword;
            set => this.RaiseAndSetIfChanged(ref _generatedPassword, value);
        }
        
        public ReactiveCommand<Unit, Unit> GenerateCommand { get; }
        public ReactiveCommand<Unit, Unit> ApplyCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }
        
        public event EventHandler<string>? PasswordGenerated;
        
        public PasswordGeneratorViewModel()
        {
            GenerateCommand = ReactiveCommand.CreateFromTask(GeneratePasswordAsync);
            ApplyCommand = ReactiveCommand.Create(Apply);
            CancelCommand = ReactiveCommand.Create(Cancel);
            
            _ = GeneratePasswordAsync();
        }
        
        private async Task GeneratePasswordAsync()
        {
            if (!UseUppercase && !UseLowercase && !UseNumbers && !UseSpecial)
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Предупреждение", 
                    "Необходимо выбрать хотя бы один тип символов", 
                    ButtonEnum.Ok, 
                    Icon.Warning);
                await box.ShowAsync();
                
                UseLowercase = true;
                UseUppercase = true;
                UseNumbers = true;
            }
            
            try
            {
                var password = await Task.Run(() => 
                    EncryptionService.GeneratePassword(
                        PasswordLength, 
                        UseUppercase, 
                        UseLowercase, 
                        UseNumbers, 
                        UseSpecial));
                
                GeneratedPassword = password;
            }
            catch (Exception ex)
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Ошибка", 
                    $"Не удалось сгенерировать пароль: {ex.Message}", 
                    ButtonEnum.Ok, 
                    Icon.Error);
                await box.ShowAsync();
            }
        }
        
        private void Apply()
        {
            if (!string.IsNullOrEmpty(GeneratedPassword))
            {
                PasswordGenerated?.Invoke(this, GeneratedPassword);
            }
            CloseWindow();
        }
        
        private void Cancel()
        {
            CloseWindow();
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