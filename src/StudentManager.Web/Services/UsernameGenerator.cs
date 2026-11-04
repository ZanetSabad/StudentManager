using System.Globalization;
using System.Text;

namespace StudentManager.Web.Services;

/// <summary>
/// Výsledek generování uživatelského jména.
/// </summary>
/// <param name="Username">Uživatelské jméno, např. Novak.Jan. Může být prázdné.</param>
/// <param name="NeedsReview">true = student má dostat stav Ke kontrole.</param>
/// <param name="ReviewReason">Česky, proč je potřeba kontrola (jinak null).</param>
public record UsernameResult(string Username, bool NeedsReview, string? ReviewReason);

/// <summary>
/// Vytvoří uživatelské jméno ve tvaru Prijmeni.Jmeno podle pravidel v docs/ROZHODNUTI.md.
/// </summary>
// PROČ samostatná služba: pravidla jsou na jednom místě a jdou otestovat bez databáze a webu.
// DALŠÍ KROK: řešení kolizí (Novak.Jan2, Novak.Jan3…) – bude potřebovat databázi.
// BUDOUCNOST: tabulka náhrad a limit by mohly jít do Nastavení.
public class UsernameGenerator
{
    /// <summary>Limit délky sAMAccountName v Active Directory.</summary>
    public const int MaxLength = 20;

    // Znaky, podle kterých se jméno dělí na části. Bere se jen první část.
    private static readonly char[] PartSeparators = { ' ', '\t', '-', '–' };

    // Tabulka náhrad zvláštních písmen.
    // PROČ ručně: tato písmena nejsou „písmeno + háček“, takže je běžné
    //   odstranění diakritiky nezvládne (ł není l s čárkou, ale samostatné písmeno).
    private static readonly Dictionary<char, string> Replacements = new()
    {
        ['ł'] = "l",  ['Ł'] = "L",
        ['ß'] = "ss",
        ['ø'] = "o",  ['Ø'] = "O",
        ['đ'] = "d",  ['Đ'] = "D",
        ['æ'] = "ae", ['Æ'] = "AE",
        ['œ'] = "oe", ['Œ'] = "OE",
    };

    /// <summary>
    /// Z křestního jména a příjmení vytvoří uživatelské jméno.
    /// </summary>
    public UsernameResult Generate(string? firstName, string? lastName)
    {
        var reasons = new List<string>();

        var last = CleanPart(lastName, out bool lastLostLetter);
        var first = CleanPart(firstName, out bool firstLostLetter);

        if (lastLostLetter || firstLostLetter)
        {
            reasons.Add("Při úpravě bylo vypuštěno písmeno, které není v tabulce náhrad.");
        }

        // PROČ celý username prázdný: ".Jan" nebo "Jan" by vypadalo platně, ale neodpovídá pravidlu.
        if (last.Length == 0 || first.Length == 0)
        {
            reasons.Add("Jméno nebo příjmení je po úpravě prázdné.");
            return new UsernameResult(string.Empty, true, string.Join(" ", reasons));
        }

        var username = $"{last}.{first}";

        if (username.Length > MaxLength)
        {
            reasons.Add($"Delší než {MaxLength} znaků (limit sAMAccountName).");
        }

        return reasons.Count == 0
            ? new UsernameResult(username, false, null)
            : new UsernameResult(username, true, string.Join(" ", reasons));
    }

    /// <summary>
    /// Upraví jednu část (jméno nebo příjmení): první část, náhrady, bez diakritiky,
    /// jen písmena A–Z, první písmeno velké.
    /// </summary>
    /// <param name="lostLetter">true, pokud se vypustilo písmeno, které není v tabulce náhrad.</param>
    private static string CleanPart(string? value, out bool lostLetter)
    {
        lostLetter = false;

        // 1) Jen první část: "Nováková Svobodová" → "Nováková", "Novák-Svoboda" → "Novák".
        var firstPart = (value ?? string.Empty)
            .Split(PartSeparators, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? string.Empty;

        // 2) Tabulka náhrad: ł → l, ß → ss…
        var replaced = new StringBuilder();
        foreach (var c in firstPart)
        {
            if (Replacements.TryGetValue(c, out var replacement))
                replaced.Append(replacement);
            else
                replaced.Append(c);
        }

        // 3) Odstranění diakritiky.
        // PROČ FormD: rozloží "ř" na "r" + samostatný háček. Háček (NonSpacingMark) pak zahodíme.
        var decomposed = replaced.ToString().Normalize(NormalizationForm.FormD);

        // 4) Ponechat jen písmena A–Z a a–z.
        var letters = new StringBuilder();
        foreach (var c in decomposed)
        {
            if (char.IsAsciiLetter(c))
            {
                letters.Append(c);
            }
            else if (char.IsLetter(c))
            {
                // Písmeno, které neumíme převést (např. þ) → vypustí se, ale chce to kontrolu.
                lostLetter = true;
            }
            // Ostatní (apostrofy, tečky, číslice, zbylé háčky) se vypustí bez kontroly.
        }

        if (letters.Length == 0)
            return string.Empty;

        // 5) První písmeno velké, zbytek malá: NOVAK → Novak.
        var text = letters.ToString();
        return char.ToUpperInvariant(text[0]) + text[1..].ToLowerInvariant();
    }
}
