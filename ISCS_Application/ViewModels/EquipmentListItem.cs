using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ISCS_Application.ViewModels
{
    public class EquipmentListItem : INotifyPropertyChanged
    {
        public int EquipmentId { get; set; }
 
        private BitmapImage _equipmentPhoto;
        public BitmapImage EquipmentPhoto
        {
            get => _equipmentPhoto;
            set
            {
                _equipmentPhoto = value;
                OnPropertyChanged();
            }
        }

        public string EquipmentName { get; set; } = "";
        public string? EquipmentDescription { get; set; }

        public string EquipmentPlace { get; set; } = "";
        public string EquipmentOffice { get; set; } = "";

        public string EquipmentPhotoPath { get; set; }
        public string StatusTextBlock { get; set; } = "";
        public Brush StatusColor { get; set; } = Brushes.Transparent;
        public Visibility StatusVisibility { get; set; } = Visibility.Collapsed;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
