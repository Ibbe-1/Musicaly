using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Spectre.Console;

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
            // Table to display current song, next up, and progress
            var table = new Table().Centered();
            table.AddColumn("Current Song");
            table.AddColumn("Next Up");
            table.AddColumn("Progress");

            // Ask user to input songs
            // CHANGE THIS LATER WHEN PUTTING ACTUAL SONGS, WE WANT TO LOAD FROM A PLAYLIST FILE OR SIMILAR
            Console.WriteLine("Enter your songs (type 'done' when finished):");

            // Collect songs from user
            // CHANGE THIS LATER WHEN PUTTING ACTUAL SONGS, WE WANT TO LOAD FROM A PLAYLIST.
            var songs = new List<string>();
            while (true)
            {
                Console.Write("Song: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "done")
                    break;
                songs.Add(input);

            }

            // Ensure there are at least 2 songs
            if (songs.Count < 2)
            {
                Console.WriteLine("You need at least 2 songs to start the player.");
                return;
            }

            // Initialize current, next, previous
            int songIndex = 0;
            string currentSong = songs[songIndex];
            string nextSong = songs[(songIndex + 1) % songs.Count];
            string previousSong = "";

            // Clear console for UI
            Console.Clear();

            // Display controls
            Console.WriteLine();
            Console.WriteLine("Controls:");
            Console.WriteLine("<- [P] Play Previous || [L] Loop ||  [S] Skip  -> || [E] Exit");
            Console.WriteLine();

            // bool to track loop request
            bool loopRequested = false;

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
                    int progress = 0;

                    // Update the table until the song ends or user requests an action
                    while (progress <= 100)
                    {
                        table.Rows.Clear();

                        // Create a simple progress bar for the current song
                        int barLength = 20; // length of the bar
                        int filledLength = (progress * barLength) / 100;
                        string bar = new string('■', filledLength) + new string('─', barLength - filledLength);

                        // Highlight the current song, add loop indicator if active
                        string currentDisplay = loopRequested
                            ? $"[bold green] {currentSong} [[Looping]][/]" // indicate that the song is being looped.
                            : $"[bold green] {currentSong}[/]";

                        // Highlight the current song and show progress visually
                        table.AddRow(
                            currentDisplay,                      // current song highlighted, with loop indicator
                            $"[dim]{nextSong}[/]",               // next song dimmed
                            $"[cyan]{bar} {progress}%[/]"        // progress bar with percentage
                        );

                        // Add controls row inside the table for style
                        table.AddEmptyRow();
                        table.AddRow(
                            "[bold white]<- [[P]] Play Previous || [[L]] Loop || [[S]] Skip -> || [[E]] Exit[/]",
                            "",
                            ""
                        );

                        ctx.Refresh();

                        // Simulate time passing for song progress
                        await Task.Delay(300);

                        // For demonstration, increment by 5%
                        progress += 5;

                        // Check for user key presses without blocking
                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(true).Key;

                            // Handle key presses
                            // L - Loop, S - Skip, P - Previous, E - Exit
                            switch (key)
                            {
                                case ConsoleKey.L: loopRequested = !loopRequested; break; // toggle loop
                                case ConsoleKey.S: skipRequested = true; break;
                                case ConsoleKey.P: playPreviousRequested = true; break;
                                case ConsoleKey.E: ExitRequested = true; return; // exit player
                            }
                        }

                        if (skipRequested || playPreviousRequested)
                            break; // immediately stop current song
                    }

                    if (ExitRequested) break;

                    // Handle user requests after stopping or finishing the song
                    if (playPreviousRequested)
                    {
                        // Move to previous song in the playlist
                        songIndex--;
                        if (songIndex < 0)
                            songIndex = songs.Count - 1;

                        currentSong = songs[songIndex];
                        nextSong = songs[(songIndex + 1) % songs.Count];
                    }
                    else if (skipRequested || !loopRequested) // move to next song if skipped or finished naturally
                    {
                        songIndex = (songIndex + 1) % songs.Count;
                        currentSong = songs[songIndex];
                        nextSong = songs[(songIndex + 1) % songs.Count];
                    }

                    // loopRequested keeps currentSong the same
                }
            });
        }
    }
}
