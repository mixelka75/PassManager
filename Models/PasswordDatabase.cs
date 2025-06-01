using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PasswordManager.Models
{
    public class PasswordDatabase
    {
        private List<PasswordEntry> _entries = new List<PasswordEntry>();
        private byte[]? _encryptionKey;
        private byte[]? _salt;
        private string _dbPath = string.Empty;
        private readonly object _lockObject = new object();
        
        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include,
            DefaultValueHandling = DefaultValueHandling.Include,
            TypeNameHandling = TypeNameHandling.None
        };
        
        public PasswordDatabase()
        {
        }
        
        public bool Initialize(string masterPassword, string? dbPath = null)
        {
            if (string.IsNullOrEmpty(masterPassword))
                return false;
                
            try
            {
                _dbPath = dbPath ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                    "PasswordManager", 
                    "passwords.db");
                
                string directory = Path.GetDirectoryName(_dbPath)!;
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                string saltPath = _dbPath + ".salt";
                
                Console.WriteLine($"Database path: {_dbPath}");
                Console.WriteLine($"Salt path: {saltPath}");
                Console.WriteLine($"DB exists: {File.Exists(_dbPath)}");
                Console.WriteLine($"Salt exists: {File.Exists(saltPath)}");
                
                if (!File.Exists(saltPath))
                {
                    Console.WriteLine("Creating new database");
                    _salt = EncryptionService.GenerateSalt();
                    _encryptionKey = EncryptionService.DeriveKeyFromPassword(masterPassword, _salt);
                    
                    string saltBase64 = Convert.ToBase64String(_salt);
                    File.WriteAllText(saltPath, saltBase64);
                    
                    _entries = new List<PasswordEntry>();
                    SaveDatabase();
                    Console.WriteLine("New database created");
                    return true;
                }
                else
                {
                    Console.WriteLine("Loading existing database");
                    string saltBase64 = File.ReadAllText(saltPath);
                    _salt = Convert.FromBase64String(saltBase64);
                    _encryptionKey = EncryptionService.DeriveKeyFromPassword(masterPassword, _salt);
                    
                    try
                    {
                        LoadDatabase();
                        Console.WriteLine($"Database loaded successfully. Entries count: {_entries.Count}");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to load database: {ex.Message}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Initialize error: {ex.Message}");
                return false;
            }
        }
        
        private void LoadDatabase()
        {
            lock (_lockObject)
            {
                if (!File.Exists(_dbPath))
                {
                    Console.WriteLine("Database file doesn't exist, creating empty list");
                    _entries = new List<PasswordEntry>();
                    return;
                }
                
                string encryptedJson = File.ReadAllText(_dbPath);
                Console.WriteLine($"Read encrypted data length: {encryptedJson.Length}");
                
                if (string.IsNullOrEmpty(encryptedJson))
                {
                    Console.WriteLine("Empty encrypted data, creating empty list");
                    _entries = new List<PasswordEntry>();
                    return;
                }
                
                string json = EncryptionService.Decrypt(encryptedJson, _encryptionKey!);
                Console.WriteLine($"Decrypted JSON: {json}");
                
                _entries = JsonConvert.DeserializeObject<List<PasswordEntry>>(json, _jsonSettings) ?? new List<PasswordEntry>();
                Console.WriteLine($"Loaded {_entries.Count} entries");
                
                foreach (var entry in _entries)
                {
                    Console.WriteLine($"Entry details - ID: {entry.Id}, Title: '{entry.Title}', Username: '{entry.Username}', Password: '{entry.Password}'");
                }
            }
        }
        
        public void SaveDatabase()
        {
            if (_encryptionKey == null)
                throw new InvalidOperationException("Database not initialized");
                
            lock (_lockObject)
            {
                try
                {
                    Console.WriteLine($"Saving {_entries.Count} entries:");
                    foreach (var entry in _entries)
                    {
                        Console.WriteLine($"Before save - ID: {entry.Id}, Title: '{entry.Title}', Username: '{entry.Username}', Password: '{entry.Password}'");
                    }
                    
                    string json = JsonConvert.SerializeObject(_entries, _jsonSettings);
                    Console.WriteLine($"Saving JSON: {json}");
                    
                    string encryptedJson = EncryptionService.Encrypt(json, _encryptionKey);
                    File.WriteAllText(_dbPath, encryptedJson);
                    
                    Console.WriteLine($"Database saved to: {_dbPath}");
                    Console.WriteLine($"File size: {new FileInfo(_dbPath).Length} bytes");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Save error: {ex.Message}");
                    throw new InvalidOperationException($"Failed to save database: {ex.Message}", ex);
                }
            }
        }
        
        public List<PasswordEntry> GetAllEntries()
        {
            lock (_lockObject)
            {
                Console.WriteLine($"GetAllEntries called, returning {_entries.Count} entries");
                return new List<PasswordEntry>(_entries);
            }
        }
        
        public void AddEntry(PasswordEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));
                
            lock (_lockObject)
            {
                entry.Id = entry.Id ?? Guid.NewGuid().ToString();
                entry.Created = entry.Created == default ? DateTime.Now : entry.Created;
                entry.Modified = DateTime.Now;
                
                Console.WriteLine($"Adding entry - ID: {entry.Id}, Title: '{entry.Title}', Username: '{entry.Username}', Password: '{entry.Password}'");
                
                _entries.Add(entry);
                Console.WriteLine($"Added entry: {entry.Title}, total entries: {_entries.Count}");
                SaveDatabase();
            }
        }
        
        public void UpdateEntry(PasswordEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));
                
            lock (_lockObject)
            {
                int index = _entries.FindIndex(e => e.Id == entry.Id);
                if (index != -1)
                {
                    entry.Modified = DateTime.Now;
                    _entries[index] = entry;
                    Console.WriteLine($"Updated entry: {entry.Title}");
                    SaveDatabase();
                }
                else
                {
                    throw new InvalidOperationException("Entry not found");
                }
            }
        }
        
        public void DeleteEntry(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));
                
            lock (_lockObject)
            {
                int removed = _entries.RemoveAll(e => e.Id == id);
                if (removed > 0)
                {
                    Console.WriteLine($"Deleted {removed} entries with id: {id}");
                    SaveDatabase();
                }
            }
        }
        
        public List<string> GetAllCategories()
        {
            lock (_lockObject)
            {
                return _entries
                    .Where(e => !string.IsNullOrWhiteSpace(e.Category))
                    .Select(e => e.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();
            }
        }
        
        public List<PasswordEntry> SearchEntries(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return GetAllEntries();
                
            string search = searchTerm.ToLowerInvariant();
            
            lock (_lockObject)
            {
                return _entries.Where(e => 
                    (e.Title?.ToLowerInvariant().Contains(search) ?? false) || 
                    (e.Username?.ToLowerInvariant().Contains(search) ?? false) || 
                    (e.Url?.ToLowerInvariant().Contains(search) ?? false) || 
                    (e.Notes?.ToLowerInvariant().Contains(search) ?? false) ||
                    (e.Category?.ToLowerInvariant().Contains(search) ?? false))
                .ToList();
            }
        }
        
        public List<PasswordEntry> GetEntriesByCategory(string category)
        {
            if (string.IsNullOrEmpty(category) || category == "Все")
                return GetAllEntries();
                
            lock (_lockObject)
            {
                return _entries.Where(e => e.Category == category).ToList();
            }
        }
    }
}