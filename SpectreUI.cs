using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Spectre.Console;
using NAudio.Wave;
using System.IO;
using System.ComponentModel.Design;

namespace Musicaly
{
    internal class SpectreUI
    {
        private string filePath;

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
            // Collect songs from user
            // CHANGE THIS LATER WHEN PUTTING ACTUAL SONGS, WE WANT TO LOAD FROM A PLAYLIST.
            List<Track> tracks = new List<Track>();
            List<string> input = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                .PageSize(10)
                .UseConverter(item => Markup.Escape(Path.GetFileNameWithoutExtension(item)))
                .AddChoices(Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))));
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

            // Clear console for UI
            Console.Clear();

            // Display controls
            Console.WriteLine();
            Console.WriteLine("Controls:");
            Console.WriteLine("<- [P] Play Previous || [L] Loop ||  [S] Skip  -> || [E] Exit");
            Console.WriteLine("[Space] Pause/Resume || [V] View Playlist || [A] Add Song || [D] Delete Song");
            Console.WriteLine();


            // bool to track loop request
            bool loopRequested = false;
            bool isPaused = false;
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
                            "[bold white]<- [[P]] Play Previous || [[L]] Loop || [[S]] Skip -> || [[E]] Exit[/]",
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
                                case ConsoleKey.L: 
                                    loopRequested = !loopRequested; // toggle loop
                                    break;
                                case ConsoleKey.S:
                                    skipRequested = true;
                                    break;
                                case ConsoleKey.P:
                                    playPreviousRequested = true; 
                                    break;
                                case ConsoleKey.E:
                                    ExitRequested = true; //exit player 
                                    return;
                                //paus with spacebar or continue w music 
                                case ConsoleKey.Spacebar:
                                    if (isPaused)
                                    {
                                        waveOutEvent.Play();   //continue playing
                                        isPaused = false;      // song is now playing 
                                    }
                                    else
                                    {
                                        waveOutEvent.Pause();  //pause t music
                                        isPaused = true;       // song is now paused 
                                    }
                                    break;  //Spacebar stops here

                                case ConsoleKey.V: //show playlist (V)
                                { 
                                    waveOutEvent.Pause();      // pause music while viewing playlist 
                                    Console.Clear();
                                    Console.WriteLine("Current Playlist:");

                                    //Loop through all songs & display them
                                    for (int i = 0; i < tracks.Count; i++)
                                    {
                                        Console.WriteLine($"{i + 1}. {tracks[i].Title}");
                                    }
                                    Console.WriteLine("\nPress any key to return to player...");
                                    Console.ReadKey(true);
                                    Console.Clear();
                                    if (!isPaused) waveOutEvent.Play();     // resume music if wasnt paused
                                    }
                                    break;


                                case ConsoleKey.A:  // ADD Music (A)
                                    {
                                        //paused the currently playing song
                                        waveOutEvent.Pause();
                                        Console.Clear();
                                        Console.WriteLine("Add more songs to the folder:");

                                        //Get the path to the user´s Music folder
                                        string musicFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

                                        //Get all audio files inside t music folder 
                                        string[] allFiles = Directory.GetFiles(musicFolder);

                                        //No music files stop here 
                                        if (allFiles.Length == 0)
                                        {
                                            Console.WriteLine("No music files found.");
                                        }
                                        else
                                        {
     
                                            //Display all songs with numbers 
                                            for (int i = 0; i < allFiles.Length; i++)
                                                Console.WriteLine($"{i + 1}. {Path.GetFileNameWithoutExtension(allFiles[i])}");

                                            Console.WriteLine("\nEnter the number of the songs you want to add ( or press Enter to cancel:");
                                            string input = Console.ReadLine();

                                            if (int.TryParse(input,out int index))
                                            {
                                                index--; //conver 1 -> 0
                                                if (index >= 0 && index < allFiles.Length)
                                                {
                                                    string file = allFiles[index];

                                                    tracks.Add(new Track()
                                                    {
                                                        Title = Path.GetFileNameWithoutExtension(file),
                                                        Path = file,
                                                        Duration = new AudioFileReader(filePath).TotalTime,
                                                    });

                                                    Console.WriteLine("Song added to Playlist!");
                                                    //Uppdate nextTrack
                                                    nextTrack = tracks[(trackIndex + 1) % tracks.Count];
                                                }
                                                else
                                                {
                                                    Console.WriteLine("Invalid number.");
                                                }
                                            }
                                            else
                                            {
                                                Console.WriteLine("Canceled.");
                                            }
                                        }

                                        Console.WriteLine("Press any key to return to plater.");
                                        Console.ReadKey(true);
                                        Console.Clear();
                                        if (!isPaused) waveOutEvent.Play();
                                        break;
                                    }

                                case ConsoleKey.D: //Delete Song (D)
                                {
                                    waveOutEvent.Pause();
                                    Console.Clear();
                                    Console.WriteLine("Remove song from playlist");

                                    if (tracks.Count == 0)
                                    {
                                        Console.WriteLine("No song in playlist.");
                                    }
                                    else
                                    {
                                       for (int i = 0; i < tracks.Count; i++)
                                       {
                                          Console.WriteLine($"{i + 1}. {tracks[i].Title}");
                                       }

                                       Console.WriteLine("\nEnter the number of the song you want to remove (or press Enter to cancel):");
                                       string removeInput = Console.ReadLine();

                                       if (!string.IsNullOrWhiteSpace(removeInput) && int.TryParse(removeInput, out int removeIndex))
                                       {
                                          removeIndex -= 1;
                                          if (removeIndex >= 0 && removeIndex < tracks.Count)
                                          {
                                              //Tillåt inte att ta bort låten som spelas 
                                              if (removeIndex == trackIndex)
                                              {
                                                 Console.WriteLine("You can not remove the song that is currently playing");
                                              }
                                              else
                                              {
                                                  tracks.RemoveAt(removeIndex);
                                                  Console.WriteLine("Song removed from playlist!");

                                                  if (trackIndex >= tracks.Count)
                                                  trackIndex = 0;

                                                  currentTrack = tracks[trackIndex];
                                                  nextTrack = tracks[(trackIndex + 1) % tracks.Count];

                                              }

                                          }
                                          else
                                          {
                                               Console.WriteLine("Invalid number.");
                                          }
                                       }
                                       else
                                       {
                                           Console.WriteLine("Canceled.");
                                       }
                                    }

                                    Console.WriteLine("Press any key to return to player.");
                                    Console.ReadKey(true);
                                    Console.Clear();
                                    if (!isPaused) waveOutEvent.Play();
                                    break;
                                }
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
                }
            });
        }
    }
}
 