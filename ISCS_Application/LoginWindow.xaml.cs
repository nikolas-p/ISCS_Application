using System.Windows;
using System.Windows.Controls;
using ISCS_Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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
            var login = LoginBox.Text;
            var password = PasswordBox.Password;

            // Проверка на пустые значения
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите логин и пароль");
                return;
            }

            var worker = VerifyUser(login, password);

            if (worker == null)
            {
                MessageBox.Show("Неверный логин или пароль");
                return;
            }

            OpenMainWindow(worker);
        }

        private void Guest_Click(object sender, RoutedEventArgs e)
        {
            OpenMainWindow(null);
        }

        private void OpenMainWindow(Worker? worker)
        {
            var main = new MainWindow(worker);
            main.Show();
            Close();
        }

        // Получение пользователя по логину и паролю
        private Worker? VerifyUser(string login, string password)
        {
            // Если гость - возвращаем null
            if (string.IsNullOrEmpty(login))
                return null;

            using var db = new OfficeDbContext();
            var worker = db.Workers
                .Include(w => w.Position)
                .FirstOrDefault(w => w.Login == login);

            // Проверяем существование пользователя и соответствие пароля
            if (worker == null || worker.Password != password)
                return null;

            return worker;
        }
    }
}
