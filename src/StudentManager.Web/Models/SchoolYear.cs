namespace StudentManager.Web.Models;

/// <summary>
/// Školní rok, např. 2026/2027.
/// </summary>
public class SchoolYear
{
    public int Id { get; set; }

    //Název pro zobrazení, např. "2026/2027".
    public string Name { get; set; } = string.Empty;

    //Rok začátku školního roku, např. 2026. Z něj se počítá ročník tříd.
    public int StartYear { get; set; }

    //Je to aktuální školní rok? Měl by být označený právě jeden.
    public bool IsCurrent { get; set; }
}