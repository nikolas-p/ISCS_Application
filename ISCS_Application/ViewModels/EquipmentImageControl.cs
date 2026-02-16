//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Controls;
//using System.Windows.Input;
//using System.Windows.Media.Imaging;
//using System.Windows;
//using ISCS_Application.Models;

//namespace ISCS_Application.ViewModels
//{
//    public partial class EquipmentImageControl : UserControl
//    {
//        private ImageManager _imageManager;
//        private string? _currentImageFileName;

//        public static readonly DependencyProperty ImageFileNameProperty =
//            DependencyProperty.Register(
//                "ImageFileName",
//                typeof(string),
//                typeof(EquipmentImageControl),
//                new PropertyMetadata(null, OnImageFileNameChanged));

//        public static readonly DependencyProperty EquipmentIdProperty =
//            DependencyProperty.Register(
//                "EquipmentId",
//                typeof(int),
//                typeof(EquipmentImageControl));

//        public string? ImageFileName
//        {
//            get { return (string?)GetValue(ImageFileNameProperty); }
//            set { SetValue(ImageFileNameProperty, value); }
//        }

//        public int EquipmentId
//        {
//            get { return (int)GetValue(EquipmentIdProperty); }
//            set { SetValue(EquipmentIdProperty, value); }
//        }

//        public EquipmentImageControl()
//        {
//            InitializeComponent();
//            _imageManager = new ImageManager();
//            Loaded += EquipmentImageControl_Loaded;
//        }

//        private void EquipmentImageControl_Loaded(object sender, RoutedEventArgs e)
//        {
//            UpdateImageSource();
//        }

//        private static void OnImageFileNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
//        {
//            var control = d as EquipmentImageControl;
//            control?.UpdateImageSource();
//        }

//        private void UpdateImageSource()
//        {
//            string imagePath = _imageManager.GetImagePath(ImageFileName);

//            try
//            {
//                if (imagePath.StartsWith("/"))
//                {
//                    // Ресурс
//                    EquipmentImage.Source = new BitmapImage(new Uri(imagePath, UriKind.Relative));
//                }
//                else
//                {
//                    // Файл на диске
//                    EquipmentImage.Source = new BitmapImage(new Uri(imagePath));
//                }

//                _currentImageFileName = ImageFileName;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
//                // Показываем заглушку при ошибке
//                string defaultPath = _imageManager.GetImagePath(null);
//                EquipmentImage.Source = new BitmapImage(new Uri(defaultPath, UriKind.RelativeOrAbsolute));
//            }
//        }

//        private async void UploadImageButton_Click(object sender, RoutedEventArgs e)
//        {
//            await UploadNewImage();
//        }

//        private async void UploadNewImage_Click(object sender, RoutedEventArgs e)
//        {
//            await UploadNewImage();
//        }

//        private async void Image_MouseDown(object sender, MouseButtonEventArgs e)
//        {
//            await UploadNewImage();
//        }

//        private async Task UploadNewImage()
//        {
//            string? newFileName;

//            // Если есть старое фото, используем его имя для перезаписи
//            if (!string.IsNullOrEmpty(_currentImageFileName) && _currentImageFileName != "sub.jpg")
//            {
//                newFileName = await _imageManager.UpdateImage(_currentImageFileName);
//            }
//            else
//            {
//                // Если нет старого фото, загружаем новое с оригинальным именем
//                newFileName = await _imageManager.UploadNewImage();
//            }

//            if (newFileName != null)
//            {
//                // Обновляем свойство
//                ImageFileName = newFileName;

//                // Обновляем в БД
//                await UpdateEquipmentImageInDatabase(EquipmentId, newFileName);
//            }
//        }

//        private async void DeleteImage_Click(object sender, RoutedEventArgs e)
//        {
//            if (_currentImageFileName == null ||
//                _currentImageFileName == "sub.jpg" ||
//                string.IsNullOrEmpty(_currentImageFileName))
//            {
//                return;
//            }

//            var result = MessageBox.Show("Удалить фотографию?",
//                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

//            if (result == MessageBoxResult.Yes)
//            {
//                _imageManager.DeleteImage(_currentImageFileName);

//                // Устанавливаем null (будет использована заглушка)
//                ImageFileName = null;

//                // Обновляем в БД
//                await UpdateEquipmentImageInDatabase(EquipmentId, null);
//            }
//        }

//        private async Task UpdateEquipmentImageInDatabase(int equipmentId, string? imageFileName)
//        {
//            try
//            {
//                // Здесь ваш код обновления БД
//                using (var context = new OfficeDbContext())
//                {
//                    var equipment = await context.Equipment.FindAsync(equipmentId);
//                    if (equipment != null)
//                    {
//                        equipment.PhotoPath = imageFileName;
//                        await context.SaveChangesAsync();
//                    }
//                }

//                MessageBox.Show("Изображение успешно обновлено",
//                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Ошибка при обновлении БД: {ex.Message}",
//                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }
//    }
//}
