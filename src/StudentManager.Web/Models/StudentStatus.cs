namespace StudentManager.Web.Models;

/// <summary>
/// Stav přípravy studentského účtu.
/// </summary>
// PROČ pevná čísla (= 0, = 1…): do databáze se ukládá číslo, ne název.
//   Kdyby se čísla posunula, stará data by najednou znamenala jiný stav.
//   Nové stavy proto přidávat VŽDY NA KONEC s dalším číslem.

public enum StudentStatus
{
    //Koncept – student je rozpracovaný.
    Draft = 0,

    //Ke kontrole – např. kolize uživatelského jména nebo chybějící údaje.
    NeedsReview = 1,

    //Připraven – lze exportovat.
    Ready = 2,

    //Exportován – už byl odeslán do CSV pro PowerShell.
    Exported = 3

    // BUDOUCNOST (fáze 4): Active = 4, Suspended = 5 (přerušení), Graduated = 6 (ukončení)…
}