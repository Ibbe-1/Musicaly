using System;
using System.Collections.Generic;
using Spectre.Console;

namespace Musicaly
{
    internal class ViewMusic
    {
        //Visar en lista över alla låtar i indexet
        public void ShowAllSongs(List<string> songs)
        {
            Console.Clear();

            if (songs == null || songs.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No songs found in the index![/]");
                return;
            }

            // Skapa en snygg extendable tree-view för låtarna
            var tree = new Tree("[bold yellow]🎵 Music Index[/]");
            int index = 1;

            foreach (var song in songs)
            {
                var node = tree.AddNode($"[green]{index}.[/] [white]{song}[/]");
                node.AddNode("[dim]Artist: Unknown[/]");
                node.AddNode("[dim]Album: Unknown[/]");
                node.AddNode("[dim]Length: --:--[/]");
                index++;
            }

            AnsiConsole.Write(tree);
        }

        // Interaktiv meny för att välja startlåt
        public int ChooseStartingSong(List<string> songs)
        {
            if (songs == null || songs.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No songs to select from![/]");
                return -1;
            }

            var prompt = new SelectionPrompt<string>()
                .Title("[bold yellow]Select a song to start playing:[/]")
                .PageSize(10)
                .AddChoices(songs);

            string selected = AnsiConsole.Prompt(prompt);
            int index = songs.IndexOf(selected);

            AnsiConsole.MarkupLine($"\n[bold green]You selected:[/] {selected}\n");
            return index;
        }
    }
}
