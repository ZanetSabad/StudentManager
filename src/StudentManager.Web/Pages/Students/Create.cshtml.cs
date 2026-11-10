using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StudentManager.Web.Data;
using StudentManager.Web.Models;
using StudentManager.Web.Options;
using StudentManager.Web.Services;

namespace StudentManager.Web.Pages.Students;

/// <summary>
/// Údaje z formuláře Nový student.
/// </summary>
// PROČ ne přímo třída Student: do Student by šlo podvrženým formulářem poslat i Status,
//   Upn nebo ReviewReason. Tady jsou jen pole, která smí vyplnit admin. Zbytek určí server.
public class StudentInput
{
    [Display(Name = "Jméno")]
    [Required(ErrorMessage = "Vyplňte jméno.")]
    [StringLength(100, ErrorMessage = "Jméno může mít nejvýše {1} znaků.")]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Příjmení")]
    [Required(ErrorMessage = "Vyplňte příjmení.")]
    [StringLength(100, ErrorMessage = "Příjmení může mít nejvýše {1} znaků.")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Identifikátor")]
    [StringLength(50, ErrorMessage = "Identifikátor může mít nejvýše {1} znaků.")]
    public string? StudentIdentifier { get; set; }

    // PROČ int? a [Required]: když admin nic nevybere, přijde prázdná hodnota.
    //   S int? se zobrazí naše česká zpráva místo anglické chyby převodu.
    [Display(Name = "Třída")]
    [Required(ErrorMessage = "Vyberte třídu.")]
    public int? SchoolClassId { get; set; }

    [Display(Name = "Školní rok")]
    [Required(ErrorMessage = "Vyberte školní rok.")]
    public int? SchoolYearId { get; set; }

    // PROČ limit 64, a ne 20: delší než 20 znaků jde uložit, jen se stavem Ke kontrole.
    [Display(Name = "Username")]
    [Required(ErrorMessage = "Vyplňte username.")]
    [StringLength(64, ErrorMessage = "Username může mít nejvýše {1} znaků.")]
    public string Username { get; set; } = string.Empty;

    [Display(Name = "Poznámka administrátora")]
    [StringLength(1000, ErrorMessage = "Poznámka může mít nejvýše {1} znaků.")]
    public string? AdminNote { get; set; }
}

/// <summary>
/// Stránka pro přidání jednoho studenta.
/// </summary>
public class CreateModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly UsernameGenerator _generator;
    private readonly UsernameValidator _validator;
    private readonly SchoolOptions _school;

    // Všechny služby předá aplikace sama (registrace v Program.cs).
    public CreateModel(
        AppDbContext context,
        UsernameGenerator generator,
        UsernameValidator validator,
        IOptions<SchoolOptions> schoolOptions)
    {
        _context = context;
        _generator = generator;
        _validator = validator;
        _school = schoolOptions.Value;
    }

    /// <summary>Hodnoty z formuláře.</summary>
    // PROČ [BindProperty]: při odeslání (POST) ASP.NET sám naplní Input z polí formuláře
    //   (pole se jmenují Input.FirstName, Input.LastName…).
    [BindProperty]
    public StudentInput Input { get; set; } = new();

    public SelectList SchoolClasses { get; set; } = default!;
    public SelectList SchoolYears { get; set; } = default!;

    /// <summary>Doména pro náhled UPN ve formuláři.</summary>
    public string EmailDomain => _school.EmailDomain;

    // Otevření prázdného formuláře.
    public async Task OnGetAsync()
    {
        // Předvybrat aktuální školní rok.
        var currentYear = await _context.SchoolYears.FirstOrDefaultAsync(y => y.IsCurrent);
        Input.SchoolYearId = currentYear?.Id;

        await LoadOptionsAsync();
    }

    /// <summary>
    /// Návrh username pro JavaScript ve formuláři. Adresa: /Students/Create?handler=SuggestUsername
    /// </summary>
    // PROČ na serveru: pravidla jsou jen v UsernameGenerator, v JavaScriptu nejsou podruhé.
    public async Task<JsonResult> OnGetSuggestUsernameAsync(string? firstName, string? lastName)
    {
        var taken = await LoadTakenUsernamesAsync();
        var result = _generator.Generate(firstName, lastName, taken);

        // Odešle se jako JSON: { "username": "...", "needsReview": true, "reviewReason": "..." }
        return new JsonResult(result);
    }

    // Odeslání formuláře.
    public async Task<IActionResult> OnPostAsync()
    {
        // Mezery na začátku a konci nikdy nechceme. Prázdný identifikátor = null
        // PROČ null: unikátní index hlídá jen vyplněné identifikátory (IS NOT NULL).
        //   Dva prázdné texty "" by se navzájem srazily.
        var firstName = Input.FirstName?.Trim() ?? string.Empty;
        var lastName = Input.LastName?.Trim() ?? string.Empty;
        var username = Input.Username?.Trim() ?? string.Empty;
        var identifier = string.IsNullOrWhiteSpace(Input.StudentIdentifier) ? null : Input.StudentIdentifier.Trim();
        var adminNote = string.IsNullOrWhiteSpace(Input.AdminNote) ? null : Input.AdminNote.Trim();

        // PROČ znovu vše kontrolovat: kontrole v prohlížeči nelze věřit (jde vypnout nebo obejít).
        // Povinná pole a délky už zkontroloval ASP.NET podle atributů ve StudentInput (ModelState).

        if (string.IsNullOrWhiteSpace(_school.EmailDomain))
        {
            ModelState.AddModelError(string.Empty,
                "V appsettings.json chybí doména (School:EmailDomain). Student nelze uložit.");
        }

        if (Input.SchoolClassId.HasValue &&
            !await _context.SchoolClasses.AnyAsync(c => c.Id == Input.SchoolClassId && c.IsActive))
        {
            ModelState.AddModelError("Input.SchoolClassId", "Vybraná třída neexistuje nebo není aktivní.");
        }

        if (Input.SchoolYearId.HasValue &&
            !await _context.SchoolYears.AnyAsync(y => y.Id == Input.SchoolYearId))
        {
            ModelState.AddModelError("Input.SchoolYearId", "Vybraný školní rok neexistuje.");
        }

        // Username kontrolujeme jen tehdy, když prošla základní kontrola (vyplněno, délka).
        // PROČ: jinak by se u pole ukázalo několik chyb najednou.
        if (ModelState.GetFieldValidationState("Input.Username") != ModelValidationState.Invalid)
        {
            var formatError = _validator.Validate(username);
            if (formatError is not null)
            {
                ModelState.AddModelError("Input.Username", formatError);
            }
            // PROČ stačí ==: sloupec Username má v databázi NOCASE, takže novak.jan = Novak.Jan.
            else if (await _context.Students.AnyAsync(s => s.Username == username))
            {
                // Nikdy tiše nepřečíslovat – admin musí vědět, jaké jméno student dostane.
                ModelState.AddModelError("Input.Username",
                    $"Username {username} je už obsazené. Zvolte jiné (nebo klikněte na Navrhnout znovu).");
            }
        }

        if (identifier is not null &&
            await _context.Students.AnyAsync(s => s.StudentIdentifier == identifier))
        {
            ModelState.AddModelError("Input.StudentIdentifier",
                $"Student s identifikátorem {identifier} už existuje.");
        }

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        // --- Stav studenta ---
        // Návrh generátoru spočítáme znovu na serveru (stejně jako ho dostal formulář).
        var taken = await LoadTakenUsernamesAsync();
        var suggestion = _generator.Generate(firstName, lastName, taken);

        StudentStatus status;
        string? reviewReason;

        // PROČ Ordinal (přesná shoda včetně velikosti písmen): i změna novak.jan místo Novak.Jan
        //   je ruční úprava adminem.
        if (string.Equals(username, suggestion.Username, StringComparison.Ordinal))
        {
            // Admin převzal návrh → platí verdikt generátoru.
            status = suggestion.NeedsReview ? StudentStatus.NeedsReview : StudentStatus.Draft;
            reviewReason = suggestion.ReviewReason;
        }
        else if (username.Length > UsernameGenerator.MaxLength)
        {
            // Ručně přepsané, ale moc dlouhé pro sAMAccountName.
            status = StudentStatus.NeedsReview;
            reviewReason = $"Delší než {UsernameGenerator.MaxLength} znaků (limit sAMAccountName).";
        }
        else
        {
            // Ručně přepsané → admin za jméno převzal odpovědnost.
            status = StudentStatus.Draft;
            reviewReason = null;
        }

        var upn = $"{username}@{_school.EmailDomain}";

        var student = new Student
        {
            FirstName = firstName,
            LastName = lastName,
            StudentIdentifier = identifier,
            Username = username,
            Upn = upn,
            Email = upn,
            Status = status,
            ReviewReason = reviewReason,
            AdminNote = adminNote,
            SchoolClassId = Input.SchoolClassId!.Value,
            SchoolYearId = Input.SchoolYearId!.Value
        };

        _context.Students.Add(student);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // PROČ: mezi kontrolou a uložením mohl stejné username nebo identifikátor
            //   uložit někdo jiný (např. druhý admin). Unikátní index v databázi to zastaví
            //   a my místo pádu aplikace zobrazíme hlášku.
            ModelState.AddModelError(string.Empty,
                "Studenta se nepodařilo uložit – username nebo identifikátor mezitím někdo použil. Zkontrolujte údaje a uložte znovu.");
            await LoadOptionsAsync();
            return Page();
        }

        // DALŠÍ KROK: zápis do auditu (ROZHODNUTI.md: každá změna dat se zapíše do auditu).

        // PROČ TempData: zpráva přežije přesměrování na jinou stránku a po zobrazení zmizí.
        TempData["SuccessMessage"] = "Student byl přidán";
        return RedirectToPage("./Index");
    }

    /// <summary>Všechna obsazená username z databáze.</summary>
    // DALŠÍ KROK: při tisících studentů načítat jen jména začínající základem (Novak.Jan…).
    // BUDOUCNOST: přidat i rezervovaná jména z AD (viz ROZHODNUTI.md, Úkoly před nasazením).
    private async Task<List<string>> LoadTakenUsernamesAsync()
    {
        return await _context.Students
            .Select(s => s.Username)
            .ToListAsync();
    }

    /// <summary>Naplní rozbalovací seznamy Třída a Školní rok.</summary>
    private async Task LoadOptionsAsync()
    {
        // Nabízíme jen aktivní třídy – do ukončené třídy se nový student nepřidává.
        var classes = await _context.SchoolClasses
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
        SchoolClasses = new SelectList(classes, nameof(SchoolClass.Id), nameof(SchoolClass.Name), Input.SchoolClassId);

        var years = await _context.SchoolYears
            .OrderByDescending(y => y.StartYear)
            .ToListAsync();
        SchoolYears = new SelectList(years, nameof(SchoolYear.Id), nameof(SchoolYear.Name), Input.SchoolYearId);
    }
}
