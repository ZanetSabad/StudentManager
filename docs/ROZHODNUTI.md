# Student Manager – rozhodnutí a pravidla projektu

Webová aplikace pro IT administrátora školy: příprava, evidence a export studentských účtů pro pozdější založení v Active Directory.

## Prostředí školy

- On-premises AD: `dom22.local`
- E-mailová doména / UPN: `sou-sos.cz`
- Souborový server: `fs.sou-sos.cz`
- Synchronizační skupina do Entra ID: `toAzureAD`
- Aplikace musí jít nastavit i pro jinou školu → nic z toho nesmí být pevně v kódu,
  vše půjde do Nastavení.

## Technologie

- .NET 10 (LTS), ASP.NET Core Razor Pages, C#
- Databáze: SQLite + Entity Framework Core (později lze přejít na SQL Server)
- Vzhled: Bootstrap 5
- Vývoj: Windows 11 (ne WSL – kvůli budoucí práci s AD a PowerShellem), VS Code, Git

## Pravidla pro studenty

### Uživatelské jméno
- Tvar: `Prijmeni.Jmeno`, bez diakritiky → `Novak.Jan`
- Kolize: `Novak.Jan2`, `Novak.Jan3`… a student dostane stav **Ke kontrole**
- Dva studenti se stejným jménem NEJSOU automaticky jedna osoba
  (rozlišuje je StudentIdentifier)
- UPN a e-mail: `Novak.Jan@sou-sos.cz`
- Pozor: sAMAccountName v AD má limit 20 znaků

### Stavy studenta
| Hodnota | Česky | Číslo v DB |
|---|---|---|
| Draft | Koncept | 0 |
| NeedsReview | Ke kontrole | 1 |
| Ready | Připraven | 2 |
| Exported | Exportován | 3 |

Nové stavy přidávat vždy na konec s dalším číslem.

## Třídy

- Název = rok nástupu + označení: `2026A`, `2026M`, `2025DI`, `2025N`…
- Název se po celou dobu studia NEMĚNÍ.
- Ročník se neukládá, dopočítává se: `aktuální rok − rok nástupu + 1`
  (ve 2026/2027: 2026A = 1. ročník, 2025A = 2. ročník…)
- Délka studia se liší podle oboru (4 / 3 / 2 roky – nástavba) → `DurationYears`
- Studenti se mohou přesouvat mezi třídami (např. do nástavby).
- Ukončená třída se nemaže, jen se označí jako neaktivní.

## Organizační jednotky (OU)

- Šablona: `OU={Trida},OU=Studenti,DC=dom22,DC=local`
- Příklad: `OU=2026A,OU=Studenti,DC=dom22,DC=local`
- Šablona bude v Nastavení, výjimky přes `OuOverride` u třídy.

## Bezpečnost

- Při vývoji pouze fiktivní studenti.
- Databáze se NEUKLÁDÁ do Gitu (osobní údaje).
- Žádná hesla v kódu ani v appsettings.json.
- Import nikdy nepřepíše existující studenty.
- Každá změna dat se zapíše do auditu.

## Úkoly před nasazením na server

- [ ] HTTPS (teď běží jen http – viz upozornění HttpsRedirection při spuštění)
- [ ] Přihlášení: Windows Authentication + AD skupina administrátorů
- [ ] Zálohování databáze
- [ ] Kontrola, že v databázi nejsou testovací data

## Budoucí fáze (zatím neimplementovat)

- Fáze 2 – Active Directory: účty, OU, skupiny, domovské složky, NTFS, PowerShell
- Fáze 3 – Entra ID a Microsoft 365: synchronizace, skupiny, licence
- Fáze 4 – Životní cyklus: přesuny, postup do ročníků, přerušení, ukončení, deaktivace

## Postup vývoje

- Malé kroky, každý ověřit (`dotnet build`, spuštění) a uložit commitem.
- Kód a názvy v kódu anglicky, komentáře a rozhraní česky.