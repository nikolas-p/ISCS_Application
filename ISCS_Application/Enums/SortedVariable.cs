using System.ComponentModel;
namespace ISCS_Application.Enums
{
    public enum EquipmentSortOption
    {
        [Description("Без сортировки")]
        None,
        [Description("По весу (возрастание)")]
        WeightAscending,
        [Description("По весу (убывание)")]
        WeightDescending
    }
}
