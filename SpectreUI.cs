using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Spectre.Console;
using NAudio.Wave;

namespace Musicaly
{
    internal class SpectreUI
    {
        // Property to track if exit is requested
        public bool ExitRequested { get; private set; } = false;
        public void ShowWelcomeMessage()
        {
            AnsiConsole.MarkupLine("[bold green]Welcome to Musicaly![/]");
        }

        public async Task SpectreMusicUI()
        {
            // We need to create instances of ViewMusic so that VivewMusic methods can be used.
            var viewMusic = new ViewMusic();

            // Table to display current song, next up, and progress
            var table = new Table().Centered();
            table.AddColumn("Current Song");
            table.AddColumn("Next Up");
            table.AddColumn("Progress");

            // Ask user to input songs
            // Collect songs from user
            // CHANGE THIS LATER WHEN PUTTING ACTUAL SONGS, WE WANT TO LOAD FROM A PLAYLIST.
            List<Track> tracks = new List<Track>();
            List<string> input = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                .PageSize(10)
                .UseConverter(item => Markup.Escape(Path.GetFileNameWithoutExtension(item)))
                .AddChoices(Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))));
            foreach (string s in input)
            {
                tracks.Add(new Track() { Title = Markup.Escape(Path.GetFileNameWithoutExtension(s)), Path = s, Duration = new AudioFileReader(s).TotalTime });
            }

            // Ensure there are at least 2 songs
            if (tracks.Count < 2)
            {
                Console.WriteLine("You need at least 2 songs to start the player.");
                return;
            }

            // Initialize current, next, previous
            int trackIndex = 0;
            Track currentTrack = tracks[trackIndex];
            Track nextTrack = tracks[(trackIndex + 1) % tracks.Count];
            string previousSong = "";

            //Håller koll om musiken är pausad
            bool isPaused = false;

            viewMusic.ShowAllSongs(tracks.Select(t => t.Title).ToList()); // Show all songs, we need this here.
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
            await AnsiConsole.Live(new Panel(table)
            {
                Header = new PanelHeader("[bold yellow]Musicaly Player[/]"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Grey, decoration: Decoration.Bold)
            }).StartAsync(async ctx =>
            {
                // main loop runs until user requests exit
                while (!ExitRequested)
                {
                    // bools to track user requests
                    bool skipRequested = false;
                    bool playPreviousRequested = false;

                    // Simulate song progress
                    double progress = 0;

                    AudioFileReader audioFileReader = new AudioFileReader(currentTrack.Path);
                    waveOutEvent.Init(audioFileReader);
                    waveOutEvent.Play();

                    // Update the table until the song ends or user requests an action
                    while (Convert.ToInt32(progress) < 100)
                    {
                        // GRID LAYOUT FOR PLAYER
                        var grid = new Grid();
                        grid.AddColumn();
                        grid.AddColumn();

                        // Now Playing Panel
                        var nowPlayingPanel = new Panel($"[bold green]{currentTrack.Title}[/]")
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
                        string playlist =
                            string.Join("\n", tracks.Select((t, i) =>
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
                         "[white] [[P]] Prev | [[Space]] Pause | [[L]] Loop | [[S]] Skip | [[E]] Exit [/]"
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

                        // For demonstration, increment by 5%
                        progress = audioFileReader.CurrentTime / audioFileReader.TotalTime * 100;

                        // Check for user key presses without blocking
                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(true).Key;

                            // Handle key presses
                            // L - Loop, S - Skip, P - Previous, E - Exit
                            switch (key)
                            {
                                case ConsoleKey.Spacebar: isPaused = !isPaused; break; // toggle pause/resume
                                case ConsoleKey.L: loopRequested = !loopRequested; break; // toggle loop
                                case ConsoleKey.S: skipRequested = true; break;
                                case ConsoleKey.P: playPreviousRequested = true; break;
                                case ConsoleKey.E: ExitRequested = true; return; // exit player
                            }
                        }

                        if (skipRequested || playPreviousRequested)
                            break; // immediately stop current song
                        if (isPaused) waveOutEvent.Pause();
                        else waveOutEvent.Play();
                    }

                    if (ExitRequested) break;

                    // Handle user requests after stopping or finishing the song
                    if (playPreviousRequested)
                    {
                        // Move to previous song in the playlist
                        trackIndex--;
                        if (trackIndex < 0)
                            trackIndex = tracks.Count - 1;

                        currentTrack = tracks[trackIndex];
                        nextTrack = tracks[(trackIndex + 1) % tracks.Count];
                    }
                    else if (skipRequested || !loopRequested) // move to next song if skipped or finished naturally
                    {
                        trackIndex = (trackIndex + 1) % tracks.Count;
                        currentTrack = tracks[trackIndex];
                        nextTrack = tracks[(trackIndex + 1) % tracks.Count];
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
