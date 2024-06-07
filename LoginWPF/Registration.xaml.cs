using DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LoginWPF
{
    /// <summary>
    /// Логика взаимодействия для Registration.xaml
    /// </summary>
    public partial class Registration : Window
    {
        public Registration()
        {
            InitializeComponent();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void RegButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(loginTextBox.Text) && string.IsNullOrEmpty(nameTextBox.Text) &&
                string.IsNullOrEmpty(passwordBox.Password) && string.IsNullOrEmpty(checkPasswordBox.Password))
            {
                System.Windows.Forms.MessageBox.Show("Вы ввели не все данные!", "Ошибка",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }

            if (passwordBox.Password != checkPasswordBox.Password)
            {
                System.Windows.Forms.MessageBox.Show("Пароли не совпадают!", "Ошибка",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }

            using GameDbContext db = new();
            if (string.IsNullOrEmpty(emailTextBox.Text))
                await db.Users.AddAsync(new User(loginTextBox.Text, passwordBox.Password, nameTextBox.Text));
            else
                await db.Users.AddAsync(new User(loginTextBox.Text, passwordBox.Password, nameTextBox.Text, emailTextBox.Text));

            await db.SaveChangesAsync();

            System.Windows.Forms.MessageBox.Show("Готово");
            loginTextBox.Text = string.Empty;
            nameTextBox.Text = string.Empty;
            passwordBox.Password = string.Empty;
            checkPasswordBox.Password = string.Empty;
            nameTextBox.Text = string.Empty;
            emailTextBox.Text = string.Empty;

            Close();
        }
    }
}
