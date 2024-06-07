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
        public PlayWindow()
        {
            InitializeComponent();


            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    Button b = new();
                    b.Click += B_Click;

                    if (i % 2 != j % 2)
                        b.Background = Brushes.White;
                    else
                        b.Background = Brushes.Brown;

                    b.Height = 50;
                    b.Width = 50;
                    Buttons[i, j] = b;
                    field.Children.Add(Buttons[i, j]);
                }
            }

        }

        private void B_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.MessageBox.Show("Field");
        }

        private void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.MessageBox.Show("New game");
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            field.Children.Clear();

            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                {
                    Button b = new();
                    b.Click += B_Click;

                    if (i % 2 != j % 2)
                        b.Background = Brushes.White;
                    else
                        b.Background = Brushes.Brown; //при замене цвета можно использовать черно/белый

                    b.Height = 50;
                    b.Width = 50;
                    Buttons[i, j] = b;
                    field.Children.Add(Buttons[i, j]);
                }

            System.Windows.Forms.MessageBox.Show("Field cleared");
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
    }
}
