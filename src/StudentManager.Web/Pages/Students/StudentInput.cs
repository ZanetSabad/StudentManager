using System.ComponentModel.DataAnnotations;

namespace StudentManager.Web.Pages.Students;

/// <summary>
/// Údaje z formuláře Nový student / Upravit studenta.
/// </summary>
// PROČ ne přímo třída Student: do Student by šlo podvrženým formulářem poslat i Status,
//   Upn nebo ReviewReason. Tady jsou jen pole, která smí vyplnit admin. Zbytek určí server.
public class StudentInput
{
    [Display(Name = "Jméno")]
    [Required(ErrorMessage = "Vyplňte jméno.")]
    [StringLength(100, ErrorMessage = "Jméno může mít nejvýše {1} znaků.")]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Příjmení")]
    [Required(ErrorMessage = "Vyplňte příjmení.")]
    [StringLength(100, ErrorMessage = "Příjmení může mít nejvýše {1} znaků.")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Identifikátor")]
    [StringLength(50, ErrorMessage = "Identifikátor může mít nejvýše {1} znaků.")]
    public string? StudentIdentifier { get; set; }

    // PROČ int? a [Required]: když admin nic nevybere, přijde prázdná hodnota.
    //   S int? se zobrazí naše česká zpráva místo anglické chyby převodu.
    [Display(Name = "Třída")]
    [Required(ErrorMessage = "Vyberte třídu.")]
    public int? SchoolClassId { get; set; }

    [Display(Name = "Školní rok")]
    [Required(ErrorMessage = "Vyberte školní rok.")]
    public int? SchoolYearId { get; set; }

    // PROČ limit 64, a ne 20: delší než 20 znaků jde uložit, jen se stavem Ke kontrole.
    [Display(Name = "Username")]
    [Required(ErrorMessage = "Vyplňte username.")]
    [StringLength(64, ErrorMessage = "Username může mít nejvýše {1} znaků.")]
    public string Username { get; set; } = string.Empty;

    [Display(Name = "Poznámka administrátora")]
    [StringLength(1000, ErrorMessage = "Poznámka může mít nejvýše {1} znaků.")]
    public string? AdminNote { get; set; }
}
