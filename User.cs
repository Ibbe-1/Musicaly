using NAudio.Wave;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musicaly {
    internal class User {
        public string UserName { get; set; }
        public string Password { get; set; }
        public List<Playlist> playlists { get; set; } = new List<Playlist>();
        public event Action<User>? Changed;

        public void CreatePlaylist() {
            playlists.Add(new Playlist() {
                Title = AnsiConsole.Prompt(
                new TextPrompt<string>("Title:")
                .Validate(item => playlists.Exists(p => p.Title.Equals(item)) ? ValidationResult.Error("Playlist already exists") : ValidationResult.Success())),
                tracks = AnsiConsole.Prompt(
                new MultiSelectionPrompt<Track>()
                .PageSize(Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)).Where(f => f != "C:\\Users\\Hugo\\Music\\desktop.ini").Count() < 3 ? 3 : Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)).Where(f => f != "C:\\Users\\Hugo\\Music\\desktop.ini").Count())
                .UseConverter(item => item.Title)
                .AddChoices(Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)).Where(f => f != "C:\\Users\\Hugo\\Music\\desktop.ini").Select(t => new Track() { Title = Markup.Escape(Path.GetFileNameWithoutExtension(t)), Path = t, Duration = new AudioFileReader(t).TotalTime })))
            });
            //Alerts UserManager something has changed
            Changed?.Invoke(this);
        }

        public void DeletePlaylist() {
            playlists.Remove(AnsiConsole.Prompt(
                new SelectionPrompt<Playlist>()
                .PageSize(playlists.Count() < 3 ? 3 : playlists.Count())
                .Title("Choose playlist to delete.")
                .UseConverter(item => item.Title)
                .AddChoices(playlists)));
            Changed?.Invoke(this);
        }
    }
}
