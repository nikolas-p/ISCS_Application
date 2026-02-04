using System;
using System.Collections.Generic;

namespace ISCS_Application.Models;

public partial class Place
{
    public int Id { get; set; }

    public int? OfficeId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();

    public virtual Office? Office { get; set; }
}
