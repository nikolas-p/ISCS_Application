using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Windows;
using System.Drawing;
using System.Drawing.Imaging;

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
        /// Изменяет размер изображения и сохраняет во временный файл
        /// </summary>
       

        private Rectangle CalculateDestRect(int srcWidth, int srcHeight, int destWidth, int destHeight)
        {
            float ratio = Math.Min((float)destWidth / srcWidth, (float)destHeight / srcHeight);
            int newWidth = (int)(srcWidth * ratio);
            int newHeight = (int)(srcHeight * ratio);

            int x = (destWidth - newWidth) / 2;
            int y = (destHeight - newHeight) / 2;

            return new Rectangle(x, y, newWidth, newHeight);
        }

        /// <summary>
        /// Сохраняет изображение из временного файла в постоянное место
        /// </summary>
        public string SaveImageToPermanent(string tempImagePath, string fileName)
        {
            try
            {
                string destPath = Path.Combine(_imagesDirectory, fileName);

                // Копируем файл
                File.Copy(tempImagePath, destPath, true);

                return destPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при сохранении изображения: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Удаляет изображение по имени файла
        /// </summary>
        public void DeleteImage(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                    return;

                string fullPath = Path.Combine(_imagesDirectory, fileName);

                if (File.Exists(fullPath))
                {
                    File.SetAttributes(fullPath, FileAttributes.Normal);
                    File.Delete(fullPath);
                    Debug.WriteLine($"Удалено изображение: {fullPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при удалении изображения: {ex.Message}");
            }
        }

        /// <summary>
        /// Обновляет изображение (удаляет старое, загружает новое с тем же именем)
        /// </summary>
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
                        Debug.WriteLine($"Удалено изображение для оборудования {equipmentId}: {file}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Ошибка при удалении: {ex.Message}");
                    }
                }
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