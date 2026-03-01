

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TodoApi;

public partial class ToDoDbContext : DbContext
{
    public ToDoDbContext()
    {
    }

    public ToDoDbContext(DbContextOptions<ToDoDbContext> options)
        : base(options)
    {
    }

    // טבלת המשימות הקיימת
    public virtual DbSet<Item> Items { get; set; }
    
    // טבלת המשתמשים החדשה לאתגר ה-JWT
    public virtual DbSet<User> Users { get; set; } 

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // השארנו ריק כי ההגדרה מתבצעת ב-Program.cs כפי שסידרנו קודם
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // הגדרות שפה ותאימות למסד הנתונים MySQL
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        // הגדרת מבנה טבלת המשימות (Items)
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("items");
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        // הגדרת מבנה טבלת המשתמשים (Users)
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("users"); // שם הטבלה ב-MySQL Workbench
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Password).IsRequired().HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}