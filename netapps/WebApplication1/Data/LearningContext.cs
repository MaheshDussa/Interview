using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models.Entities;
using Task = WebApplication1.Models.Entities.UserTask;

namespace WebApplication1.Data;

public partial class LearningContext : DbContext
{
    public LearningContext()
    {
    }

    public LearningContext(DbContextOptions<LearningContext> options)
        : base(options)
    {
    }

    public virtual DbSet<UserTask> Tasks { get; set; }

    public virtual DbSet<User> Users { get; set; }

}
