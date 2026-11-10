using StudentManager.Web.Models;

namespace StudentManager.Web.Services;

/// <summary>
/// Výsledek schválení.
/// </summary>
/// <param name="Approved">true = student je teď ve stavu Připraven.</param>
/// <param name="Error">Česky, proč schválit nešlo (při úspěchu null).</param>
public record ApprovalResult(bool Approved, string? Error)
{
    public static ApprovalResult Success() => new(true, null);
    public static ApprovalResult Fail(string error) => new(false, error);
}

/// <summary>
/// Schválení studenta: Koncept / Ke kontrole → Připraven (pravidla v docs/ROZHODNUTI.md).
/// </summary>
// PROČ samostatná služba bez databáze: pravidla jdou otestovat a použije je
//   i budoucí hromadné schválení. Uložení do databáze dělá stránka.
public class StudentApprovalService
{
    private readonly UsernameValidator _validator;

    public StudentApprovalService(UsernameValidator validator)
    {
        _validator = validator;
    }

    /// <summary>
    /// Zkusí studenta schválit. Při úspěchu změní Status na Ready a smaže ReviewReason.
    /// Při neúspěchu studenta nezmění.
    /// </summary>
    // PROČ pořadí stav → délka → tvar: admin má dostat co nejkonkrétnější hlášku.
    //   Dlouhé jméno má hlásit „delší než 20 znaků“, ne obecně „špatný tvar“.
    public ApprovalResult Approve(Student student)
    {
        var name = $"{student.LastName} {student.FirstName}";

        // 1) Stav. Připraven/Exportován typicky = dvojklik nebo dva admini najednou → přátelská hláška.
        switch (student.Status)
        {
            case StudentStatus.Ready:
                return ApprovalResult.Fail($"Student {name} už je schválen.");
            case StudentStatus.Exported:
                return ApprovalResult.Fail($"Student {name} už je exportován – schválení se ho netýká.");
            case StudentStatus.Draft:
            case StudentStatus.NeedsReview:
                break;
            default:
                // BUDOUCNOST (fáze 4): nové stavy – rozhodnout, jestli jdou schválit.
                return ApprovalResult.Fail($"Studenta {name} ve stavu {student.Status.ToCzech()} nelze schválit.");
        }

        // 2) Délka. Kolizní číslo (Novak.Jan2) se do délky počítá – je součástí username.
        if (student.Username.Length > UsernameGenerator.MaxLength)
        {
            return ApprovalResult.Fail(
                $"Studenta {name} nelze schválit: username {student.Username} je delší než {UsernameGenerator.MaxLength} znaků " +
                "(limit sAMAccountName). Zkraťte ho v úpravě studenta.");
        }

        // 3) Tvar. Starší záznam mohl vzniknout dřív, než platila pravidla pro tvar.
        var formatError = _validator.Validate(student.Username);
        if (formatError is not null)
        {
            return ApprovalResult.Fail(
                $"Studenta {name} nelze schválit: {formatError} Opravte username v úpravě studenta.");
        }

        // Kolize schválit lze – admin ověřil, že jde o jinou osobu.
        student.Status = StudentStatus.Ready;
        student.ReviewReason = null;
        return ApprovalResult.Success();
    }
}
