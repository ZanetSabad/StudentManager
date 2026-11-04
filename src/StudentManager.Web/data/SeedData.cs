using StudentManager.Web.Models;

namespace StudentManager.Web.Data;

/// <summary>
/// Fiktivní testovací data. Spouští se JEN ve vývojovém prostředí (Development).
/// </summary>
// PROČ ne v migraci: data v migraci by se dostala i na produkční server.
public static class SeedData
{
    public static void Initialize(AppDbContext context)
    {
        // Pokud už v databázi nějaký školní rok je, data už existují – nic neděláme.
        if (context.SchoolYears.Any())
        {
            return;
        }

        var year = new SchoolYear { Name = "2026/2027", StartYear = 2026, IsCurrent = true };

        var class2026A = new SchoolClass { Name = "2026A", EntryYear = 2026, Suffix = "A", DurationYears = 4 };
        var class2026DI = new SchoolClass { Name = "2026DI", EntryYear = 2026, Suffix = "DI", DurationYears = 3 };
        var class2025N = new SchoolClass { Name = "2025N", EntryYear = 2025, Suffix = "N", DurationYears = 2 };

        context.SchoolYears.Add(year);
        context.SchoolClasses.AddRange(class2026A, class2026DI, class2025N);

        context.Students.AddRange(
            new Student
            {
                FirstName = "Jan", LastName = "Novák", StudentIdentifier = "10001",
                Username = "Novak.Jan", Upn = "Novak.Jan@sou-sos.cz", Email = "Novak.Jan@sou-sos.cz",
                Status = StudentStatus.Ready, SchoolClass = class2026A, SchoolYear = year
            },
            new Student
            {
                FirstName = "Petra", LastName = "Svobodová", StudentIdentifier = "10002",
                Username = "Svobodova.Petra", Upn = "Svobodova.Petra@sou-sos.cz", Email = "Svobodova.Petra@sou-sos.cz",
                Status = StudentStatus.Ready, SchoolClass = class2026DI, SchoolYear = year
            },
            // Testovací případ: shodné jméno – jiná osoba (jiné ID).
            new Student
            {
                FirstName = "Jan", LastName = "Novák", StudentIdentifier = "10003",
                Username = "Novak.Jan2", Upn = "Novak.Jan2@sou-sos.cz", Email = "Novak.Jan2@sou-sos.cz",
                Status = StudentStatus.NeedsReview, SchoolClass = class2026A, SchoolYear = year,
                AdminNote = "Shodné jméno se studentem 10001 – ověřit."
            },
            // Testovací případ: student bez identifikátoru.
            new Student
            {
                FirstName = "Tomáš", LastName = "Dvořák",
                Username = "Dvorak.Tomas", Upn = "Dvorak.Tomas@sou-sos.cz", Email = "Dvorak.Tomas@sou-sos.cz",
                Status = StudentStatus.Draft, SchoolClass = class2025N, SchoolYear = year
            },
            // Testovací případ: dlouhé jméno (21 znaků – víc než limit sAMAccountName v AD).
            new Student
            {
                FirstName = "Kristýna", LastName = "Krejčiříková", StudentIdentifier = "10005",
                Username = "Krejcirikova.Kristyna", Upn = "Krejcirikova.Kristyna@sou-sos.cz", Email = "Krejcirikova.Kristyna@sou-sos.cz",
                Status = StudentStatus.Draft, SchoolClass = class2026DI, SchoolYear = year
            }
        );

        context.SaveChanges();
    }
}