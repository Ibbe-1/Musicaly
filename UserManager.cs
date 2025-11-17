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
            string password = SpectreUI.Password();
            while (!users.Exists(u => u.Password.Equals(BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(password))).Replace("-", "")))) {
                if (password != "") Console.WriteLine("Incorrect password.");
                else return false;
                password = SpectreUI.Password();
            }
            return true;
        }
        public void Register() {
            users.Add(new User() { UserName = SpectreUI.Username(), Password = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(SpectreUI.Password()))).Replace("-", "") });
            File.WriteAllText(usersPath, JsonSerializer.Serialize(users));
        }
    }
}
