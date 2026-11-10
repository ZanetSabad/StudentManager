namespace StudentManager.Web.Options;

/// <summary>
/// Nastavení školy ze sekce "School" v appsettings.json.
/// </summary>
// PROČ samostatná třída: hodnoty z konfigurace jsou pak v kódu jako běžné vlastnosti
//   (options.EmailDomain) místo textových klíčů ("School:EmailDomain"), kde se snadno udělá překlep.
// BUDOUCNOST: tyto hodnoty se budou upravovat na stránce Nastavení.
public class SchoolOptions
{
    /// <summary>Název sekce v appsettings.json.</summary>
    public const string SectionName = "School";

    /// <summary>Doména pro UPN a e-mail, např. sou-sos.cz.</summary>
    public string EmailDomain { get; set; } = string.Empty;
}
