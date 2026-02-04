using System.ComponentModel;
namespace ISCS_Application.Enums
{
    public enum EquipmentSortOption
    {
        // Сортировка по инвентарному номеру
        [Description("По инвентарному номеру (А-Я)")]
        ByInventoryNumberAsc,

        [Description("По инвентарному номеру (Я-А)")]
        ByInventoryNumberDesc,

        // Сортировка по аудитории
        [Description("По аудитории расположения (А-Я)")]
        ByLocationAsc,

        [Description("По аудитории расположения (Я-А)")]
        ByLocationDesc,

        // Сортировка по дате установки
        [Description("По дате установки на учёт (сначала старые)")]
        ByInstallationDateAsc,

        [Description("По дате установки на учёт (сначала новые)")]
        ByInstallationDateDesc,

        // Сортировка по сроку годности
        [Description("С истекающим сроком годности")]
        ByExpiryDateAsc,

        [Description("С дальним сроком годности")]
        ByExpiryDateDesc
    }
}
