using StudentManager.Web.Models;

namespace StudentManager.Web.Services;

/// <summary>
/// Výsledek vyhodnocení: jaký stav má student dostat a proč.
/// </summary>
/// <param name="Status">Draft nebo NeedsReview.</param>
/// <param name="ReviewReason">Česky, proč je potřeba kontrola (u Draft null).</param>
public record StatusDecision(StudentStatus Status, string? ReviewReason);

/// <summary>
/// Určí stav studenta podle username, které admin ukládá (pravidla v docs/ROZHODNUTI.md).
/// </summary>
// PROČ samostatná služba: stejné pravidlo platí pro Nový student, Úpravu i budoucí import.
//   Je to čistá funkce bez databáze, takže jde snadno otestovat.
public class StudentStatusEvaluator
{
    /// <summary>
    /// Vyhodnotí stav.
    /// </summary>
    /// <param name="username">Username, které admin ukládá (už oříznuté o mezery).</param>
    /// <param name="suggestion">Co pro stejné jméno a příjmení navrhl UsernameGenerator.</param>
    public StatusDecision Evaluate(string username, UsernameResult suggestion)
    {
        // PROČ Ordinal (přesná shoda včetně velikosti písmen): i změna novak.jan místo Novak.Jan
        //   je ruční úprava adminem.
        if (string.Equals(username, suggestion.Username, StringComparison.Ordinal))
        {
            // Admin převzal návrh → platí verdikt generátoru.
            return suggestion.NeedsReview
                ? new StatusDecision(StudentStatus.NeedsReview, suggestion.ReviewReason)
                : new StatusDecision(StudentStatus.Draft, null);
        }

        // Ručně přepsané, ale moc dlouhé pro sAMAccountName.
        if (username.Length > UsernameGenerator.MaxLength)
        {
            return new StatusDecision(StudentStatus.NeedsReview,
                $"Delší než {UsernameGenerator.MaxLength} znaků (limit sAMAccountName).");
        }

        // Ručně přepsané → admin za jméno převzal odpovědnost.
        return new StatusDecision(StudentStatus.Draft, null);
    }
}
