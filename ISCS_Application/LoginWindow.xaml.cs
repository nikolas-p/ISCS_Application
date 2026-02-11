using System.Windows;
using System.Windows.Controls;
using ISCS_Application.Models;
using Microsoft.EntityFrameworkCore;

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

            OpenMainWindow(user.Login);

        }

        private void Guest_Click(object sender, RoutedEventArgs e)
        {
            OpenMainWindow(null);
        }

        private void OpenMainWindow(string? login)
        {
            var worker = VerifyUser(login);
            var main = new MainWindow(worker);
            main.Show();
            Close();
        }
        // Получение пользователя и его роли
        private Worker? VerifyUser(string login)
        {
            using var db = new OfficeDbContext();
            var worker = db.Workers
                .Include(w => w.Position)
                .FirstOrDefault(w => w.Login == login);

            if (worker == null)
                return null;

            return worker;
        }
    }
}
