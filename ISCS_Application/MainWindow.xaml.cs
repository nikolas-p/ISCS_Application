using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using ISCS_Application.Models;
using ISCS_Application.ViewModels;
using ISCS_Application.Enums;
using System.Diagnostics;

namespace ISCS_Application
{
    public partial class MainWindow : Window
    {
        private readonly bool _isGuest;
        private ObservableCollection<EquipmentListItem> _equipmentItems = new();

        public MainWindow(string? login, bool isGuest)
        {
           
            InitializeComponent();
            _isGuest = isGuest;

            UserInfoText.Text = isGuest
                ? "Вы зашли как гость"
                : $"Вы вошли как: {login}";

            LoadSortOptions();
            LoadEquipment();
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


        }

       
        private void LoadEquipment()
        {
            using var db = new OfficeDbContext();

            var equipmentList = db.Equipment
                .Include(e => e.Place)
                    .ThenInclude(p => p.Office)
                .AsNoTracking()
                .ToList();

            _equipmentItems.Clear();

            foreach (var e in equipmentList)
            {
                var item = new EquipmentListItem
                {
                    EquipmentName = e.Name,
                    EquipmentDescription = e.Description,
                    EquipmentPlace = $"Место: {e.Place?.Name ?? "—"}",
                    EquipmentOffice = $"Офис: {e.Place?.Office?.ShortName ?? "—"}",
                    EquipmentPhotoPath = GetEquipmentImagePath(e.PhotoPath) ?? "/Resources/Images/stub.jpg"
                };

                var endDate = e.ServiceStart.AddYears(e.ServiceLife);
                if (endDate < DateOnly.FromDateTime(DateTime.Now))
                {
                    item.StatusTextBlock = "Срок службы истёк";
                    item.StatusColor = System.Windows.Media.Brushes.IndianRed;
                    item.StatusVisibility = Visibility.Visible;
                }

                _equipmentItems.Add(item);
            }

            EquipmentListBox.ItemsSource = _equipmentItems;
        }

        private string? GetEquipmentImagePath(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            // Отладочный вывод
            Debug.WriteLine($"\nИщем файл: {fileName}");

            // 1. Пробуем как ресурс WPF (самый правильный способ)
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

            // 2. Ищем в корне проекта (где .csproj файл)
            string projectDirectory = GetProjectRootDirectory();
            Debug.WriteLine($"Директория проекта: {projectDirectory}");

            if (!string.IsNullOrEmpty(projectDirectory))
            {
                // Правильный путь: Проект/Resources/Images/
                string imagesDirectory = Path.Combine(projectDirectory, "Resources", "Images");
                Debug.WriteLine($"Директория Images: {imagesDirectory}");
                Debug.WriteLine($"Существует ли: {Directory.Exists(imagesDirectory)}");

                if (Directory.Exists(imagesDirectory))
                {
                    // Показываем все файлы в папке
                    var files = Directory.GetFiles(imagesDirectory);
                    Debug.WriteLine($"Файлов в папке: {files.Length}");
                    foreach (var file in files)
                    {
                        Debug.WriteLine($"  - {Path.GetFileName(file)}");
                    }

                    // Ищем файл
                    string filePath = Path.Combine(imagesDirectory, fileName);
                    Debug.WriteLine($"Полный путь к файлу: {filePath}");
                    Debug.WriteLine($"Существует ли файл: {File.Exists(filePath)}");

                    if (File.Exists(filePath))
                    {
                        Debug.WriteLine($"✓ Найден в проекте: {filePath}");
                        return filePath; // Возвращаем полный путь
                    }

                    // Пробуем без учета регистра
                    var foundFile = files.FirstOrDefault(f =>
                        string.Equals(Path.GetFileName(f), fileName, StringComparison.OrdinalIgnoreCase));

                    if (foundFile != null)
                    {
                        Debug.WriteLine($"✓ Найден (без учета регистра): {foundFile}");
                        return foundFile;
                    }
                }
            }

            Debug.WriteLine($"✗ Файл '{fileName}' не найден");
            return null;
        }

        private string GetProjectRootDirectory()
        {
            try
            {
                // Текущая директория приложения
                string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                Debug.WriteLine($"BaseDirectory: {currentDir}");

                // Поднимаемся по дереву папок
                DirectoryInfo? dir = new DirectoryInfo(currentDir);

                // Поднимаемся пока не найдем .csproj файл
                while (dir != null && !dir.GetFiles("*.csproj").Any())
                {
                    dir = dir.Parent;
                }

                if (dir != null)
                {
                    Debug.WriteLine($"Найден проект в: {dir.FullName}");
                    return dir.FullName;
                }

                // Альтернативный способ
                string? projectPath = Directory.GetParent(currentDir)?
                                             .Parent?.Parent?.FullName;

                Debug.WriteLine($"Альтернативный путь: {projectPath}");
                return projectPath ?? string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка поиска проекта: {ex.Message}");
                return string.Empty;
            }
        }

    }
}