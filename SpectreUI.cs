using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Spectre.Console;
using NAudio.Wave;

namespace Musicaly
{
    internal static class SpectreUI
    {
        // Property to track if exit is requested
        public static bool ExitRequested { get; private set; } = false;
        public static string ShowWelcomeMessage() {
            AnsiConsole.MarkupLine("[bold green]Welcome to Musicaly![/]");
            return AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .PageSize(3)
                    .AddChoices(["Log In", "Register", "Exit"]));
        }

        public static string Username() {
            return AnsiConsole.Prompt(
                new TextPrompt<string>("Enter username. Leave empty to exit.")
                    .AllowEmpty());
        }

        public static string Password() {
            return AnsiConsole.Prompt(
                new TextPrompt<string>("Enter password. Leave empty to exit.")
                    .AllowEmpty()
                    .Secret());
        }

        public static async Task SpectreMusicUI()
        {
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
                .AddChoices(Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)).Where(f => f != "C:\\Users\\Hugo\\Music\\desktop.ini")));
            foreach (string s in input) {
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
                        table.Rows.Clear();

                        // Create a simple progress bar for the current song
                        int barLength = 20; // length of the bar
                        int filledLength = Convert.ToInt32(progress * barLength / 100);
                        string bar = new string('■', filledLength) + new string('─', barLength - filledLength);

                        // Highlight the current song, add loop indicator if active
                        string currentDisplay = loopRequested
                            ? $"[bold green] {currentTrack.Title} [[Looping]][/]" // indicate that the song is being looped.
                            : $"[bold green] {currentTrack.Title}[/]";

                        //Show clear paused tag in blue 
                        if (isPaused)
                            currentDisplay += " [blue][[Paused]][/]";

                        bool ltHr = audioFileReader.TotalTime.TotalHours < 1;
                        // Highlight the current song and show progress visually
                        table.AddRow(
                            currentDisplay,                      // current song highlighted, with loop indicator
                            $"[dim]{nextTrack.Title}[/]",               // next song dimmed
                            $"[cyan]{bar} {(ltHr ? audioFileReader.CurrentTime.ToString(@"mm\:ss") + "/" + audioFileReader.TotalTime.ToString(@"mm\:ss") : audioFileReader.CurrentTime.ToString(@"hh\:mm\:ss") + "/" + audioFileReader.TotalTime.ToString(@"hh\:mm\:ss"))}[/]"        // progress bar with percentage
                        );

                        // Add controls row inside the table for style
                        table.AddEmptyRow();
                        table.AddRow(
                            "[bold white]<- [[P]] Play Previous || [[Space]] Pause || [[L]] Loop || [[S]] Skip -> || [[E]] Exit[/]",
                            "",
                            ""
                        );

                        ctx.Refresh();

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

                    if (ExitRequested) {
                        waveOutEvent.Stop();
                        break;
                    }

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