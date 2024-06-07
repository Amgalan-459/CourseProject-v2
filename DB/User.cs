using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required (ErrorMessage = "Имя обязательно")]
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Email { get; set; }
        public int Rating { get; set; } = 1000;

        public User (string userName, string password, string name, string? email)
        {
            UserName = userName;
            Password = password;
            Name = name;
            Email = email;
        }

        public User(string userName, string password, string name, string? email, int rating)
        {
            UserName = userName;
            Password = password;
            Name = name;
            Email = email;
            Rating = rating;
        }

        public User(string userName, string password, string name, int rating)
        {
            UserName = userName;
            Password = password;
            Name = name;
            Rating = rating;
        }

        public User (string userName, string password, string name)
        {
            UserName = userName;
            Password = password;
            Name = name;
        }

        public override string ToString() => UserName;
    }
}
