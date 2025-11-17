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
        }
        public bool LogIn() {
            string userName = SpectreUI.Username();
            while (!users.Exists(u => u.UserName.Equals(userName))) {
                if (userName != "") Console.WriteLine("Username does not exist.");
                else return false;
                userName = SpectreUI.Username();
            }
            string password = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(SpectreUI.Password()))).Replace("-", "");
            while (!users.Exists(u => u.Password.Equals(password))) {
                if (password != "") Console.WriteLine("Incorrect password.");
                else return false;
                password = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(SpectreUI.Password()))).Replace("-", "");
            }
            return true;
        }
        public void Register() {
            string userName = SpectreUI.Username();
            while (users.Exists(u => u.UserName.Equals(userName))) {
                if (userName != "") Console.WriteLine("Username already exists.");
                else return;
                userName = SpectreUI.Username();
            }
            string password;
            while (true) {
                password = SpectreUI.Password();
                if (password == "") return;
                if (password.Length < 8) {
                    Console.WriteLine("Password must be at least 8 characters long.");
                    continue;
                }

                if (!password.Any(char.IsUpper)) {
                    Console.WriteLine("Password must contain at least one uppercase letter.");
                    continue;
                }

                if (!password.Any(char.IsLower)) {
                    Console.WriteLine("Password must contain at least one lowercase letter.");
                    continue;
                }

                if (!password.Any(char.IsDigit)) {
                    Console.WriteLine("Password must contain at least one number.");
                    continue;
                }

                if (!password.Any(ch => "!@#$%^&*()-_=+[]{};:,.<>?".Contains(ch))) {
                    Console.WriteLine("Password must contain at least one special character.");
                    continue;
                }
                break;
            }
            users.Add(new User() { UserName = userName, Password = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(password))).Replace("-", "") });
            File.WriteAllText(usersPath, JsonSerializer.Serialize(users));
        }
    }
}
