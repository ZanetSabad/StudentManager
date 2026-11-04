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

    // TODO: pravidla tabulek (unikátní Username, StudentIdentifier…).
    // TODO: automatický zápis změn do auditu.
}