using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Spectre.Console;
using NAudio.Wave;
using System.Data;

namespace Musicaly
{
    internal static class SpectreUI
    {
        public static string ShowWelcomeMessage() {
            AnsiConsole.MarkupLine("[bold green]Welcome to Musicaly![/]");
            return AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .PageSize(3)
                    .AddChoices(["Log In", "Register", "Exit"]));
        }

        public static string Username() {
            return AnsiConsole.Prompt(
                new TextPrompt<string>("Enter username:")
                    .AllowEmpty());
        }

        public static string Password() {
            return AnsiConsole.Prompt(
                new TextPrompt<string>("Enter password:")
                    .AllowEmpty()
                    .Secret());
        }

        public static string UserMenu(User user) {
            return user.playlists.Any() ? AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                .PageSize(5)
                .AddChoices(["Choose playlist", "Create playlist", "Edit playlist", "Delete playlist", "Logout"])) : AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                .PageSize(3)
                .AddChoices(["Create playlist", "Logout"]));
        }

        public async static Task UserMenuDropdown(User user) {
            while (true) {
                Console.Clear();
                string choice;
                switch (choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                .PageSize(user.playlists.Count() + 5)
                .AddChoices("Choose playlist")
                .AddChoiceGroup("Playlists", user.playlists.Select(p => p.Title))
                .AddChoices(["Create playlist", "Delete playlist", "Logout"]))) {
                    case "Choose playlist":
                        return;
                    case "Create playlist":
                        user.CreatePlaylist();
                        break;
                    case "Delete playlist":
                        user.DeletePlaylist();
                        break;
                    case "Logout":
                        return;
                    default:
                        await SpectreMusicUI(user.playlists.Find(p => p.Title.Equals(choice)));
                        return;
                }
            }
        }

        public static async Task SpectreMusicUI(Playlist playlist) {
            // We need to create instances of ViewMusic so that VivewMusic methods can be used.
            var viewMusic = new ViewMusic();

            // Table to display current song, next up, and progress
            var table = new Table().Centered();
            table.AddColumn("Current Song");
            table.AddColumn("Next Up");
            table.AddColumn("Progress");

            // Ensure there are at least 2 songs
            if (playlist.tracks.Count < 2) {
                Console.WriteLine("You need at least 2 songs to start the player.");
                return;
            }

            // Initialize current, next, previous
            int trackIndex = 0;
            Track currentTrack = playlist.tracks[trackIndex];
            Track nextTrack = playlist.tracks[(trackIndex + 1) % playlist.tracks.Count];
            string previousSong = "";

            // Property to track if exit is requested
            bool ExitRequested = false;
            //Håller koll om musiken är pausad
            bool isPaused = false;

            viewMusic.ShowAllSongs(playlist.tracks.Select(t => t.Title).ToList()); // Show all songs, we need this here.
            // this needs to be fixed currently it shows no songs
            // also implement a way to popout the viewer so that it doesn't interfere with the player UI

            // Display controls
            Console.WriteLine();
            Console.WriteLine("Controls:");
            Console.WriteLine("<- [P] Play Previous || [L] Loop ||  [S] Skip  -> || [E] Exit");
            Console.WriteLine();
            Console.Clear();

            // bool to track loop request
            bool loopRequested = false;
            WaveOutEvent waveOutEvent = new WaveOutEvent();

            // Use a single Live display for the table wrapped in a Panel
            await AnsiConsole.Live(new Panel(table) {
                Header = new PanelHeader("[bold yellow]Musicaly Player[/]"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Grey, decoration: Decoration.Bold)
            }).StartAsync(async ctx => {
                // main loop runs until user requests exit
                while (!ExitRequested) {
                    // bools to track user requests
                    bool skipRequested = false;
                    bool playPreviousRequested = false;

                    // Simulate song progress
                    double progress = 0;

                    AudioFileReader audioFileReader = new AudioFileReader(currentTrack.Path);
                    try {
                        waveOutEvent.Init(audioFileReader);
                    }
                    catch (Exception ex) {
                        Console.WriteLine(ex.Message + ". Press any key to continue.");
                        Console.ReadKey();
                        return;
                    }
                    waveOutEvent.Play();

                    // Update the table until the song ends or user requests an action
                    while (Convert.ToInt32(progress) < 100)
                    {
                        // GRID LAYOUT FOR PLAYER
                        var grid = new Grid();
                        grid.AddColumn();
                        grid.AddColumn();

                        // Build Now Playing text with indicators
                        string nowPlayingText = $"[bold green]{currentTrack.Title}[/]";

                        if (loopRequested)
                            nowPlayingText += " [yellow][[Looping]][/]";

                        if (isPaused)
                            nowPlayingText += " [blue][[Paused]][/]";

                        // Now Playing Panel
                        var nowPlayingPanel = new Panel(nowPlayingText)
                        {
                            Header = new PanelHeader("Now Playing"),
                            Border = BoxBorder.Rounded
                        };

                        // Next Up Panel
                        var nextUpPanel = new Panel($"[yellow]{nextTrack.Title}[/]")
                        {
                            Header = new PanelHeader("Next Up"),
                            Border = BoxBorder.Rounded
                        };

                        grid.AddRow(nowPlayingPanel, nextUpPanel);

                        // Progress Bar
                        int barLength = 30;
                        int filled = Convert.ToInt32(progress * barLength / 100);
                        string bar = new string('■', filled) + new string('─', barLength - filled);
                        bool ltHr = audioFileReader.TotalTime.TotalHours < 1;
                        string timeString = ltHr
                            ? $"{audioFileReader.CurrentTime:mm\\:ss}/{audioFileReader.TotalTime:mm\\:ss}"
                            : $"{audioFileReader.CurrentTime:hh\\:mm\\:ss}/{audioFileReader.TotalTime:hh\\:mm\\:ss}";

                        var progressPanel = new Panel($"[cyan]{bar}[/] {timeString}")
                        {
                            Header = new PanelHeader("Progress"),
                            Border = BoxBorder.Rounded
                        };

                        // Playlist Panel
                        string playlistDisplay =
                            string.Join("\n", playlist.tracks.Select((t, i) =>
                                i == trackIndex ? $"[green]> {t.Title}[/]" : $"  {t.Title}"
                            ));

                        var playlistPanel = new Panel(playlist)
                        {
                            Header = new PanelHeader("Playlist"),
                            Border = BoxBorder.Rounded
                        };

                        grid.AddRow(progressPanel, playlistPanel);

                        // Controls Panel
                        var controlsPanel = new Panel(
                            "[white] [[P]] Prev | [[Space]] Pause | [[L]] Loop | [[J]] Jump | [[<-]] -5s | [[->]] +5s | [[S]] Skip | [[E]] Exit [/]"
                            )
                        {
                            Border = BoxBorder.None
                        };

                        grid.AddRow(controlsPanel);

                        // Combine everything into the main player panel with decorative title
                        var playerPanel = new Panel(grid)
                        {
                            Header = new PanelHeader("[bold yellow] MUSICALY [/]"), // Decorative top title
                            Border = BoxBorder.Double,
                            Padding = new Padding(1, 1, 1, 1)
                        };

                        // Update live display
                        ctx.UpdateTarget(playerPanel);

                        // Simulate time passing for song progress
                        await Task.Delay(300);

                        progress = audioFileReader.CurrentTime / audioFileReader.TotalTime * 100;

                        // Check for user key presses without blocking
                        if (Console.KeyAvailable) {
                            var key = Console.ReadKey(true).Key;

                            // Handle key presses
                            switch (key)
                            {
                                case ConsoleKey.Spacebar:
                                    isPaused = !isPaused;
                                    break; // toggle pause/resume

                                case ConsoleKey.L:
                                    loopRequested = !loopRequested;
                                    break; // toggle loop

                                case ConsoleKey.S:
                                    skipRequested = true;
                                    break;

                                case ConsoleKey.P:
                                    playPreviousRequested = true;
                                    break;

                                case ConsoleKey.E:
                                    ExitRequested = true;
                                    return; // exit player

                                // Seek backward 5 seconds
                                case ConsoleKey.LeftArrow:
                                    audioFileReader.CurrentTime -= TimeSpan.FromSeconds(5);
                                    if (audioFileReader.CurrentTime < TimeSpan.Zero)
                                        audioFileReader.CurrentTime = TimeSpan.Zero;
                                    break;

                                // Seek forward 5 seconds
                                case ConsoleKey.RightArrow:
                                    audioFileReader.CurrentTime += TimeSpan.FromSeconds(5);
                                    if (audioFileReader.CurrentTime > audioFileReader.TotalTime)
                                        audioFileReader.CurrentTime = audioFileReader.TotalTime;
                                    break;

                                // Jump to specific time
                                case ConsoleKey.J:
                                    {
                                        Console.CursorVisible = true;
                                        Console.Write("\nJump to (mm:ss): ");
                                        string jumpInput = Console.ReadLine();
                                        Console.CursorVisible = false;

                                        if (TimeSpan.TryParseExact(jumpInput, @"m\:ss", null, out TimeSpan jumpTo))
                                        {
                                            if (jumpTo < audioFileReader.TotalTime)
                                            {
                                                audioFileReader.CurrentTime = jumpTo;
                                            }
                                        }
                                        Console.Clear();
                                        break;
                                    }
                            }
                        }

                        if (ExitRequested) {
                            waveOutEvent.Stop();
                            return;
                        }
                        if (skipRequested || playPreviousRequested)
                            break; // immediately stop current song
                        if (isPaused) waveOutEvent.Pause();
                        else waveOutEvent.Play();
                    }

                    // Handle user requests after stopping or finishing the song
                    if (playPreviousRequested) {
                        // Move to previous song in the playlist
                        trackIndex--;
                        if (trackIndex < 0)
                            trackIndex = playlist.tracks.Count - 1;

                        currentTrack = playlist.tracks[trackIndex];
                        nextTrack = playlist.tracks[(trackIndex + 1) % playlist.tracks.Count];
                    }
                    else if (skipRequested || !loopRequested) // move to next song if skipped or finished naturally
                    {
                        trackIndex = (trackIndex + 1) % playlist.tracks.Count;
                        currentTrack = playlist.tracks[trackIndex];
                        nextTrack = playlist.tracks[(trackIndex + 1) % playlist.tracks.Count];
                    }

                    waveOutEvent.Stop();

                    // loopRequested keeps currentSong the same

                    //Reset pause when switching to a new songs
                    isPaused = false;
                }
            });
        }
    }
}
