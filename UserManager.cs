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
        public void LogIn() {
            while (!users.Exists(u => u.UserName.Equals(SpectreUI.Username()))) {
                Console.WriteLine("Username does not exist.");
                Console.Clear();
            }
            while (!users.Exists(u => u.Password.Equals(SpectreUI.Password()))) {
                Console.WriteLine("Password does not exist.");
                Console.Clear();
            }
        }
        public void Register() {
            users.Add(new User() { UserName = SpectreUI.Username(), Password = SpectreUI.Password() });
            File.WriteAllText(usersPath, JsonSerializer.Serialize(users));
        }
    }
}
