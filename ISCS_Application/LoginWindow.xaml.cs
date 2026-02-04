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
using ISCS_Application.Models;

namespace ISCS_Application
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            using var db = new OfficeDbContext();

            var user = db.Workers.FirstOrDefault(
                w => w.Login == LoginBox.Text &&
                w.Password == PasswordBox.Password);

            if (user == null)
            {
                MessageBox.Show("Неверный логин или пароль");
                return;
            }

            OpenMainWindow(user.Login, false);

        }

        private void Guest_Click(object sender, RoutedEventArgs e)
        {
            OpenMainWindow("Гость", true);
        }

        private void OpenMainWindow(string? login, bool isGuest)
        {
            var main = new MainWindow(login, isGuest);
            main.Show();
            Close();
        }
    }
}
