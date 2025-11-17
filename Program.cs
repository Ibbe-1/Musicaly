using Spectre.Console;

namespace Musicaly
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            UserManager userManager = new UserManager();
            bool exitRequested = false;
            // Main loop keeps running until exit is prompted.
            while (!exitRequested)
            {
                Console.Clear();
                switch (SpectreUI.ShowWelcomeMessage()) {
                    case "Log In":
                        if (userManager.LogIn()) {
                            Console.Clear();
                            await SpectreUI.SpectreMusicUI();
                        }
                        break;
                    case "Register":
                        userManager.Register();
                        break;
                    case "Exit":
                        exitRequested = true;
                        break;
                }
            }
            // Exit message
            AnsiConsole.MarkupLine("[bold green]A work in progress!, thanks for using Musicaly![/]");
        }
    }
}
