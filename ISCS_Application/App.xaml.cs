using System.Windows;
using ISCS_Application.Models;

namespace ISCS_Application
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // ✅ Инициализируем БД
                InitializeDatabase();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeDatabase()
        {
            try
            {
                using var context = new OfficeDbContext();

                // Проверяем подключение
                if (!context.Database.CanConnect())
                {
                    MessageBox.Show("Не удалось подключиться к базе данных. Проверьте строку подключения.",
                        "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DatabaseInitializer.Initialize(context);

                // Проверяем, создались ли данные
                var equipmentCount = context.Equipment.Count();
                System.Diagnostics.Debug.WriteLine($"В базе данных {equipmentCount} записей оборудования");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации БД:\n{ex.Message}\n\nInner: {ex.InnerException?.Message}",
                    "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

}
