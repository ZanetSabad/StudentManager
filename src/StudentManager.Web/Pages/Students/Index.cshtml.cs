using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StudentManager.Web.Data;
using StudentManager.Web.Models;

namespace StudentManager.Web.Pages.Students;

/// <summary>
/// Seznam studentů.
/// </summary>
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    // Databázi stránce předá aplikace sama (registrace AddDbContext v Program.cs).
    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Studenti k zobrazení v tabulce.</summary>
    public List<Student> Students { get; set; } = new();

    // Spustí se, když prohlížeč stránku otevře (požadavek GET).
    public async Task OnGetAsync()
    {
        Students = await _context.Students
            .Include(s => s.SchoolClass)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync();
    }

    // DALŠÍ KROK: vyhledávání a filtry (třída, stav, školní rok).
}