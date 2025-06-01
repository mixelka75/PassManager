using PasswordManager.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive;
using Avalonia.Controls;
using PasswordManager.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using Avalonia.Threading;
using System.Threading.Tasks;
using Avalonia.Input.Platform;

namespace PasswordManager.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly PasswordDatabase? _database;
        private ObservableCollection<PasswordEntry> _entries;
        private ObservableCollection<string> _categories;
        private string _searchText = "";
        private string _selectedCategory = "Все";
        private PasswordEntry? _selectedEntry;
        private bool _isPasswordVisible = false;
        
        public ObservableCollection<PasswordEntry> Entries
        {
            get => _entries;
            set => this.RaiseAndSetIfChanged(ref _entries, value);
        }
        
        public ObservableCollection<string> Categories
        {
            get => _categories;
            set => this.RaiseAndSetIfChanged(ref _categories, value);
        }
        
        public string SearchText
        {
            get => _searchText;
            set
            {
                this.RaiseAndSetIfChanged(ref _searchText, value);
                FilterEntries();
            }
        }
        
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedCategory, value);
                FilterEntries();
            }
        }
        
        public PasswordEntry? SelectedEntry
        {
            get => _selectedEntry;
            set 
            { 
                this.RaiseAndSetIfChanged(ref _selectedEntry, value);
                _isPasswordVisible = false;
                this.RaisePropertyChanged(nameof(PasswordDisplay));
                this.RaisePropertyChanged(nameof(ShowPasswordButtonText));
            }
        }
        
        public string PasswordDisplay => _isPasswordVisible ? (SelectedEntry?.Password ?? "") : new string('•', SelectedEntry?.Password?.Length ?? 0);
        public string ShowPasswordButtonText => _isPasswordVisible ? "🙈" : "👁";
        
        public ReactiveCommand<Unit, Unit> AddNewEntryCommand { get; }
        public ReactiveCommand<Unit, Unit> EditEntryCommand { get; }
        public ReactiveCommand<Unit, Unit> DeleteEntryCommand { get; }
        public ReactiveCommand<Unit, Unit> GeneratePasswordCommand { get; }
        public ReactiveCommand<Unit, Unit> CopyUsernameCommand { get; }
        public ReactiveCommand<Unit, Unit> CopyPasswordCommand { get; }
        public ReactiveCommand<Unit, Unit> CopyUrlCommand { get; }
        public ReactiveCommand<Unit, Unit> CopyNotesCommand { get; }
        public ReactiveCommand<Unit, Unit> TogglePasswordVisibilityCommand { get; }
        
        public MainWindowViewModel()
        {
            _database = null;
            _entries = new ObservableCollection<PasswordEntry>
            {
                new PasswordEntry { Title = "Gmail", Username = "user@gmail.com", Category = "Email", Password = "secretpassword" },
                new PasswordEntry { Title = "Facebook", Username = "user", Category = "Social", Password = "mypassword123" }
            };
            _categories = new ObservableCollection<string> { "Все", "Email", "Social", "Work" };
            
            AddNewEntryCommand = ReactiveCommand.Create(() => { });
            EditEntryCommand = ReactiveCommand.Create(() => { });
            DeleteEntryCommand = ReactiveCommand.Create(() => { });
            GeneratePasswordCommand = ReactiveCommand.Create(() => { });
            CopyUsernameCommand = ReactiveCommand.Create(() => { });
            CopyPasswordCommand = ReactiveCommand.Create(() => { });
            CopyUrlCommand = ReactiveCommand.Create(() => { });
            CopyNotesCommand = ReactiveCommand.Create(() => { });
            TogglePasswordVisibilityCommand = ReactiveCommand.Create(() => { });
        }
        
        public MainWindowViewModel(PasswordDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _entries = new ObservableCollection<PasswordEntry>();
            _categories = new ObservableCollection<string>();
            
            Console.WriteLine("MainWindowViewModel constructor called");
            LoadData();
            
            AddNewEntryCommand = ReactiveCommand.Create(AddNewEntry);
            EditEntryCommand = ReactiveCommand.Create(EditEntry, 
                this.WhenAnyValue(x => x.SelectedEntry).Select(x => x != null));
            DeleteEntryCommand = ReactiveCommand.CreateFromTask(DeleteEntryAsync, 
                this.WhenAnyValue(x => x.SelectedEntry).Select(x => x != null));
            GeneratePasswordCommand = ReactiveCommand.Create(ShowPasswordGenerator);
            
            CopyUsernameCommand = ReactiveCommand.CreateFromTask(CopyUsernameAsync,
                this.WhenAnyValue(x => x.SelectedEntry).Select(x => x != null && !string.IsNullOrEmpty(x.Username)));
            CopyPasswordCommand = ReactiveCommand.CreateFromTask(CopyPasswordAsync,
                this.WhenAnyValue(x => x.SelectedEntry).Select(x => x != null && !string.IsNullOrEmpty(x.Password)));
            CopyUrlCommand = ReactiveCommand.CreateFromTask(CopyUrlAsync,
                this.WhenAnyValue(x => x.SelectedEntry).Select(x => x != null && !string.IsNullOrEmpty(x.Url)));
            CopyNotesCommand = ReactiveCommand.CreateFromTask(CopyNotesAsync,
                this.WhenAnyValue(x => x.SelectedEntry).Select(x => x != null && !string.IsNullOrEmpty(x.Notes)));
            
            TogglePasswordVisibilityCommand = ReactiveCommand.Create(TogglePasswordVisibility,
                this.WhenAnyValue(x => x.SelectedEntry).Select(x => x != null));
        }
        
        private void TogglePasswordVisibility()
        {
            _isPasswordVisible = !_isPasswordVisible;
            this.RaisePropertyChanged(nameof(PasswordDisplay));
            this.RaisePropertyChanged(nameof(ShowPasswordButtonText));
        }
        
        private async Task CopyUsernameAsync()
        {
            if (SelectedEntry?.Username != null)
            {
                await CopyToClipboard(SelectedEntry.Username);
                await ShowCopyMessage("Логин скопирован в буфер обмена");
            }
        }
        
        private async Task CopyPasswordAsync()
        {
            if (SelectedEntry?.Password != null)
            {
                await CopyToClipboard(SelectedEntry.Password);
                await ShowCopyMessage("Пароль скопирован в буфер обмена");
            }
        }
        
        private async Task CopyUrlAsync()
        {
            if (SelectedEntry?.Url != null)
            {
                await CopyToClipboard(SelectedEntry.Url);
                await ShowCopyMessage("URL скопирован в буфер обмена");
            }
        }
        
        private async Task CopyNotesAsync()
        {
            if (SelectedEntry?.Notes != null)
            {
                await CopyToClipboard(SelectedEntry.Notes);
                await ShowCopyMessage("Заметки скопированы в буфер обмена");
            }
        }
        
        private async Task CopyToClipboard(string text)
        {
            try
            {
                if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var clipboard = desktop.MainWindow?.Clipboard;
                    if (clipboard != null)
                    {
                        await clipboard.SetTextAsync(text);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Copy to clipboard error: {ex.Message}");
            }
        }
        
        private async Task ShowCopyMessage(string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                "Информация", 
                message, 
                ButtonEnum.Ok, 
                Icon.Info);
            await box.ShowAsync();
        }
        
        private void LoadData()
        {
            if (_database == null) 
            {
                Console.WriteLine("Database is null in LoadData");
                return;
            }
            
            try
            {
                Console.WriteLine("Loading data from database");
                var entries = _database.GetAllEntries();
                var allCategories = _database.GetAllCategories();
                
                Console.WriteLine($"Loaded {entries.Count} entries");
                foreach (var entry in entries)
                {
                    Console.WriteLine($"Entry: {entry.Title} - {entry.Username}");
                }
                
                Entries = new ObservableCollection<PasswordEntry>(entries);
                Categories = new ObservableCollection<string>(new[] { "Все" }.Concat(allCategories));
                
                Console.WriteLine($"UI updated with {Entries.Count} entries");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadData error: {ex.Message}");
                Entries = new ObservableCollection<PasswordEntry>();
                Categories = new ObservableCollection<string> { "Все" };
            }
        }
        
        private async void FilterEntries()
        {
            if (_database == null) return;
            
            try
            {
                var searchText = SearchText?.ToLower() ?? "";
                var selectedCategory = SelectedCategory;
                
                var filteredEntries = await Task.Run(() => 
                {
                    var allEntries = _database.GetAllEntries();
                    
                    if (string.IsNullOrEmpty(searchText) && (selectedCategory == "Все" || string.IsNullOrEmpty(selectedCategory)))
                    {
                        return allEntries;
                    }
                    
                    var result = allEntries.AsEnumerable();
                    
                    if (selectedCategory != "Все" && !string.IsNullOrEmpty(selectedCategory))
                    {
                        result = result.Where(e => e.Category == selectedCategory);
                    }
                    
                    if (!string.IsNullOrEmpty(searchText))
                    {
                        result = result.Where(e => 
                            (e.Title?.ToLower().Contains(searchText) ?? false) || 
                            (e.Username?.ToLower().Contains(searchText) ?? false) || 
                            (e.Url?.ToLower().Contains(searchText) ?? false) || 
                            (e.Notes?.ToLower().Contains(searchText) ?? false) ||
                            (e.Category?.ToLower().Contains(searchText) ?? false));
                    }
                    
                    return result.ToList();
                });
                
                Entries = new ObservableCollection<PasswordEntry>(filteredEntries);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FilterEntries error: {ex.Message}");
            }
        }
        
        public void RefreshData()
        {
            Console.WriteLine("RefreshData called");
            LoadData();
        }
        
        private void AddNewEntry()
        {
            if (_database == null) return;
            
            var entry = new PasswordEntry();
            var viewModel = new PasswordEntryViewModel(entry, _database, this, true);
            
            var window = new PasswordEntryView
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
        
        private void EditEntry()
        {
            if (SelectedEntry == null || _database == null) return;
            
            var viewModel = new PasswordEntryViewModel(SelectedEntry, _database, this, false);
            
            var window = new PasswordEntryView
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
        
        private async Task DeleteEntryAsync()
        {
            if (SelectedEntry == null || _database == null) return;
            
            var box = MessageBoxManager.GetMessageBoxStandard(
                "Подтверждение", 
                "Вы действительно хотите удалить эту запись?", 
                ButtonEnum.YesNo, 
                Icon.Question);
            
            var result = await box.ShowAsync();
            
            if (result == ButtonResult.Yes)
            {
                try
                {
                    await Task.Run(() => _database.DeleteEntry(SelectedEntry.Id));
                    RefreshData();
                }
                catch (Exception ex)
                {
                    var errorBox = MessageBoxManager.GetMessageBoxStandard(
                        "Ошибка", 
                        $"Не удалось удалить запись: {ex.Message}", 
                        ButtonEnum.Ok, 
                        Icon.Error);
                    await errorBox.ShowAsync();
                }
            }
        }
        
        private void ShowPasswordGenerator()
        {
            var viewModel = new PasswordGeneratorViewModel();
            
            var window = new PasswordGeneratorView
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
    }
}