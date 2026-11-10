namespace StudentManager.Web.Models;

/// <summary>
/// Student se připravuje k založení účtu v AD.
/// Jeden objekt = jeden řádek v tabulce Students v databázi.
/// </summary>
public class Student
{
    //ID studenta. Přiděluje ho databáze automaticky (1, 2, 3…).
    public int Id { get; set; }

    // PROČ string bez otazníku: jméno a příjmení jsou povinné.
    // PROČ "= string.Empty": výchozí hodnota je prázdný text místo null (nic),
    //   jinak C# varuje, že hodnota může chybět.

    //Křestní jméno, např. Jan (s diakritikou)
    public string FirstName { get; set; } = string.Empty;

    //Příjmení, např. Novák (s diakritikou).
    public string LastName { get; set; } = string.Empty;

    //Identifikátor studenta ze školní evidence. Nepovinný (string?), ale doporučený – rozliší dva studenty se stejným jménem. Našla jsem v bakalářích - byly tam 2 iodentifikační čísla, už si nepamatuju jak se lišily, ale jedno jsem říkala, že by šlo 
    // TODO: v databázi nastavíme, že musí být unikátní (pokud je vyplněný).
    public string? StudentIdentifier { get; set; }

    // Uživatelské jméno ve tvaru Prijmeni.Jmeno bez diakritiky, např. Novak.Jan.
    // Při kolizi: Novak.Jan2, Novak.Jan3… a stav NeedsReview.
    // TODO: bude ho generovat služba UsernameGenerator, admin ho bude moci ručně upravit.
    // BUDOUCNOST: sAMAccountName v AD má limit 20 znaků – přidáme samostatnou vlastnost, SamAccountName a aplikace bude na dlouhá jména upozorňovat. To samé se zamyslet jak to udělat při vícečetném jméně, např. Jana Anna NOváková, Li Thu Thi Jong, atd...
    public string Username { get; set; } = string.Empty;

    //Přihlašovací jméno do Microsoft 365: Username + "@" + doména, např. Novak.Jan@sou-sos.cz.
    // PROČ ne pevně "sou-sos.cz": doména by se mohla brát z Nastavení, aby aplikace šla použít i v jiné škole.
    public string Upn { get; set; } = string.Empty;

    //E-mailová adresa. Obvykle stejná jako UPN.
    public string Email { get; set; } = string.Empty;

    //Stav přípravy účtu. Nový student začíná jako Draft (Koncept).
    // PROČ enum a ne text: povolí jen pevně dané hodnoty, žádné překlepy ("pripraven", "ok"…).
    public StudentStatus Status { get; set; } = StudentStatus.Draft;

    //Volná poznámka administrátora. Nepovinná.
    public string? AdminNote { get; set; }

    //Proč je student ve stavu Ke kontrole (např. kolize jména, délka nad 20 znaků). Nepovinné.
    // PROČ zvlášť od AdminNote: důvod vyplňuje aplikace, poznámku píše admin – nemají se přepisovat.
    public string? ReviewReason { get; set; }

    // vazba na třídu (SchoolClassId) a školní rok (SchoolYearId). Díky tomu vímne, který student bude patřit do jaké třídy a pro který školnmí rok je připraven. Lepší kvůli překlepům atd....vyzby už jsem dělala tak si to zopakujeme (foreign key, ...)

    // BUDOUCNOST: přesun do jiné třídy = změna této hodnoty.
    public int SchoolClassId { get; set; }

    //Třída jako celý objekt – pro pohodlný přístup v kódu (student.SchoolClass.Name).
    // Neukládá se do databáze....díky tom,u lze v kódu napssat "student.SchoolClass.Name" a dostaneme "2026A" = Nnemusí se tedy hledat ručně podle čísla => Entity Framework (knihovna pro databázi), tu přidáme později
    // díky názvům SchoolClass a SchoolClasId pozná, že patří k sobě
    public SchoolClass? SchoolClass { get; set; }

    //Id školního roku, pro který je student připraven.
    public int SchoolYearId { get; set; }

    //Školní rok jako celý objekt.
    public SchoolYear? SchoolYear { get; set; }




    // TODO: DisplayName ("Novák Jan"), SamAccountName.
    // TODO: CreatedAt, UpdatedAt – kdy byl záznam vytvořen/změněn (pro audit).
    // BUDOUCNOST (fáze 2): AdObjectGuid – propojení se skutečným účtem v AD.
    // BUDOUCNOST (fáze 4): přesuny mezi třídami = změna SchoolClassId, historie v auditu.
}