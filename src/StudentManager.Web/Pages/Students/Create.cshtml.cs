using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StudentManager.Web.Data;
using StudentManager.Web.Models;
using StudentManager.Web.Options;
using StudentManager.Web.Services;

namespace StudentManager.Web.Pages.Students;

/// <summary>
/// Stránka pro přidání jednoho studenta.
/// </summary>
// Formulář, seznamy a kontroly jsou ve společném základu StudentFormPageModel.
public class CreateModel : StudentFormPageModel
{
    // PROČ ": base(...)": služby, které dostaneme od aplikace, jen předáme dál do základní třídy.
    public CreateModel(
        AppDbContext context,
        UsernameGenerator generator,
        UsernameValidator validator,
        StudentStatusEvaluator statusEvaluator,
        IOptions<SchoolOptions> schoolOptions)
        : base(context, generator, validator, statusEvaluator, schoolOptions)
    {
    }

    // Otevření prázdného formuláře.
    public async Task OnGetAsync()
    {
        // Předvybrat aktuální školní rok.
        var currentYear = await Context.SchoolYears.FirstOrDefaultAsync(y => y.IsCurrent);
        Input.SchoolYearId = currentYear?.Id;

        await LoadOptionsAsync();
    }

    /// <summary>Návrh username pro JavaScript. Adresa: /Students/Create?handler=SuggestUsername</summary>
    public Task<JsonResult> OnGetSuggestUsernameAsync(string? firstName, string? lastName)
        => SuggestUsernameAsync(firstName, lastName, excludeStudentId: null);

    // Odeslání formuláře.
    public async Task<IActionResult> OnPostAsync()
    {
        NormalizeInput();
        await ValidateAsync(studentId: null, currentClassId: null, checkUsername: true);

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        var decision = await EvaluateStatusAsync(studentId: null);
        var upn = BuildUpn(Input.Username);

        var student = new Student
        {
            FirstName = Input.FirstName,
            LastName = Input.LastName,
            StudentIdentifier = Input.StudentIdentifier,
            Username = Input.Username,
            Upn = upn,
            Email = upn,
            Status = decision.Status,
            ReviewReason = decision.ReviewReason,
            AdminNote = Input.AdminNote,
            SchoolClassId = Input.SchoolClassId!.Value,
            SchoolYearId = Input.SchoolYearId!.Value
        };

        Context.Students.Add(student);

        if (!await TrySaveAsync())
        {
            await LoadOptionsAsync();
            return Page();
        }

        // PROČ TempData: zpráva přežije přesměrování na jinou stránku a po zobrazení zmizí.
        TempData["SuccessMessage"] = "Student byl přidán";
        return RedirectToPage("./Index");
    }
}
