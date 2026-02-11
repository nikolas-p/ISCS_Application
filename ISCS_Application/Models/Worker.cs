using System;
using System.Collections.Generic;

namespace ISCS_Application.Models;

public partial class Worker
{
    public int Id { get; set; }

    public string Firstname { get; set; } = null!;

    public string Lastname { get; set; } = null!;

    public string? Surname { get; set; }

    public int PositionId { get; set; }

    public int BdYear { get; set; }

    public int OfficeId { get; set; }

    public string? Login { get; set; }

    public string? Password { get; set; }

    public virtual Office Office { get; set; } = null!;

    public virtual ICollection<Office> Offices { get; set; } = new List<Office>();

    public virtual Position Position { get; set; } = null!;

    internal string GetUserRoleByAuth()
    {
        throw new NotImplementedException();
    }
}
