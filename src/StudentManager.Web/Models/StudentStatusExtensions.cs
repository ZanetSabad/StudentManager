namespace StudentManager.Web.Models;

/// <summary>
/// Pomocné funkce pro zobrazení stavu studenta v rozhraní.
/// </summary>
public static class StudentStatusExtensions
{
    /// <summary>Český název stavu.</summary>
    public static string ToCzech(this StudentStatus status) => status switch
    {
        StudentStatus.Draft => "Koncept",
        StudentStatus.NeedsReview => "Ke kontrole",
        StudentStatus.Ready => "Připraven",
        StudentStatus.Exported => "Exportován",
        _ => status.ToString()
    };

    /// <summary>Barva odznaku (třída Bootstrapu).</summary>
    public static string ToBadgeClass(this StudentStatus status) => status switch
    {
        StudentStatus.Draft => "text-bg-secondary",     // šedá
        StudentStatus.NeedsReview => "text-bg-warning", // žlutá
        StudentStatus.Ready => "text-bg-success",       // zelená
        StudentStatus.Exported => "text-bg-primary",    // modrá
        _ => "text-bg-light"
    };

    // BUDOUCNOST (fáze 4): při přidání nového stavu doplnit i sem.
}