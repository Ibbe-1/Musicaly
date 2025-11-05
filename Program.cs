using Spectre.Console;

namespace Musicaly
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Initialize SpectreUI
            var ui = new SpectreUI();
            ui.ShowWelcomeMessage();
            // Main loop keeps running until exit is prompted.
            while (!ui.ExitRequested)
            {
                await ui.SpectreMusicUI();
            }
            // Exit message
            AnsiConsole.MarkupLine("[bold green]A work in progress!, thanks for using Musicaly![/]");
        }
    }
}
