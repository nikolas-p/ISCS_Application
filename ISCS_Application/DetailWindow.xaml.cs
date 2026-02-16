using System.Windows;
using ISCS_Application.Models;
using ISCS_Application.ViewModels;
using Microsoft.Win32;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Windows.Media.Imaging;
using System.Windows.Input;

namespace ISCS_Application
{
    /// <summary>
    /// Логика взаимодействия для DetailWindow.xaml
    /// </summary>
    public partial class DetailWindow : Window
    {
        private MainWindow _mainWindow;
        private Equipment _detailEquipment;
        private readonly int _itemId;
        private ImageManager _imageManager;
       

        public event EventHandler? DataUpdated;

        public DetailWindow(int itemId, MainWindow mainWindow) // Добавлен параметр mainWindow
        {
            InitializeComponent();

            _itemId = itemId;
            _imageManager = new ImageManager();
            _mainWindow = mainWindow; // Сохраняем ссылку на MainWindow

            if (Application.Current.MainWindow != null)
            {
                Owner = Application.Current.MainWindow;
            }

            IdTextBlock.Text = itemId.ToString();
            MessageTextBlock.Text = $"открылся элемент с id {itemId}";

            LoadEquipmentData();
        }

        private void LoadEquipmentData()
        {
            try
            {
                using (var context = new OfficeDbContext())
                {
                    _detailEquipment = context.Equipment
                        .Include(e => e.Place)
                        .FirstOrDefault(e => e.Id == _itemId);

                    if (_detailEquipment != null)
                    {
                        InventarNumberText.Text = $"Инвентарный номер: {_detailEquipment.InventarNumber}";
                        NameText.Text = _detailEquipment.Name;
                        DescriptionText.Text = _detailEquipment.Description ?? "Нет описания";
                        WeightText.Text = $"Вес: {_detailEquipment.Weight} кг";

                        EquipmentInfoPanel.Visibility = Visibility.Visible;
                        LoadEquipmentImage();
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadEquipmentImage()
        {
            if (_detailEquipment == null)
                return;

            try
            {
                string imagePath = _imageManager.GetImagePath(_detailEquipment.PhotoPath);

                if (!File.Exists(imagePath))
                {
                    LoadDefaultImage();
                    return;
                }

                using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze(); // ВАЖНО

                    EquipmentImage.Source = bitmap;
                }
            }
            catch
            {
                LoadDefaultImage();
            }
        }


        private void LoadDefaultImage()
        {
            try
            {
                string defaultPath = _imageManager.GetImagePath(null);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();

                if (defaultPath.StartsWith("/"))
                {
                    bitmap.UriSource = new Uri(defaultPath, UriKind.Relative);
                }
                else
                {
                    bitmap.UriSource = new Uri(defaultPath, UriKind.Absolute);
                }

                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                EquipmentImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки заглушки: {ex.Message}");
            }
        }

        private void ChangePhotoButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Выберите фотографию"
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                string imagesDirectory = Path.Combine(
                    _imageManager.GetProjectRootDirectory(),
                    "Resources",
                    "Images");

                Directory.CreateDirectory(imagesDirectory);

                string extension = Path.GetExtension(openFileDialog.FileName);
                string newFileName = $"{_itemId}{extension}";
                string destinationPath = Path.Combine(imagesDirectory, newFileName);

               
                // Удаляем изображение из UI (снимаем блокировку)
                EquipmentImage.Source = null;

                // Удаляем старые файлы по ID
                _imageManager.DeleteAllImagesByEquipmentId(_itemId);

                // Копируем новый (без ошибки exists)
                using (var sourceStream = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read))
                using (var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                {
                    sourceStream.CopyTo(destinationStream);
                }

                // 🔥 ТОЛЬКО ПОСЛЕ КОПИРОВАНИЯ обновляем БД
                using (var context = new OfficeDbContext())
                {
                    var equipment = context.Equipment.Find(_itemId);
                    if (equipment != null)
                    {
                        equipment.PhotoPath = newFileName;
                        context.SaveChanges();
                        _detailEquipment.PhotoPath = newFileName;
                    }
                }

                // 🔥 ПЕРЕЗАГРУЖАЕМ КАРТИНКУ
                LoadEquipmentImage();

                // 🔥 ПОЛНОЕ ОБНОВЛЕНИЕ СПИСКА
                _mainWindow.RefreshEquipmentPhoto(_itemId);


                MessageBox.Show("Фото успешно обновлено");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }



        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new OfficeDbContext())
                {
                    var equipment = context.Equipment.Find(_itemId);
                    if (equipment != null)
                    {
                        equipment.PhotoPath = _detailEquipment?.PhotoPath;
                        context.SaveChanges();
                    }
                }

                _mainWindow.RefreshEquipmentPhoto(_itemId);


                MessageBox.Show("Изменения успешно сохранены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
    }
}