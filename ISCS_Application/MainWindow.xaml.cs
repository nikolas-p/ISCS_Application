using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ISCS_Application.Enums;
using ISCS_Application.Models;
using ISCS_Application.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ISCS_Application
{
    public partial class MainWindow : Window
    {
        private Worker? currentWorker;
        private ObservableCollection<EquipmentListItem> _equipmentItems = new();
        private DetailWindow _currentDetailWindow;

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
            LoadActionPanel();
            LoadSortOptions();
        }

        private void LoadActionPanel()
        {
            var panel = (StackPanel)FindName("EquipmentActionPanel");
            if (panel == null) return;

            if (currentWorker == null)
            {
                EquipmentActionPanel.Visibility = Visibility.Collapsed;
                return;
            }

            bool isAdminOrEnjeener =
                currentWorker.Position.Name.ToLower().Contains("админ") ||
                currentWorker.Position.Name.ToLower().Contains("инженер");

            EquipmentActionPanel.Visibility = isAdminOrEnjeener
                ? Visibility.Visible
                : Visibility.Collapsed;
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

        // Публичный метод для обновления списка оборудования
        public void UpdateEquipmentList()
        {
            if (currentWorker != null)
            {
                LoadEquipmentByRole();
            }
            else
            {
                LoadGuestEquipment();
            }
        }

        private void UpdateEquipmentList(List<Equipment> equipmentList)
        {
            var newCollection = new ObservableCollection<EquipmentListItem>();

            string userRole = GetUserRoleByAuth();
            bool canSeeStatus = !string.IsNullOrEmpty(userRole) &&
                               (userRole.Contains("администратор") || userRole.Contains("заведующий"));

            foreach (var e in equipmentList)
            {
                var item = new EquipmentListItem
                {
                    EquipmentId = e.Id,
                    EquipmentName = e.Name,
                    EquipmentDescription = e.Description,
                    EquipmentPlace = $"Аудитория: {e.Place?.Name ?? "—"}",
                    EquipmentOffice = $"Подразделение: {e.Place?.Office?.ShortName ?? e.Place?.Office?.FullName ?? "—"}",
                    EquipmentPhoto = LoadImageSafe(e.PhotoPath),
                    StatusVisibility = Visibility.Collapsed
                };

                newCollection.Add(item);
            }

            // Очищаем и обновляем коллекцию
            Application.Current.Dispatcher.Invoke(() =>
            {
                _equipmentItems.Clear();
                foreach (var item in newCollection)
                {
                    _equipmentItems.Add(item);
                }

                EquipmentListBox.ItemsSource = null;
                EquipmentListBox.ItemsSource = _equipmentItems;
                EquipmentListBox.Items.Refresh(); // Принудительное обновление
            });
        }

        private string GetImagesDirectory()
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            Debug.WriteLine($"BaseDirectory: {basePath}");

            // выход из bin/Debug/netX
            var projectPath = Path.GetFullPath(Path.Combine(basePath, @"..\..\.."));
            Debug.WriteLine($"ProjectPath: {projectPath}");

            var imagesPath = Path.Combine(projectPath, "Resources", "Images");
            Debug.WriteLine($"ImagesPath: {imagesPath}");

            return imagesPath;
        }

        private BitmapImage LoadImageSafe(string? path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    System.Diagnostics.Debug.WriteLine($"LoadImageSafe: path is null");
                    return LoadStub();
                }

                string fullPath = Path.Combine(GetImagesDirectory(), path);
                System.Diagnostics.Debug.WriteLine($"LoadImageSafe: trying to load {fullPath}");

                if (!File.Exists(fullPath))
                {
                    System.Diagnostics.Debug.WriteLine($"LoadImageSafe: file not exist {fullPath}");
                    return LoadStub();
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.EndInit();
                bitmap.Freeze();

                System.Diagnostics.Debug.WriteLine($"LoadImageSafe: success");
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadImageSafe error: {ex.Message}");
                return LoadStub();
            }
        }
        private BitmapImage LoadStub()
        {
            try
            {
                string fullPath = Path.Combine(GetImagesDirectory(), "stub.jpg");

                if (!File.Exists(fullPath))
                    return new BitmapImage();

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                return bitmap;
            }
            catch
            {
                return new BitmapImage();
            }
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

        private void EquipmentListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EquipmentListBox.SelectedItem is EquipmentListItem selectedItem)
            {
                EquipmentListBox.IsEnabled = false;
                _currentDetailWindow = new DetailWindow(selectedItem.EquipmentId, this);
                _currentDetailWindow.Closed += DetailWindow_Closed;
                _currentDetailWindow.ShowDialog();
            }
        }

       
        private void DetailWindow_Closed(object? sender, EventArgs e)
        {
            EquipmentListBox.IsEnabled = true;
            EquipmentListBox.SelectedItem = null;

            if (_currentDetailWindow != null)
            {
                _currentDetailWindow.Closed -= DetailWindow_Closed;
                _currentDetailWindow = null;
            }
        }
        public void RefreshEquipmentPhoto(int equipmentId)
        {
            var item = _equipmentItems.FirstOrDefault(x => x.EquipmentId == equipmentId);
            if (item == null)
                return;

            using var db = new OfficeDbContext();
            var equipment = db.Equipment
                .AsNoTracking()
                .FirstOrDefault(e => e.Id == equipmentId);

            if (equipment == null)
                return;

            // Загружаем новое фото
            item.EquipmentPhoto = LoadImageSafe(equipment.PhotoPath);

            // Принудительно обновляем отображение
            EquipmentListBox.Items.Refresh();
        }



    }
}