using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace ISCS_Application.ViewModels
{
    public class EquipmentListItem
    {
        public string EquipmentName { get; set; } = "";
        public string? EquipmentDescription { get; set; }

        public string EquipmentPlace { get; set; } = "";
        public string EquipmentOffice { get; set; } = "";

        public string EquipmentPhotoPath { get; set; } 

        public string StatusTextBlock { get; set; } = "";
        public Brush StatusColor { get; set; } = Brushes.Transparent;
        public Visibility StatusVisibility { get; set; } = Visibility.Collapsed;
    }
}
