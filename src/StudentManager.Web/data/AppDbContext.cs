using Microsoft.EntityFrameworkCore;
using StudentManager.Web.Models;

namespace StudentManager.Web.Data;

/// <summary>
/// Přes tuhle třídu bude mluvit aplikace s databází 
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Toto je způsob jak třída dostane při vytvoření nastavení (jaká databáze, kde leží, ...) Nastavení předává app sama
    // Tyhle řádky se píšou vždycky stejně
    public DbSet<Student> Students => Set<Student>();

    public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();

    public DbSet<SchoolYear> SchoolYears => Set<SchoolYear>();

    // pravidla tabulek (unikátní Username, StudentIdentifier…).
    /// Pravidla tabulek: unikátnost, vazby, chování při mazání.
    /// EF Core tuto metodu zavolá sám, když sestavuje model databáze.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Student>(entity =>
        {
            // PROČ NOCASE: v AD je Novak.Jan a novak.jan stejný účet.
            //   SQLite jinak velká a malá písmena rozlišuje.
            entity.Property(s => s.Username).UseCollation("NOCASE");

            // Uživatelské jméno musí být unikátní.
            entity.HasIndex(s => s.Username).IsUnique();

            // Identifikátor unikátní, ale jen pokud je vyplněný.
            entity.HasIndex(s => s.StudentIdentifier)
                  .IsUnique()
                  .HasFilter("StudentIdentifier IS NOT NULL");

            // Vazba na třídu. PROČ Restrict: třídu se studenty nelze smazat.
            entity.HasOne(s => s.SchoolClass)
                  .WithMany(c => c.Students)
                  .HasForeignKey(s => s.SchoolClassId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Vazba na školní rok. Stejné pravidlo.
            entity.HasOne(s => s.SchoolYear)
                  .WithMany()
                  .HasForeignKey(s => s.SchoolYearId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SchoolClass>(entity =>
        {
            entity.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<SchoolYear>(entity =>
        {
            entity.HasIndex(y => y.Name).IsUnique();
        });
    }
    // TODO: automatický zápis změn do auditu.
}