using System;
using System.Collections.Generic;

namespace ISCS_Application.Models;

public partial class Office
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string? ShortName { get; set; }

    public int? Floor { get; set; }

    public int? GeneralWorkerId { get; set; }

    public virtual Worker? GeneralWorker { get; set; }

    public virtual ICollection<Place> Places { get; set; } = new List<Place>();

    public virtual ICollection<Worker> Workers { get; set; } = new List<Worker>();
}
