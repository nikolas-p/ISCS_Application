using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using ISCS_Application.Enums;
using ISCS_Application.Models;
using ISCS_Application.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ISCS_Application
{
    public partial class MainWindow : Window
    {
        private Worker? currentWorker;
        private ObservableCollection<EquipmentListItem> _equipmentItems = new();

        public MainWindow(Worker? worker)
        {
            InitializeComponent();
            currentWorker = worker;

            if (currentWorker != null)
            {
                UserInfoText.Text = $"Вы {worker.Position.Name}: {worker.Firstname} {worker.Lastname} {worker.Surname}";
                LoadEquipmentByRole();
            }
            else
            {
                UserInfoText.Text = "Вы зашли как гость";
                LoadGuestEquipment();
            }
            LoadSortOptions();
        }

        private void LoadSortOptions()
        {
            var values = Enum.GetValues(typeof(EquipmentSortOption))
                             .Cast<EquipmentSortOption>()
                             .Select(e => new
                             {
                                 Value = e,
                                 Description = GetEnumDescription(e)
                             })
                             .ToList();

            SortComboBox.ItemsSource = values;
            SortComboBox.DisplayMemberPath = "Description";
            SortComboBox.SelectedValuePath = "Value";
            SortComboBox.SelectedIndex = 0;
        }

        private static string GetEnumDescription(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();

            return attribute?.Description ?? value.ToString();
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SortComboBox.SelectedItem == null)
                return;

            dynamic selected = SortComboBox.SelectedItem;
            // Здесь можно добавить логику сортировки
        }

        // Метод для гостя: оборудование с PlaceID или OfficeID = null, исключая секретные и складские места
        private List<Equipment> GetGuestEquipment()
        {
            using var db = new OfficeDbContext();
            return db.Equipment
                .Include(e => e.Place)
                    .ThenInclude(p => p.Office)
                .AsNoTracking()
                .Where(e =>
                    // Случай 1: Оборудование без места
                    e.Place == null ||

                    // Случай 2: Место есть, но без офиса И название не содержит запрещенных слов
                    (e.Place != null &&
                     e.Place.OfficeId == null &&
                     e.Place.Name != null &&
                     !e.Place.Name.Contains("засекречено") &&
                     !e.Place.Name.Contains("склад"))
                )
                .ToList();
        }

        // Метод для заведующего отделом: оборудование его офиса
        private List<Equipment> GetManagerEquipment(int managerOfficeId)
        {
            using var db = new OfficeDbContext();
            return db.Equipment
                .Include(e => e.Place)
                    .ThenInclude(p => p.Office)
                .AsNoTracking()
                .Where(e => e.Place != null &&
                           e.Place.Office != null &&
                           e.Place.Office.Id == managerOfficeId)
                .ToList();
        }

        // Метод для администратора: всё оборудование
        private List<Equipment> GetAdminEquipment()
        {
            using var db = new OfficeDbContext();
            return db.Equipment.AsNoTracking().ToList();
        }

        // Метод для обычного пользователя (не гость!)
        private List<Equipment> GetUserEquipment(int userOfficeId)
        {
            using var db = new OfficeDbContext();
            return db.Equipment
                .Include(e => e.Place)
                    .ThenInclude(p => p.Office)
                .AsNoTracking()
                .ToList() // Фильтрация в памяти для ToLower()
                .Where(e =>
                {
                    // 1. Оборудование без места
                    if (e.Place == null)
                        return true;

                    var placeName = e.Place.Name ?? "";
                    var lowerPlaceName = placeName.ToLower();

                    // 2. Место без офиса (общедоступное)
                    if (e.Place.OfficeId == null)
                    {
                        // Проверяем, не является ли место секретным/складским
                        return !lowerPlaceName.Contains("засекречено") &&
                               !lowerPlaceName.Contains("склад");
                    }

                    // 3. Место с офисом - проверяем совпадение с офисом пользователя
                    if (e.Place.OfficeId == userOfficeId)
                    {
                        // Пользователь видит ВСЁ оборудование своего офиса
                        // (включая секретное и складское)
                        return true;
                    }

                    return false;
                })
                .ToList();
        }

        private void LoadEquipmentByRole()
        {
            List<Equipment> equipmentList = new List<Equipment>();

            string UserRole = GetUserRoleByAuth();
            
            if (UserRole.Contains("администратор")) 
                equipmentList = GetAdminEquipment();

            if (UserRole.Contains("склад")) 
                equipmentList = GetStorageEquipment();

            if (UserRole.Contains("заведующий")) 
                equipmentList = GetManagerEquipment(currentWorker.OfficeId);
          
            UpdateEquipmentList(equipmentList);
        }

        private List<Equipment> GetStorageEquipment()
        {
            return new List<Equipment>();
            
        }

        private string GetUserRoleByAuth()
        {
            if (currentWorker == null)
                return "";

            using var db = new OfficeDbContext();
            var position = db.Positions.FirstOrDefault(o => o.Id == currentWorker.PositionId);

            return position?.Name.ToLower() ?? "";
        }


        private void LoadGuestEquipment()
        {
            var equipmentList = GetGuestEquipment();
            UpdateEquipmentList(equipmentList);
        }

        private void UpdateEquipmentList(List<Equipment> equipmentList)
        {
            _equipmentItems.Clear();

            string userRole = GetUserRoleByAuth();
            bool canSeeStatus = !string.IsNullOrEmpty(userRole) &&
                               (userRole.Contains("администратор") || userRole.Contains("заведующий"));

            foreach (var e in equipmentList)
            {
                var item = new EquipmentListItem
                {
                    EquipmentName = e.Name,
                    EquipmentDescription = e.Description,
                    EquipmentPlace = $"Аудитория: {e.Place?.Name ?? "—"}",
                    EquipmentOffice = $"Подразделение: {e.Place?.Office?.ShortName ?? e.Place?.Office?.FullName ?? "—"}",
                    EquipmentPhotoPath = GetEquipmentImagePath(e.PhotoPath) ?? "/Resources/Images/stub.jpg",
                    StatusVisibility = Visibility.Collapsed // По умолчанию скрыто
                };

                if (canSeeStatus)
                {
                    // Пропускаем оборудование на складе
                    if (item.EquipmentPlace.ToLower().Contains("склад"))
                    {
                        continue;
                    }

                    var endDate = e.ServiceStart.AddYears(e.ServiceLife);

                    if (endDate < DateOnly.FromDateTime(DateTime.Now))
                    {
                        item.StatusTextBlock = "На списание";
                        item.StatusColor = System.Windows.Media.Brushes.IndianRed;
                        item.StatusVisibility = Visibility.Visible;
                    }
                    else
                    {
                        // Рассчитываем оставшийся срок
                        var remainingYears = e.ServiceLife - (DateTime.Now.Year - e.ServiceStart.Year);
                        if (remainingYears <= 1)
                        {
                            item.StatusTextBlock = $"Истекает в этом году";
                            item.StatusColor = System.Windows.Media.Brushes.Orange;
                            item.StatusVisibility = Visibility.Visible;
                        }
                        else
                        {
                            item.StatusTextBlock = "Срок службы до " + (e.ServiceStart.Year + e.ServiceLife).ToString();
                            item.StatusColor = System.Windows.Media.Brushes.Transparent;
                            item.StatusVisibility = Visibility.Visible;
                        }
                    }
                }

                _equipmentItems.Add(item);
            }

            EquipmentListBox.ItemsSource = _equipmentItems;
        }

        private string? GetEquipmentImagePath(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            Debug.WriteLine($"\nИщем файл: {fileName}");

            // 1. Пробуем как ресурс WPF
            string resourcePath = $"/Resources/Images/{fileName}";
            Debug.WriteLine($"Ресурсный путь: {resourcePath}");

            try
            {
                var uri = new Uri(resourcePath, UriKind.Relative);
                var streamInfo = Application.GetResourceStream(uri);

                if (streamInfo != null)
                {
                    Debug.WriteLine($"✓ Найден как ресурс: {resourcePath}");
                    return resourcePath;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка ресурса: {ex.Message}");
            }

            // 2. Ищем в корне проекта
            string projectDirectory = GetProjectRootDirectory();

            if (!string.IsNullOrEmpty(projectDirectory))
            {
                string imagesDirectory = Path.Combine(projectDirectory, "Resources", "Images");

                if (Directory.Exists(imagesDirectory))
                {
                    string filePath = Path.Combine(imagesDirectory, fileName);

                    if (File.Exists(filePath))
                    {
                        return filePath;
                    }

                    // Поиск без учета регистра
                    var files = Directory.GetFiles(imagesDirectory);
                    var foundFile = files.FirstOrDefault(f =>
                        string.Equals(Path.GetFileName(f), fileName, StringComparison.OrdinalIgnoreCase));

                    if (foundFile != null)
                    {
                        return foundFile;
                    }
                }
            }

            return null;
        }

        // Метод для получения корневой директории проекта
        private string GetProjectRootDirectory()
        {
            try
            {
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var projectPath = Path.GetFullPath(Path.Combine(basePath, @"..\..\..\"));

                return Directory.Exists(projectPath) ? projectPath : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow();
            login.Show();
            Close();
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}