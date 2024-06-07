 using DB;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LoginWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(loginTextBox.Text))
            {
                System.Windows.Forms.MessageBox.Show("Введите имя!", "Ошибка",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrEmpty(passwordBox.Password))
            {
                System.Windows.Forms.MessageBox.Show("Введите пароль!", "Ошибка",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }

            using GameDbContext db = new();
            User? user = await db.Users
                .Where(u => u.UserName == loginTextBox.Text && u.Password == passwordBox.Password)
                .FirstOrDefaultAsync();
            IEnumerable<User> users = db.Users;

            if (user is null)
            {
                System.Windows.Forms.MessageBox.Show("Неверный логин или пароль!", "Ошибка",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }

            System.Windows.Forms.MessageBox.Show($"Добро пожаловать, {user.Name}!");
            loginTextBox.Text = string.Empty;
            passwordBox.Password = string.Empty;

            PlayWindow playWindow = new(user);
            playWindow.Show();
        }

        private void RegistrationButton_Click(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Hidden;
            Registration reg = new();
            reg.ShowDialog();
            Visibility = Visibility.Visible;
        }

        private async void ForgetButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(loginTextBox.Text))
            {
                System.Windows.Forms.MessageBox.Show("Введите имя!", "Ошибка",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }

            using GameDbContext db = new();
            User? user = await db.Users
                .Where(u => u.UserName == loginTextBox.Text)
                .FirstOrDefaultAsync();
            IEnumerable<User> users = db.Users;

            if (user is null)
            {
                System.Windows.Forms.MessageBox.Show("Такого юзера не существует!", "Ошибка",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }

            Random rnd = new();

            MailAddress from = new MailAddress("amborgolov@mail.ru", "Test mail");
            MailAddress to = new MailAddress("amborgolov@mail.ru", "Test mail");

            MailMessage message = new(from, to);
            string msg = "";
            for (int i = 0; i < 4; i++)
            {
                msg += rnd.Next(0, 9);
            }

            message.Subject = "Код для восстановления пароля";

            message.IsBodyHtml = true;

            using SmtpClient smtpClient = new SmtpClient("smtp.mail.ru");
            smtpClient.Credentials = new NetworkCredential("amborgolov@mail.ru", "fvufkfy2007");
            smtpClient.EnableSsl = true;
            message.Body = $@"<h1>Код: {msg}</h1>";

            await smtpClient.SendMailAsync(message);
            System.Windows.Forms.MessageBox.Show("Письмо отправлено", "Успех");

            //тут открывает новое окно
        }

        //private async Task<User> CheckUser()
        //{
        //    if (string.IsNullOrEmpty(loginTextBox.Text))
        //    {
        //        System.Windows.Forms.MessageBox.Show("Введите имя!", "Ошибка",
        //            System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
        //        return null;
        //    }
        //    if (string.IsNullOrEmpty(passwordBox.Password))
        //    {
        //        System.Windows.Forms.MessageBox.Show("Введите пароль!", "Ошибка",
        //            System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
        //        return;
        //    }

        //    using GameDbContext db = new();
        //    User? user = await db.Users
        //        .Where(u => u.UserName == loginTextBox.Text && u.Password == passwordBox.Password)
        //        .FirstOrDefaultAsync();
        //    IEnumerable<User> users = db.Users;

        //    if (user is null)
        //    {
        //        System.Windows.Forms.MessageBox.Show("Неверный логин или пароль!", "Ошибка",
        //            System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
        //        return;
        //    }

        //    System.Windows.Forms.MessageBox.Show($"Добро пожаловать, {user.Name}!");
        //    loginTextBox.Text = string.Empty;
        //    passwordBox.Password = string.Empty;

        //    return user;
        //}
    }
}