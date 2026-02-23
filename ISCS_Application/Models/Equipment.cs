using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ISCS_Application.Models;

public partial class Equipment
{
    [Key] // Primary Key
    public int Id { get; set; }

    public string InventarNumber { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public double Weight { get; set; }

    public string? PhotoPath { get; set; }

    public DateOnly ServiceStart { get; set; }

    public int ServiceLife { get; set; }

    public int PlaceId { get; set; }

    public virtual Place Place { get; set; } = null!;
}
