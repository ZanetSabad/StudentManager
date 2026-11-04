# Student Manager – pokyny pro Claude Code

## O mně a o tom, jak spolu pracujeme
- Komunikuj se mnou česky.
- Jsem začátečnice v C# (znám PowerShell a správu AD). Chci kódu rozumět, nejen ho mít.
- Pracuj po malých krocích. Před změnou mi řekni, co a proč uděláš a které soubory upravíš.
- U nových C# konstrukcí stručně vysvětli, co dělají (klidně s analogií s PowerShellem).
- Bez mého souhlasu neměň architekturu, datový model ani strukturu projektu.
- Nedělej nic z budoucích fází (AD, Entra ID, životní cyklus), pokud o to výslovně nepožádám.

## Projekt
- Pravidla a rozhodnutí: viz `docs/ROZHODNUTI.md` – vždy je dodržuj.
- .NET 10, ASP.NET Core Razor Pages, EF Core + SQLite, Bootstrap 5.
- Vývoj na Windows 11, příkazy v PowerShellu.
- Kód a názvy v kódu anglicky, komentáře a uživatelské rozhraní česky.
- Komentáře používají značky `PROČ:`, `DALŠÍ KROK:`, `BUDOUCNOST:`.

## Příkazy (spouštět z kořene projektu)
- Sestavení: `dotnet build`
- Spuštění: `dotnet run --project src/StudentManager.Web`
- Nová migrace: `dotnet ef migrations add NazevMigrace --project src/StudentManager.Web`
- Aplikace migrací: `dotnet ef database update --project src/StudentManager.Web`

## Bezpečnost
- Jen fiktivní testovací data. Databáze (`*.db`) nesmí do Gitu.
- Žádná hesla v kódu ani v appsettings.json.
- Databázi měň jen přes EF Core migrace, nikdy ručně.
- Nedělej git commit ani push – commity dělám sama.