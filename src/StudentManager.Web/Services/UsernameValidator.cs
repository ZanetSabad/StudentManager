namespace StudentManager.Web.Services;

/// <summary>
/// Kontroluje, jestli má uživatelské jméno povolený tvar (pravidla v docs/ROZHODNUTI.md).
/// </summary>
// PROČ samostatná služba: stejnou kontrolu použije formulář Nový student,
//   později i import CSV a úprava studenta. Pravidla jsou na jednom místě.
// PROČ se tu nekontroluje délka nad 20 znaků: to není chyba, jen důvod ke stavu Ke kontrole.
// PROČ se tu nekontroluje obsazenost: k tomu je potřeba databáze, tohle je čistá kontrola textu.
public class UsernameValidator
{
    /// <summary>
    /// Zkontroluje tvar uživatelského jména.
    /// </summary>
    /// <returns>null = jméno je v pořádku, jinak česká chybová zpráva.</returns>
    public string? Validate(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return "Uživatelské jméno je povinné.";

        // Povolené znaky: A–Z, a–z, číslice, tečka a pomlčka.
        foreach (var c in username)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '.' && c != '-')
                return $"Nepovolený znak „{c}“. Povolená jsou jen písmena A–Z bez diakritiky, číslice, tečka a pomlčka.";
        }

        // PROČ pravidla pro tečky: část UPN před @ nesmí začínat ani končit tečkou
        //   a nesmí obsahovat dvě tečky za sebou.
        if (username.StartsWith('.') || username.EndsWith('.'))
            return "Uživatelské jméno nesmí začínat ani končit tečkou.";

        if (username.Contains(".."))
            return "Uživatelské jméno nesmí obsahovat dvě tečky za sebou.";

        return null;
    }
}
