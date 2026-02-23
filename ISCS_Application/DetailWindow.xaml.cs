using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ISCS_Application.Models;
using ISCS_Application.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace ISCS_Application
{
    public partial class DetailWindow : Window
    {
        private MainWindow _mainWindow;
        private Equipment _detailEquipment;
        private readonly int? _itemId; // Nullable для режима добавления
        private readonly bool _isEditMode;
        private readonly Worker _currentWorker;
        private ImageManager _imageManager;
        private string _tempImagePath; // Временный путь для нового изображения
        private bool _isImageChanged = false;

        public DetailWindow(int? itemId, MainWindow mainWindow, Worker currentWorker)
        {
            InitializeComponent();
            _itemId = itemId;
            _isEditMode = itemId.HasValue;
            _mainWindow = mainWindow;
            _currentWorker = currentWorker;
            _imageManager = new ImageManager();

            if (Application.Current.MainWindow != null)
            {
                Owner = Application.Current.MainWindow;
            }

            ConfigureWindowMode(); // Сначала права
            LoadInitialData();     // Потом данные
        }

        private void ConfigureWindowMode()
        {
            if (_isEditMode)
            {
                Title = "Редактирование оборудования";
                TitleTextBlock.Text = $"Редактирование оборудования (ID: {_itemId})";

                DeleteButton.Visibility = IsAdmin() ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                Title = "Добавление нового оборудования";
                TitleTextBlock.Text = "Добавление нового оборудования";
                DeleteButton.Visibility = Visibility.Collapsed;

                bool canAdd = IsAdminOrZaveduyuschiy();
                if (!canAdd)
                {
                    InfoMessageTextBlock.Text = "У вас нет прав для добавления оборудования";
                }
            }
        }

        private void UpdateControlsState()
        {
            bool canEdit = IsAdminOrZaveduyuschiy();

            NameTextBox.IsReadOnly = !canEdit;
            DescriptionTextBox.IsReadOnly = !canEdit;
            WeightTextBox.IsReadOnly = !canEdit;
            ServiceLifeTextBox.IsReadOnly = !canEdit;
            ChangePhotoButton.IsEnabled = canEdit;
            DepartmentComboBox.IsEnabled = canEdit && IsAdmin();
            PlaceComboBox.IsEnabled = canEdit;

            InventarNumberTextBox.IsReadOnly = _isEditMode; // Только чтение при редактировании
            RegistrationDatePicker.IsEnabled = false;

            UpdateSaveButtonState();
        }

        private void LoadInitialData()
        {
            LoadDepartments();

            if (_isEditMode)
            {
                LoadEquipmentData();
            }
            else
            {
                RegistrationDatePicker.SelectedDate = DateTime.Today;
                InventarNumberTextBox.Text = ""; 
                LoadDefaultImage();
            }

            UpdateControlsState(); // Устанавливает IsReadOnly в зависимости от режима
        }

        private void LoadEquipmentData()
        {
            try
            {
                using (var context = new OfficeDbContext())
                {
                    _detailEquipment = context.Equipment
                        .AsNoTracking()
                        .Include(e => e.Place)
                            .ThenInclude(p => p.Office)
                        .FirstOrDefault(e => e.Id == _itemId);

                    if (_detailEquipment != null)
                    {
                        NameTextBox.Text = _detailEquipment.Name;
                        InventarNumberTextBox.Text = _detailEquipment.InventarNumber; // Загружаем существующий
                        DescriptionTextBox.Text = _detailEquipment.Description ?? "";
                        WeightTextBox.Text = _detailEquipment.Weight.ToString("F2");
                        ServiceLifeTextBox.Text = _detailEquipment.ServiceLife.ToString();
                        RegistrationDatePicker.SelectedDate = _detailEquipment.ServiceStart.ToDateTime(TimeOnly.MinValue);

                        DepartmentComboBox.SelectedValue = _detailEquipment.Place?.OfficeId;
                        LoadPlaces(_detailEquipment.Place?.OfficeId);
                        PlaceComboBox.SelectedValue = _detailEquipment.PlaceId;

                        LoadEquipmentImage();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSaveButtonState()
        {
            bool canEdit = IsAdminOrZaveduyuschiy();
            bool hasRequiredFields = !string.IsNullOrWhiteSpace(NameTextBox.Text) &&
                                    !string.IsNullOrWhiteSpace(InventarNumberTextBox.Text) &&
                                    PlaceComboBox.SelectedValue != null;

            SaveButton.IsEnabled = canEdit && hasRequiredFields;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields()) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new OfficeDbContext())
                {
                    Equipment equipment;

                    if (_isEditMode)
                    {
                        equipment = context.Equipment.Find(_itemId);
                        if (equipment == null)
                        {
                            MessageBox.Show("Оборудование не найдено", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        // ✅ При редактировании инвентарный номер НЕ изменяется
                    }
                    else
                    {
                        // ✅ При добавлении - проверяем уникальность
                        if (IsInventarNumberExists(context, InventarNumberTextBox.Text.Trim()))
                        {
                            MessageBox.Show("Инвентарный номер уже существует!", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        equipment = new Equipment();
                        context.Equipment.Add(equipment);
                    }

                    // ✅ Всегда используем значение из TextBox (при редактировании оно не меняется)
                    equipment.InventarNumber = InventarNumberTextBox.Text.Trim();
                    equipment.Name = NameTextBox.Text.Trim();
                    equipment.Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text)
                        ? null : DescriptionTextBox.Text.Trim();

                    equipment.Weight = double.TryParse(WeightTextBox.Text, out double weight) ? weight : 0;
                    equipment.ServiceLife = int.TryParse(ServiceLifeTextBox.Text, out int life) ? life : 0;

                    if (RegistrationDatePicker.SelectedDate.HasValue)
                    {
                        equipment.ServiceStart = DateOnly.FromDateTime(RegistrationDatePicker.SelectedDate.Value);
                    }

                    if (PlaceComboBox.SelectedValue is int placeId)
                    {
                        equipment.PlaceId = placeId;
                    }

                    // Обработка изображения
                    if (_isImageChanged && !string.IsNullOrEmpty(_tempImagePath))
                    {
                        if (_isEditMode && !string.IsNullOrEmpty(equipment.PhotoPath))
                        {
                            _imageManager.DeleteImage(equipment.PhotoPath);
                        }

                        string fileName = _isEditMode
                            ? $"{_itemId!.Value}.jpg"
                            : $"{DateTime.Now.Ticks}.jpg";

                        string destPath = _imageManager.SaveImageToPermanent(_tempImagePath, fileName);
                        equipment.PhotoPath = Path.GetFileName(destPath);
                    }

                    context.SaveChanges();

                    MessageBox.Show("Данные успешно сохранены", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    _mainWindow.UpdateEquipmentList();
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}\n\n{ex.InnerException?.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private bool IsInventarNumberExists(OfficeDbContext context, string inventarNumber)
        {
            return context.Equipment.Any(e => e.InventarNumber == inventarNumber);
        }

        private bool ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                InfoMessageTextBlock.Text = "Поле 'Наименование' обязательно";
                return false;
            }

            if (string.IsNullOrWhiteSpace(InventarNumberTextBox.Text))
            {
                InfoMessageTextBlock.Text = "Инвентарный номер обязателен";
                return false;
            }

            if (PlaceComboBox.SelectedValue == null)
            {
                InfoMessageTextBlock.Text = "Выберите кабинет";
                return false;
            }

            InfoMessageTextBlock.Text = "";
            return true;
        }
        // Обработчик изменений для обновления состояния кнопки
        private void RequiredField_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateControlsState();
        }

        private void PositiveNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateControlsState();
        }
        private bool IsAdminOrZaveduyuschiy()
        {
            string role = GetUserRole().ToLower();
            return role.Contains("администратор") || role.Contains("заведующий");
        }

        private bool IsAdmin()
        {
            return GetUserRole().ToLower().Contains("администратор");
        }

        private string GetUserRole()
        {
            using var db = new OfficeDbContext();
            var position = db.Positions.FirstOrDefault(p => p.Id == _currentWorker.PositionId);
            return position?.Name ?? "";
        }

        private void LoadPlaces(int? officeId)
        {
            using var db = new OfficeDbContext();

            IQueryable<Place> query = db.Places.AsNoTracking();

            if (officeId.HasValue)
            {
                // Кабинеты конкретного подразделения
                query = query.Where(p => p.OfficeId == officeId.Value);
            }
            else
            {
                // Кабинеты без подразделения (OfficeId IS NULL)
                query = query.Where(p => p.OfficeId == null);
            }

            var places = query
                .OrderBy(p => p.Name)
                .Select(p => new { p.Id, p.Name })
                .ToList();

            PlaceComboBox.ItemsSource = places;
            PlaceComboBox.SelectedValue = null; // Сбрасываем выбор
        }

        private void LoadDepartments()
        {
            using var db = new OfficeDbContext();

            // Получаем подразделения из БД
            var dbDepartments = db.Offices
                .AsNoTracking()
                .OrderBy(o => o.ShortName ?? o.FullName)
                .Select(o => new { o.Id, Name = o.ShortName ?? o.FullName })
                .ToList();

            // Создаем общий список объектов
            var allDepartments = new List<object>();

            // Добавляем "Без подразделения"
            allDepartments.Add(new { Id = (int?)null, Name = "Без подразделения" });

            // Добавляем подразделения из БД
            allDepartments.AddRange(dbDepartments);

            DepartmentComboBox.ItemsSource = allDepartments;

            // Для заведующего фиксируем его подразделение
            if (!IsAdmin() && _currentWorker.OfficeId != null)
            {
                DepartmentComboBox.SelectedValue = _currentWorker.OfficeId;
                DepartmentComboBox.IsEnabled = false;
                LoadPlaces(_currentWorker.OfficeId);
            }
            else
            {
                // По умолчанию "Без подразделения"
                DepartmentComboBox.SelectedValue = null;
                LoadPlaces(null);
            }
        }
        private void DepartmentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DepartmentComboBox.SelectedValue is int officeId)
            {
                LoadPlaces(officeId);
            }
            else
            {
                // Если подразделение не выбрано
                LoadPlaces(null);
            }
        }
        private void LoadEquipmentImage()
        {
            if (_detailEquipment == null)
            {
                LoadDefaultImage();
                return;
            }
            try
            {
                string imagePath = _imageManager.GetImagePath(_detailEquipment.PhotoPath);

                if (!File.Exists(imagePath))
                {
                    LoadDefaultImage();
                    return;
                }

                LoadImageFromPath(imagePath);
            }
            catch
            {
                LoadDefaultImage();
            }
        }

        private void LoadImageFromPath(string path)
        {
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();

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

                if (File.Exists(defaultPath))
                {
                    LoadImageFromPath(defaultPath);
                }
                else
                {
                    // Создаем пустое изображение если нет заглушки
                    EquipmentImage.Source = null;
                }
            }
            catch
            {
                EquipmentImage.Source = null;
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

                // Создаем временный файл
                string tempFile = Path.Combine(Path.GetTempPath(), $"temp_{Guid.NewGuid()}.jpg");

                // Просто копируем выбранный файл во временную папку
                File.Copy(openFileDialog.FileName, tempFile, true);

                _tempImagePath = tempFile;
                _isImageChanged = true;

                // Отображаем новое изображение
                LoadImageFromPath(tempFile);

                MessageBox.Show("Изображение загружено. Нажмите 'Сохранить' для применения изменений.",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке изображения: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditMode) return;

            try
            {
                using (var context = new OfficeDbContext())
                {
                    var equipment = context.Equipment
                        .Include(e => e.Place)
                        .FirstOrDefault(e => e.Id == _itemId);

                    if (equipment == null)
                    {
                        MessageBox.Show("Оборудование не найдено", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Проверяем возможность удаления
                    string deleteError = CanDeleteEquipment(equipment);
                    if (!string.IsNullOrEmpty(deleteError))
                    {
                        MessageBox.Show(deleteError, "Невозможно удалить",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var result = MessageBox.Show(
                        "Вы уверены, что хотите удалить это оборудование?",
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Удаляем изображение
                        if (!string.IsNullOrEmpty(equipment.PhotoPath))
                        {
                            _imageManager.DeleteImage(equipment.PhotoPath);
                        }

                        context.Equipment.Remove(equipment);
                        context.SaveChanges();

                        MessageBox.Show("Оборудование успешно удалено", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        _mainWindow.UpdateEquipmentList();
                        Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string CanDeleteEquipment(Equipment equipment)
        {
            // Только администратор может удалять
            if (!IsAdmin())
                return "Только администратор может удалять оборудование";

            // Проверка на склад
            bool isStorage = equipment.Place?.Name?.ToLower().Contains("склад") ?? false;

            // ИСПРАВЛЕНО: для не-nullable типов
            // Проверка на превышение срока службы
            var registrationDate = equipment.ServiceStart;
            var serviceLife = equipment.ServiceLife;
            var expirationDate = registrationDate.AddYears(serviceLife);
            bool isExpired = expirationDate < DateOnly.FromDateTime(DateTime.Today);

            if (!isStorage && !isExpired)
            {
                return "Оборудование можно удалять только со склада или с превышенным сроком использования";
            }

            return null; // Можно удалять
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Валидация ввода чисел
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            string currentText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

            // Разрешаем: цифры, одну точку, опциональный знак минус в начале
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(currentText, @"^\d*\,?\d*$");
        }

        protected override void OnClosed(EventArgs e)
        {
            // Очищаем временные файлы
            if (!string.IsNullOrEmpty(_tempImagePath) && File.Exists(_tempImagePath))
            {
                try { File.Delete(_tempImagePath); } catch { }
            }

            base.OnClosed(e);
        }
    }
}