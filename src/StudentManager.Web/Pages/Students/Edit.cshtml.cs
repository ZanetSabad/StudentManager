using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StudentManager.Web.Data;
using StudentManager.Web.Models;
using StudentManager.Web.Options;
using StudentManager.Web.Services;

namespace StudentManager.Web.Pages.Students;

/// <summary>
/// Úprava existujícího studenta. Adresa: /Students/Edit/5
/// </summary>
// Formulář, seznamy a kontroly jsou ve společném základu StudentFormPageModel.
// PROČ id jako parametr metody (z adresy), ne z formuláře: skryté pole s id by šlo
//   v prohlížeči přepsat a uložit tak změny k jinému studentovi.
public class EditModel : StudentFormPageModel
{
    public EditModel(
        AppDbContext context,
        UsernameGenerator generator,
        UsernameValidator validator,
        StudentStatusEvaluator statusEvaluator,
        IOptions<SchoolOptions> schoolOptions)
        : base(context, generator, validator, statusEvaluator, schoolOptions)
    {
    }

    /// <summary>Student, jak je teď uložený v databázi (pro nadpis, stav a důvod kontroly).</summary>
    public Student Student { get; set; } = default!;

    /// <summary>Exportovaný student má účet v AD → lze upravit jen poznámku.</summary>
    // PROČ override: přepisuje pravidlo ze základní třídy (tam je vždy false).
    // BUDOUCNOST (fáze 4): změna jména/username exportovaného studenta i v AD.
    public override bool IsLocked => Student.Status == StudentStatus.Exported;

    public override string UsernameHint =>
        "Změna username znovu vyhodnotí stav studenta. Nový návrh podle jména: Navrhnout znovu.";

    // Otevření formuláře s údaji studenta.
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var student = await Context.Students.FindAsync(id);
        if (student is null)
        {
            return NotFound();
        }

        Student = student;
        FillInputFrom(student);
        await LoadOptionsAsync(keepClassId: student.SchoolClassId);
        return Page();
    }

    /// <summary>Návrh username pro JavaScript. Adresa: /Students/Edit/5?handler=SuggestUsername</summary>
    // PROČ id: vlastní username upravovaného studenta se nesmí počítat jako obsazené,
    //   jinak by generátor Novákovi Janovi navrhl Novak.Jan2 kvůli němu samotnému.
    public Task<JsonResult> OnGetSuggestUsernameAsync(int id, string? firstName, string? lastName)
        => SuggestUsernameAsync(firstName, lastName, excludeStudentId: id);

    // Odeslání formuláře.
    public async Task<IActionResult> OnPostAsync(int id)
    {
        // PROČ znovu z databáze: stav (např. Exportován) mohl mezitím někdo změnit.
        var student = await Context.Students.FindAsync(id);
        if (student is null)
        {
            return NotFound();
        }

        Student = student;

        if (IsLocked)
        {
            return await SaveAdminNoteOnlyAsync(student);
        }

        NormalizeInput();

        // PROČ Ordinal: i změna velikosti písmen (Novak.Jan → novak.jan) je změna username.
        var usernameChanged = !string.Equals(Input.Username, student.Username, StringComparison.Ordinal);

        // Nezměněné username se znovu nekontroluje – starý záznam nesmí zablokovat úpravu jiných polí.
        await ValidateAsync(studentId: id, currentClassId: student.SchoolClassId, checkUsername: usernameChanged);

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(keepClassId: student.SchoolClassId);
            return Page();
        }

        // PROČ "student." a ne nový objekt: EF Core si načteného studenta hlídá
        //   a při uložení pošle do databáze jen změněné sloupce (UPDATE).
        student.FirstName = Input.FirstName;
        student.LastName = Input.LastName;
        student.StudentIdentifier = Input.StudentIdentifier;
        student.SchoolClassId = Input.SchoolClassId!.Value;
        student.SchoolYearId = Input.SchoolYearId!.Value;
        student.AdminNote = Input.AdminNote;

        // Stav, důvod, UPN a e-mail se mění JEN při změně username.
        // PROČ: student ve stavu Připraven s novým jménem musí projít kontrolou znovu.
        //   Změna jen jména, třídy apod. stav nemění.
        if (usernameChanged)
        {
            var decision = await EvaluateStatusAsync(studentId: id);
            var upn = BuildUpn(Input.Username);

            student.Username = Input.Username;
            student.Upn = upn;
            student.Email = upn;
            student.Status = decision.Status;
            student.ReviewReason = decision.ReviewReason;
        }

        return await SaveAndRedirectAsync(student);
    }

    /// <summary>Exportovaný student: uloží se jen poznámka, vše ostatní se ignoruje.</summary>
    private async Task<IActionResult> SaveAdminNoteOnlyAsync(Student student)
    {
        // PROČ: ostatní pole jsou ve formuláři zakázaná (disabled) a prohlížeč je neposílá.
        //   Chyby „Vyplňte jméno“ apod. proto u nich nedávají smysl – zahodíme je.
        //   A i kdyby je někdo poslal podvrženým formulářem, nepoužijeme je.
        foreach (var key in ModelState.Keys.Where(k => k != "Input.AdminNote").ToList())
        {
            ModelState.Remove(key);
        }

        var note = string.IsNullOrWhiteSpace(Input.AdminNote) ? null : Input.AdminNote.Trim();

        if (ModelState.IsValid)
        {
            student.AdminNote = note;
            return await SaveAndRedirectAsync(student);
        }

        // Chyba (např. moc dlouhá poznámka) → formulář znovu, ostatní pole z databáze.
        FillInputFrom(student);
        Input.AdminNote = note;
        await LoadOptionsAsync(keepClassId: student.SchoolClassId);
        return Page();
    }

    /// <summary>Uloží změny a přesměruje na seznam, při chybě vrátí formulář.</summary>
    private async Task<IActionResult> SaveAndRedirectAsync(Student student)
    {
        if (!await TrySaveAsync())
        {
            // PROČ ReloadAsync: vrátí studenta do stavu z databáze, aby nadpis stránky
            //   neukazoval neuložené hodnoty (formulář si je drží zvlášť).
            await Context.Entry(student).ReloadAsync();
            await LoadOptionsAsync(keepClassId: student.SchoolClassId);
            return Page();
        }

        TempData["SuccessMessage"] = "Změny byly uloženy";
        return RedirectToPage("./Index");
    }

    /// <summary>Předvyplní formulář údaji studenta.</summary>
    private void FillInputFrom(Student student)
    {
        Input = new StudentInput
        {
            FirstName = student.FirstName,
            LastName = student.LastName,
            StudentIdentifier = student.StudentIdentifier,
            SchoolClassId = student.SchoolClassId,
            SchoolYearId = student.SchoolYearId,
            Username = student.Username,
            AdminNote = student.AdminNote
        };
    }
}
