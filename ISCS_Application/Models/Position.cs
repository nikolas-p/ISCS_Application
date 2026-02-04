using System;
using System.Collections.Generic;

namespace ISCS_Application.Models;

public partial class Position
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public decimal Salary { get; set; }

    public virtual ICollection<Worker> Workers { get; set; } = new List<Worker>();
}
