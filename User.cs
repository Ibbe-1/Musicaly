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
            string[] files = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)).Where(f => f != "C:\\Users\\Hugo\\Music\\desktop.ini").ToArray();
            string title = AnsiConsole.Prompt(
                new TextPrompt<string>("[grey](Leave empty to exit)[/]\nTitle:")
                .Validate(item => playlists.Exists(p => p.Title.Equals(item)) ? ValidationResult.Error("[red]Playlist already exists.[/]") : ValidationResult.Success())
                .AllowEmpty());
            if (title != "") playlists.Add(new Playlist() {
                Title = title,
                tracks = AnsiConsole.Prompt(
                new MultiSelectionPrompt<Track>()
                .PageSize(files.Count() < 3 ? 3 : files.Count())
                .UseConverter(item => item.Title)
                .AddChoices(files.Select(t => new Track() { Title = Markup.Escape(Path.GetFileNameWithoutExtension(t)), Path = t, Duration = new AudioFileReader(t).TotalTime })))
            });
            else return;
            //Alerts UserManager something has changed
            Changed?.Invoke(this);
        }

        public void DeletePlaylist() {
            List<Playlist> playlistsDelete = AnsiConsole.Prompt(
                new MultiSelectionPrompt<Playlist>()
                .PageSize(playlists.Count() < 3 ? 3 : playlists.Count())
                .Title("Choose playlist to delete.")
                .UseConverter(item => item.Title)
                .AddChoices(playlists));
            if (AnsiConsole.Prompt(
                new TextPrompt<bool>("Delete playlist(s):\n" + string.Join('\n', playlistsDelete.Select(p => p.Title)) + "?")
                    .AddChoice(true)
                    .AddChoice(false)
                    .DefaultValue(false)
                    .WithConverter(choice => choice ? "y" : "n"))) playlists.RemoveAll(p => playlistsDelete.Contains(p));
            Changed?.Invoke(this);
        }

        public void EditPlaylist() {
            List<Track> tracks = new List<Track>();
            Playlist playlist = AnsiConsole.Prompt(
                new SelectionPrompt<Playlist>()
                .PageSize(playlists.Count() < 3 ? 3 : playlists.Count())
                .Title("Choose playlist to edit.")
                .UseConverter(item => item.Title)
                .AddChoices(playlists));
            Console.Clear();
            switch (AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                .PageSize(3)
                .AddChoices(["Add tracks", "Delete tracks"]))) {
                case "Add tracks":
                    string[] files = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)).Where(f => f != "C:\\Users\\Hugo\\Music\\desktop.ini").ToArray();
                    tracks = AnsiConsole.Prompt(
                new MultiSelectionPrompt<Track>()
                .PageSize(files.Count() < 3 ? 3 : files.Count())
                .UseConverter(item => item.Title)
                .AddChoices(files.Where(f => !playlist.tracks.Exists(t => t.Title.Equals(Markup.Escape(Path.GetFileNameWithoutExtension(f))))).Select(t => new Track() { Title = Markup.Escape(Path.GetFileNameWithoutExtension(t)), Path = t, Duration = new AudioFileReader(t).TotalTime })));
                    if (AnsiConsole.Prompt(
                new TextPrompt<bool>("Add track(s):\n" + string.Join('\n', tracks.Select(t => t.Title)) + "?")
                    .AddChoice(true)
                    .AddChoice(false)
                    .DefaultValue(false)
                    .WithConverter(choice => choice ? "y" : "n"))) playlist.tracks.AddRange(tracks);
                    break;
                case "Delete tracks":
                    tracks = AnsiConsole.Prompt(
                new MultiSelectionPrompt<Track>()
                .PageSize(playlist.tracks.Count() < 3 ? 3 : playlist.tracks.Count())
                .UseConverter(item => item.Title)
                .AddChoices(playlist.tracks));
                    if (AnsiConsole.Prompt(
                new TextPrompt<bool>("Delete track(s):\n" + string.Join('\n', tracks.Select(t => t.Title)) + "?")
                    .AddChoice(true)
                    .AddChoice(false)
                    .DefaultValue(false)
                    .WithConverter(choice => choice ? "y" : "n"))) playlist.tracks.RemoveAll(t => tracks.Contains(t));
                    break;
            }
            Changed?.Invoke(this);
        }
    }
}
