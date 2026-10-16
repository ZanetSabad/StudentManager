namespace StudentManager.Web.Models;

/// <summary>
/// Třída, např. 2026A. Název = rok nástupu + označení oboru/třídy.
/// Název se po celou dobu studia NEMĚNÍ.
/// </summary>
// PROČ není vázaná na školní rok: třída 2026A existuje po celou dobu studia.
// PROČ se ročník neukládá: dopočítá se jako  aktuální StartYear − EntryYear + 1.
//   Ve 2026/2027: 2026A = 1. ročník, 2025A = 2. ročník, 2024A = 3. ročník.
//   Nic se tedy nemusí každý rok ručně přepisovat.
public class SchoolClass
{
    public int Id { get; set; }

    //Celý název třídy, např. "2026A", "2025DI".
    // PROČ ukládáme, i když jde složit z EntryYear + Suffix: snazší vyhledávání a filtrování.
    public string Name { get; set; } = string.Empty;

    //Rok nástupu, např. 2026.
    public int EntryYear { get; set; }

    //Označení za rokem: A, M, N, DI… (může mít víc znaků).
    public string Suffix { get; set; } = string.Empty;

    //Délka studia v letech: 4 maturitní, 3 učební obor, 2 nástavba.
    // BUDOUCNOST: rok ukončení = EntryYear + DurationYears → deaktivace účtů.
    public int DurationYears { get; set; } = 4;

    
    //Ruční OU jen pro výjimky. Normálně se OU sestaví ze šablony
    //v Nastavení: OU={Trida},OU=Studenti,DC=dom22,DC=local
    
    public string? OuOverride { get; set; }

    //Je třída aktivní? Po ukončení studia se jen deaktivuje, nemaže se.
    // PROČ nemazat: zůstane historie – kdo kdy ve třídě byl. Asi je to zbytčné, ale uvidíme jak to bude závislé na licencích
    public bool IsActive { get; set; } = true;

    
    //Seznam studentů této třídy (opačná strana vazby). Něco jako pole. <Student> = v seznamu smí být jen objekty typu Student. Číslo ani text se do něj nevloží. =new() vytvoří práízdný seznam, aby na začátku nebyl null.
    public List<Student> Students { get; set; } = new();
}