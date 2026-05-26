using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models.Entities;

[Table("Tasks")]
public partial class UserTask
{
    public int TaskId { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = null!;

    public bool? IsCompleted { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
