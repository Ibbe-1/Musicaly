using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Musicaly {
    internal class UserManager {
        List<User> users = new List<User>();
        string usersPath = "users.json";
        SHA512 sha = SHA512.Create();
        public UserManager() {
            try {
                string usersJSON = File.ReadAllText(usersPath);
                users = JsonSerializer.Deserialize<List<User>>(usersJSON);
            }
            catch {
                File.Create(usersPath).Close();
            }
            //If a user changes something which needs to be saved like making a playlist SaveJSON gets called
            users.ForEach(u => u.Changed += SaveJSON);
        }

        public string Hash(string p) {
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(p))).Replace("-", "");
        }
        public User LogIn() {
            AnsiConsole.MarkupLine("[grey](Leave empty to exit)[/]");
            string userName = SpectreUI.Username();
            while (!users.Exists(u => u.UserName.Equals(userName))) {
                Console.Clear();
                if (userName != "") AnsiConsole.MarkupLine("[red]Username does not exist.[/]");
                else return null;
                AnsiConsole.MarkupLine("[grey](Leave empty to exit)[/]");
                userName = SpectreUI.Username();
            }
            Console.Clear();
            AnsiConsole.MarkupLine("[grey](Leave empty to exit)[/]\nEnter username: " + userName);
            string password = Hash(SpectreUI.Password());
            while (!users.Exists(u => u.Password.Equals(password) && u.UserName.Equals(userName))) {
                if (password != Hash("")) AnsiConsole.MarkupLine("[red]Incorrect password.[/]");
                else return null;
                password = Hash(SpectreUI.Password());
            }
            return users.First(u => u.UserName.Equals(userName) && u.Password.Equals(password));
        }
        public void Register() {
            AnsiConsole.MarkupLine("[grey](Leave empty to exit)[/]");
            string userName = SpectreUI.Username();
            while (users.Exists(u => u.UserName.Equals(userName)) || userName == "") {
                Console.Clear();
                if (userName != "") AnsiConsole.MarkupLine("[red]Username already exists.[/]");
                else return;
                AnsiConsole.MarkupLine("[grey](Leave empty to exit)[/]");
                userName = SpectreUI.Username();
            }
            Console.Clear();
            AnsiConsole.MarkupLine("[grey](Leave empty to exit)[/]\nEnter username: " + userName);
            string password;
            while (true) {
                password = SpectreUI.Password();
                if (password == "") return;
                if (password.Length < 8) {
                    AnsiConsole.MarkupLine("[red]Password must be at least 8 characters long.[/]");
                    continue;
                }

                if (!password.Any(char.IsUpper)) {
                    AnsiConsole.MarkupLine("[red]Password must contain at least one uppercase letter.[/]");
                    continue;
                }

                if (!password.Any(char.IsLower)) {
                    AnsiConsole.MarkupLine("[red]Password must contain at least one lowercase letter.[/]");
                    continue;
                }

                if (!password.Any(char.IsDigit)) {
                    AnsiConsole.MarkupLine("[red]Password must contain at least one number.[/]");
                    continue;
                }

                if (!password.Any(ch => "!@#$%^&*()-_=+[]{};:,.<>?".Contains(ch))) {
                    AnsiConsole.MarkupLine("[red]Password must contain at least one special character.[/]");
                    continue;
                }
                break;
            }
            users.Add(new User() { UserName = userName, Password = Hash(password) });
            SaveJSON(users.Last());
        }

        private void SaveJSON(User user) {
            File.WriteAllText(usersPath, JsonSerializer.Serialize(users));
        }
    }
}
