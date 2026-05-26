using System;
using System.Collections.Generic;

namespace WebApplication1.Models.Entities;

public partial class User
{
    public int UserId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public byte[]? PasswordHash { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual ICollection<UserTask> Tasks { get; set; } = new List<UserTask>();
}
