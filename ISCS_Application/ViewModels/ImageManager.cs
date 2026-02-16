using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Windows;

namespace ISCS_Application.ViewModels
{
    public class ImageManager
    {
        private const string IMAGES_FOLDER = "Resources/Images";
        private const string DEFAULT_IMAGE = "stub.jpg"; // Заглушка

        private readonly string _imagesDirectory;

        public ImageManager()
        {
            // Определяем путь к папке с изображениями
            string projectDirectory = GetProjectRootDirectory();
            _imagesDirectory = Path.Combine(projectDirectory, IMAGES_FOLDER);

            // Создаем папку, если её нет
            if (!Directory.Exists(_imagesDirectory))
            {
                Directory.CreateDirectory(_imagesDirectory);
            }
        }

        /// <summary>
        /// Загружает новое изображение с сохранением оригинального имени
        /// </summary>
        /// <returns>Имя сохраненного файла или null, если загрузка отменена</returns>
        public async Task<string?> UploadNewImage()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp|All Files|*.*",
                Title = "Выберите изображение"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Получаем оригинальное имя файла
                    string fileName = Path.GetFileName(openFileDialog.FileName);
                    string destinationPath = Path.Combine(_imagesDirectory, fileName);

                    // Проверяем, существует ли уже файл с таким именем
                    if (File.Exists(destinationPath))
                    {
                        // Спрашиваем, хочет ли пользователь перезаписать файл
                        var result = MessageBox.Show(
                            $"Файл {fileName} уже существует. Перезаписать?",
                            "Подтверждение",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.No)
                        {
                            return null;
                        }

                        // Удаляем существующий файл
                        File.Delete(destinationPath);
                    }

                    // Копируем файл с оригинальным именем
                    File.Copy(openFileDialog.FileName, destinationPath);

                    return fileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке изображения: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Загружает изображение с указанным именем (для перезаписи существующего)
        /// </summary>
        public async Task<string?> UploadImageWithName(string? oldFileName)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp|All Files|*.*",
                Title = "Выберите изображение"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string fileName;

                    // Если есть старое имя файла, используем его
                    if (!string.IsNullOrEmpty(oldFileName) && oldFileName != DEFAULT_IMAGE)
                    {
                        fileName = oldFileName;
                    }
                    else
                    {
                        // Иначе используем оригинальное имя нового файла
                        fileName = Path.GetFileName(openFileDialog.FileName);
                    }

                    string destinationPath = Path.Combine(_imagesDirectory, fileName);

                    // Удаляем старый файл, если он существует
                    if (File.Exists(destinationPath))
                    {
                        File.Delete(destinationPath);
                    }

                    // Копируем новый файл
                    File.Copy(openFileDialog.FileName, destinationPath);

                    return fileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке изображения: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Обновляет изображение (удаляет старое, загружает новое с тем же именем)
        /// </summary>
        public async Task<string?> UpdateImage(string? oldFileName)
        {
            return await UploadImageWithName(oldFileName);
        }
        public void DeleteAllImagesByEquipmentId(int equipmentId)
        {
            if (!Directory.Exists(_imagesDirectory))
                return;

            var files = Directory.GetFiles(_imagesDirectory);

            foreach (var file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);

                if (name == equipmentId.ToString())
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Удаляет изображение из папки
        /// </summary>
        /// <summary>
        /// Удаляет изображение из папки
        /// </summary>
        /// <summary>
        /// Удаляет изображение из папки
        /// </summary>
        public void DeleteImage(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName) ||
                fileName == DEFAULT_IMAGE ||
                fileName == "stub.jpg" ||
                fileName == "sub.jpg")
                return;

            string filePath = Path.Combine(_imagesDirectory, fileName);

            try
            {
                if (File.Exists(filePath))
                {
                    // Пробуем снять атрибут "Только для чтения", если он есть
                    File.SetAttributes(filePath, FileAttributes.Normal);

                    File.Delete(filePath);
                    Debug.WriteLine($"Файл {fileName} удален");
                }
                else
                {
                    // Пробуем найти файл без учета регистра
                    var files = Directory.GetFiles(_imagesDirectory);
                    var foundFile = files.FirstOrDefault(f =>
                        string.Equals(Path.GetFileName(f), fileName, StringComparison.OrdinalIgnoreCase));

                    if (foundFile != null)
                    {
                        File.SetAttributes(foundFile, FileAttributes.Normal);
                        File.Delete(foundFile);
                        Debug.WriteLine($"Файл {fileName} удален (найден по неточному совпадению)");
                    }
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Ошибка при удалении файла {fileName}: {ex.Message}");
                // Пробуем еще раз через небольшую задержку
                try
                {
                    Task.Delay(500).Wait();
                    if (File.Exists(filePath))
                    {
                        File.SetAttributes(filePath, FileAttributes.Normal);
                        File.Delete(filePath);
                    }
                }
                catch (Exception innerEx)
                {
                    Debug.WriteLine($"Повторная попытка удаления не удалась: {innerEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при удалении файла {fileName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Получает путь к изображению для отображения
        /// </summary>
        public string GetImagePath(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                // Возвращаем путь к заглушке
                return GetDefaultImagePath();
            }

            // Проверяем существование файла
            string? foundPath = GetEquipmentImagePath(fileName);

            if (foundPath != null)
            {
                return foundPath;
            }

            return GetDefaultImagePath();
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
                    return resourcePath;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка ресурса: {ex.Message}");
            }

            // 2. Ищем в папке проекта
            if (Directory.Exists(_imagesDirectory))
            {
                string filePath = Path.Combine(_imagesDirectory, fileName);

                if (File.Exists(filePath))
                {
                    return filePath;
                }

                // Поиск без учета регистра
                var files = Directory.GetFiles(_imagesDirectory);
                var foundFile = files.FirstOrDefault(f =>
                    string.Equals(Path.GetFileName(f), fileName, StringComparison.OrdinalIgnoreCase));

                if (foundFile != null)
                {
                    return foundFile;
                }
            }

            return null;
        }

        private string GetDefaultImagePath()
        {
            // Проверяем существование заглушки
            string defaultPath = Path.Combine(_imagesDirectory, DEFAULT_IMAGE);

            if (File.Exists(defaultPath))
            {
                return defaultPath;
            }

            // Если заглушка не найдена, возвращаем путь к ресурсу
            return $"/Resources/Images/{DEFAULT_IMAGE}";
        }

        
        public string GetProjectRootDirectory()
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
    }
}
