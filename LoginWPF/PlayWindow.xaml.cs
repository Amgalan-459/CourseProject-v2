using DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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
    /// Логика взаимодействия для PlayWindow.xaml
    /// </summary>
    public partial class PlayWindow : Window
    {
        public Button[,] Buttons = new Button[8, 8];
        public PlayWindow(User user)
        {
            InitializeComponent();


            username.Text = $"Добро пожаловать, {user.Name}!";
            rating.Text = $"Рейтинг: {user.Rating}";
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            TcpClient server = new TcpClient("192.168.138.113", 2024);
        }

        private void NewOfflineGameButton_Click(object sender, RoutedEventArgs e)
        {
            SrcChess2.MainWindow game = new();
            this.Visibility = Visibility.Hidden;
            game.ShowDialog();
            this.Visibility = Visibility.Visible;
        }

        private void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
