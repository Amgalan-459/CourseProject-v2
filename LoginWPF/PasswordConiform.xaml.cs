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
    /// Логика взаимодействия для PasswordConiform.xaml
    /// </summary>
    public partial class PasswordConiform : Window
    {
        string pass_;
        public PasswordConiform(string pass)
        {
            InitializeComponent();

            pass_ = pass;
        }

        private void ConifromButton_Click(object sender, RoutedEventArgs e)
        {
            if (pass_ == password.Text)
            {
                DialogResult = true;
                return;
            }
            DialogResult = false;
        }
    }
}
