using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StudentManager.Web.Data;
using StudentManager.Web.Models;
using StudentManager.Web.Services;

namespace StudentManager.Web.Pages.Students;

/// <summary>
/// Seznam studentů s vyhledáváním a filtry.
/// </summary>
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly StudentApprovalService _approvalService;

    // Databázi a služby stránce předá aplikace sama (registrace v Program.cs).
    public IndexModel(AppDbContext context, StudentApprovalService approvalService)
    {
        _context = context;
        _approvalService = approvalService;
    }

    /// <summary>Studenti k zobrazení v tabulce (už vyfiltrovaní).</summary>
    public List<Student> Students { get; set; } = new();

    // --- Hodnoty filtrů ---
    // PROČ SupportsGet = true: filtry chodí v adrese stránky (?Search=novak&Status=Ready).
    //   Bez toho by ASP.NET plnil vlastnosti jen při odeslání formuláře metodou POST.
    // PROČ otazník (int?, StudentStatus?): prázdná hodnota = filtr se nepoužije.

    /// <summary>Hledaný text (jméno, příjmení, username, třída).</summary>
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    /// <summary>Vybraný školní rok.</summary>
    [BindProperty(SupportsGet = true)]
    public int? SchoolYearId { get; set; }

    /// <summary>Vybraná třída.</summary>
    [BindProperty(SupportsGet = true)]
    public int? SchoolClassId { get; set; }

    /// <summary>Vybraný stav.</summary>
    [BindProperty(SupportsGet = true)]
    public StudentStatus? Status { get; set; }

    // --- Položky rozbalovacích seznamů ---
    public SelectList SchoolYears { get; set; } = default!;
    public SelectList SchoolClasses { get; set; } = default!;
    public SelectList Statuses { get; set; } = default!;

    // Spustí se, když prohlížeč stránku otevře (požadavek GET).
    public async Task OnGetAsync()
    {
        await LoadFilterOptionsAsync();

        // PROČ IQueryable: dotaz se zatím jen skládá, do databáze se nic neposílá.
        //   Každý vyplněný filtr přidá další podmínku (jako Where-Object v rouře).
        //   Teprve ToListAsync() pošle do databáze jeden výsledný SQL dotaz.
        IQueryable<Student> query = _context.Students
            .Include(s => s.SchoolClass);

        if (!string.IsNullOrWhiteSpace(Search))
        {
            // PROČ LIKE: v SQLite ignoruje velikost písmen (jen u znaků bez diakritiky).
            // DALŠÍ KROK: hledání bez ohledu na diakritiku (Novák = novak).
            var pattern = $"%{Search.Trim()}%";
            query = query.Where(s =>
                EF.Functions.Like(s.FirstName, pattern) ||
                EF.Functions.Like(s.LastName, pattern) ||
                EF.Functions.Like(s.Username, pattern) ||
                (s.SchoolClass != null && EF.Functions.Like(s.SchoolClass.Name, pattern)));
        }

        if (SchoolYearId.HasValue)
        {
            query = query.Where(s => s.SchoolYearId == SchoolYearId.Value);
        }

        if (SchoolClassId.HasValue)
        {
            query = query.Where(s => s.SchoolClassId == SchoolClassId.Value);
        }

        if (Status.HasValue)
        {
            query = query.Where(s => s.Status == Status.Value);
        }

        Students = await query
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync();
    }

    /// <summary>
    /// Tlačítko Schválit u studenta. Adresa: /Students?handler=Approve&amp;id=5 (POST).
    /// </summary>
    // PROČ POST, ne odkaz (GET): schválení mění data. Odkaz by šlo spustit omylem
    //   (náhled odkazu, vyhledávač). POST formulář navíc chrání antiforgery token,
    //   který tag helper <form> přidá sám.
    // PROČ filtry z formuláře: Search, Status… mají [BindProperty], takže je ASP.NET
    //   naplní i ze skrytých polí formuláře. Po schválení se vrátíme na stejně
    //   vyfiltrovaný seznam.
    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student is null)
        {
            return NotFound();
        }

        var result = _approvalService.Approve(student);

        if (result.Approved)
        {
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Student {student.LastName} {student.FirstName} byl schválen.";
        }
        else
        {
            TempData["ErrorMessage"] = result.Error;
        }

        // DALŠÍ KROK: zápis do auditu.

        // PROČ jen hodnoty filtrů, ne celá návratová adresa: adresu by šlo podvrhnout
        //   a přesměrovat admina na cizí web. Takhle vždy skončíme na seznamu studentů.
        return RedirectToPage("./Index", new { Search, SchoolYearId, SchoolClassId, Status });
    }

    /// <summary>Naplní rozbalovací seznamy Školní rok, Třída a Stav.</summary>
    private async Task LoadFilterOptionsAsync()
    {
        var years = await _context.SchoolYears
            .OrderByDescending(y => y.StartYear)
            .ToListAsync();
        // SelectList(položky, co se odešle, co se zobrazí, co je vybráno)
        SchoolYears = new SelectList(years, nameof(SchoolYear.Id), nameof(SchoolYear.Name), SchoolYearId);

        var classes = await _context.SchoolClasses
            .OrderBy(c => c.Name)
            .ToListAsync();
        SchoolClasses = new SelectList(classes, nameof(SchoolClass.Id), nameof(SchoolClass.Name), SchoolClassId);

        // Stavy nejsou v databázi, bereme je přímo z enumu StudentStatus.
        var statuses = Enum.GetValues<StudentStatus>()
            .Select(s => new { Value = s.ToString(), Text = s.ToCzech() });
        Statuses = new SelectList(statuses, "Value", "Text", Status?.ToString());
    }
}
