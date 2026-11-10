using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StudentManager.Web.Data;
using StudentManager.Web.Options;
using StudentManager.Web.Services;

namespace StudentManager.Web.Pages.Students;

/// <summary>
/// Společný základ stránek Nový student (Create) a Upravit studenta (Edit).
/// </summary>
// PROČ abstract: tahle třída sama stránkou není. Create a Edit z ní dědí
//   (": StudentFormPageModel") a dostanou všechno, co je tady – formulář, seznamy,
//   kontroly. Každá pak doplní jen to, čím se liší.
// PROČ: kontroly na serveru jsou pro obě stránky stejné. Kdyby byly ve dvou kopiích,
//   dřív nebo později by se oprava udělala jen v jedné.
public abstract class StudentFormPageModel : PageModel
{
    protected StudentFormPageModel(
        AppDbContext context,
        UsernameGenerator generator,
        UsernameValidator validator,
        StudentStatusEvaluator statusEvaluator,
        IOptions<SchoolOptions> schoolOptions)
    {
        Context = context;
        Generator = generator;
        Validator = validator;
        StatusEvaluator = statusEvaluator;
        School = schoolOptions.Value;
    }

    // PROČ protected: vidí je jen tahle třída a stránky, které z ní dědí. Formulář ani nikdo jiný ne.
    protected AppDbContext Context { get; }
    protected UsernameGenerator Generator { get; }
    protected UsernameValidator Validator { get; }
    protected StudentStatusEvaluator StatusEvaluator { get; }
    protected SchoolOptions School { get; }

    /// <summary>Hodnoty z formuláře.</summary>
    // PROČ [BindProperty]: při odeslání (POST) ASP.NET sám naplní Input z polí formuláře
    //   (pole se jmenují Input.FirstName, Input.LastName…).
    [BindProperty]
    public StudentInput Input { get; set; } = new();

    public SelectList SchoolClasses { get; set; } = default!;
    public SelectList SchoolYears { get; set; } = default!;

    /// <summary>Doména pro náhled UPN ve formuláři.</summary>
    public string EmailDomain => School.EmailDomain;

    /// <summary>true = lze upravit jen poznámku (exportovaný student). Ostatní pole jsou jen ke čtení.</summary>
    // PROČ virtual: stránka Edit tuto vlastnost přepíše (override) vlastním pravidlem.
    public virtual bool IsLocked => false;

    /// <summary>Nápověda pod polem Username.</summary>
    public virtual string UsernameHint =>
        "Vyplní se samo podle jména a příjmení. Můžete ho ručně přepsat.";

    /// <summary>
    /// Návrh username jako JSON: { "username": "...", "needsReview": true, "reviewReason": "..." }
    /// </summary>
    /// <param name="excludeStudentId">Upravovaný student – jeho vlastní username se nepočítá jako obsazené.</param>
    // PROČ na serveru: pravidla jsou jen v UsernameGenerator, v JavaScriptu nejsou podruhé.
    protected async Task<JsonResult> SuggestUsernameAsync(string? firstName, string? lastName, int? excludeStudentId)
    {
        var taken = await LoadTakenUsernamesAsync(excludeStudentId);
        var result = Generator.Generate(firstName, lastName, taken);
        return new JsonResult(result);
    }

    /// <summary>Ořízne mezery. Prázdný identifikátor a poznámka = null.</summary>
    // PROČ null u identifikátoru: unikátní index hlídá jen vyplněné identifikátory (IS NOT NULL).
    //   Dva prázdné texty "" by se navzájem srazily.
    protected void NormalizeInput()
    {
        // PROČ "?." i u textu bez otazníku: když pole přijde prázdné, ASP.NET do něj dá null.
        Input.FirstName = Input.FirstName?.Trim() ?? string.Empty;
        Input.LastName = Input.LastName?.Trim() ?? string.Empty;
        Input.Username = Input.Username?.Trim() ?? string.Empty;
        Input.StudentIdentifier = string.IsNullOrWhiteSpace(Input.StudentIdentifier) ? null : Input.StudentIdentifier.Trim();
        Input.AdminNote = string.IsNullOrWhiteSpace(Input.AdminNote) ? null : Input.AdminNote.Trim();
    }

    /// <summary>
    /// Kontroly na serveru. Chyby zapíše do ModelState k jednotlivým polím.
    /// </summary>
    /// <param name="studentId">Upravovaný student (u nového null). Sám se sebou nekoliduje.</param>
    /// <param name="currentClassId">Současná třída upravovaného studenta – smí zůstat, i když už není aktivní.</param>
    /// <param name="checkUsername">false = username se nezměnilo, nekontroluje se.</param>
    // PROČ znovu vše kontrolovat: kontrole v prohlížeči nelze věřit (jde vypnout nebo obejít).
    // Povinná pole a délky už zkontroloval ASP.NET podle atributů ve StudentInput (ModelState).
    protected async Task ValidateAsync(int? studentId, int? currentClassId, bool checkUsername)
    {
        if (string.IsNullOrWhiteSpace(School.EmailDomain))
        {
            ModelState.AddModelError(string.Empty,
                "V appsettings.json chybí doména (School:EmailDomain). Studenta nelze uložit.");
        }

        if (Input.SchoolClassId.HasValue &&
            !await Context.SchoolClasses.AnyAsync(c =>
                c.Id == Input.SchoolClassId && (c.IsActive || c.Id == currentClassId)))
        {
            ModelState.AddModelError("Input.SchoolClassId", "Vybraná třída neexistuje nebo není aktivní.");
        }

        if (Input.SchoolYearId.HasValue &&
            !await Context.SchoolYears.AnyAsync(y => y.Id == Input.SchoolYearId))
        {
            ModelState.AddModelError("Input.SchoolYearId", "Vybraný školní rok neexistuje.");
        }

        // Username kontrolujeme jen tehdy, když prošla základní kontrola (vyplněno, délka).
        // PROČ: jinak by se u pole ukázalo několik chyb najednou.
        if (checkUsername &&
            ModelState.GetFieldValidationState("Input.Username") != ModelValidationState.Invalid)
        {
            var formatError = Validator.Validate(Input.Username);
            if (formatError is not null)
            {
                ModelState.AddModelError("Input.Username", formatError);
            }
            // PROČ stačí ==: sloupec Username má v databázi NOCASE, takže novak.jan = Novak.Jan.
            // PROČ s.Id != studentId: upravovaný student nesmí kolidovat sám se sebou.
            //   U nového studenta je studentId null a podmínka platí pro všechny.
            else if (await Context.Students.AnyAsync(s => s.Username == Input.Username && s.Id != studentId))
            {
                // Nikdy tiše nepřečíslovat – admin musí vědět, jaké jméno student dostane.
                ModelState.AddModelError("Input.Username",
                    $"Username {Input.Username} je už obsazené. Zvolte jiné (nebo klikněte na Navrhnout znovu).");
            }
        }

        if (Input.StudentIdentifier is not null &&
            await Context.Students.AnyAsync(s => s.StudentIdentifier == Input.StudentIdentifier && s.Id != studentId))
        {
            ModelState.AddModelError("Input.StudentIdentifier",
                $"Student s identifikátorem {Input.StudentIdentifier} už existuje.");
        }
    }

    /// <summary>Stav pro username z formuláře (generátor se spustí znovu na serveru).</summary>
    protected async Task<StatusDecision> EvaluateStatusAsync(int? studentId)
    {
        var taken = await LoadTakenUsernamesAsync(studentId);
        var suggestion = Generator.Generate(Input.FirstName, Input.LastName, taken);
        return StatusEvaluator.Evaluate(Input.Username, suggestion);
    }

    /// <summary>UPN (a e-mail): Novak.Jan@sou-sos.cz</summary>
    protected string BuildUpn(string username) => $"{username}@{School.EmailDomain}";

    /// <summary>
    /// Uloží změny. Když databáze odmítne duplicitu, vrátí false a zapíše chybu do formuláře.
    /// </summary>
    // DALŠÍ KROK: zápis do auditu (ROZHODNUTI.md: každá změna dat se zapíše do auditu).
    protected async Task<bool> TrySaveAsync()
    {
        try
        {
            await Context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            // PROČ: mezi kontrolou a uložením mohl stejné username nebo identifikátor
            //   uložit někdo jiný (např. druhý admin). Unikátní index v databázi to zastaví
            //   a my místo pádu aplikace zobrazíme hlášku.
            ModelState.AddModelError(string.Empty,
                "Studenta se nepodařilo uložit – username nebo identifikátor mezitím někdo použil. Zkontrolujte údaje a uložte znovu.");
            return false;
        }
    }

    /// <summary>Všechna obsazená username z databáze (kromě upravovaného studenta).</summary>
    // DALŠÍ KROK: při tisících studentů načítat jen jména začínající základem (Novak.Jan…).
    // BUDOUCNOST: přidat i rezervovaná jména z AD (viz ROZHODNUTI.md, Úkoly před nasazením).
    protected async Task<List<string>> LoadTakenUsernamesAsync(int? excludeStudentId)
    {
        return await Context.Students
            .Where(s => s.Id != excludeStudentId)
            .Select(s => s.Username)
            .ToListAsync();
    }

    /// <summary>Naplní rozbalovací seznamy Třída a Školní rok.</summary>
    /// <param name="keepClassId">Současná třída upravovaného studenta – zobrazí se, i když není aktivní.</param>
    protected async Task LoadOptionsAsync(int? keepClassId = null)
    {
        // Nabízíme jen aktivní třídy – do ukončené třídy se student nepřidává ani nepřesouvá.
        var classes = await Context.SchoolClasses
            .Where(c => c.IsActive || c.Id == keepClassId)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, Name = c.IsActive ? c.Name : c.Name + " (neaktivní)" })
            .ToListAsync();
        SchoolClasses = new SelectList(classes, "Id", "Name", Input.SchoolClassId);

        var years = await Context.SchoolYears
            .OrderByDescending(y => y.StartYear)
            .ToListAsync();
        SchoolYears = new SelectList(years, "Id", "Name", Input.SchoolYearId);
    }
}
