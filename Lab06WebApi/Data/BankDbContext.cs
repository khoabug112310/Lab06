using System;
using System.Collections.Generic;
using Lab06WebApi.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lab06WebApi.Data;

public partial class BankDbContext : DbContext
{
    public BankDbContext()
    {
    }

    public BankDbContext(DbContextOptions<BankDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Transfer> Transfers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Name=ConnectionStrings:DefaultConnection");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Accounts__3214EC0709015A69");

            entity.Property(e => e.AccountNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Balance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.RefreshToken)
                .HasMaxLength(255);
        });

        modelBuilder.Entity<Transfer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Transfer__3214EC07F24B009E");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TransferDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.FromAccount).WithMany(p => p.TransferFromAccounts)
                .HasForeignKey(d => d.FromAccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FromAccount");

            entity.HasOne(d => d.ToAccount).WithMany(p => p.TransferToAccounts)
                .HasForeignKey(d => d.ToAccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ToAccount");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
