using System;
using ReactiveUI;
using Newtonsoft.Json;

namespace PasswordManager.Models
{
    public class PasswordEntry : ReactiveObject
    {
        private string _id = string.Empty;
        private string _title = string.Empty;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _url = string.Empty;
        private string _notes = string.Empty;
        private string _category = string.Empty;
        private DateTime _created;
        private DateTime _modified;

        [JsonProperty]
        public string Id
        {
            get => _id;
            set => this.RaiseAndSetIfChanged(ref _id, value ?? string.Empty);
        }

        [JsonProperty]
        public string Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value ?? string.Empty);
        }

        [JsonProperty]
        public string Username
        {
            get => _username;
            set => this.RaiseAndSetIfChanged(ref _username, value ?? string.Empty);
        }

        [JsonProperty]
        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value ?? string.Empty);
        }

        [JsonProperty]
        public string Url
        {
            get => _url;
            set => this.RaiseAndSetIfChanged(ref _url, value ?? string.Empty);
        }

        [JsonProperty]
        public string Notes
        {
            get => _notes;
            set => this.RaiseAndSetIfChanged(ref _notes, value ?? string.Empty);
        }

        [JsonProperty]
        public string Category
        {
            get => _category;
            set => this.RaiseAndSetIfChanged(ref _category, value ?? string.Empty);
        }

        [JsonProperty]
        public DateTime Created
        {
            get => _created;
            set => this.RaiseAndSetIfChanged(ref _created, value);
        }

        [JsonProperty]
        public DateTime Modified
        {
            get => _modified;
            set => this.RaiseAndSetIfChanged(ref _modified, value);
        }

        public PasswordEntry()
        {
            Id = Guid.NewGuid().ToString();
            Created = DateTime.Now;
            Modified = DateTime.Now;
        }
    }
}