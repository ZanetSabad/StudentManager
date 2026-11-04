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
- Vždy jen PRVNÍ část příjmení i jména:
  - Nováková Svobodová Jana → `Novakova.Jana`
  - Novák-Svoboda Jan → `Novak.Jan`
  - Jan Pavel Novák → `Novak.Jan`
- Velké první písmeno, zbytek malá (NOVÁK → `Novak`)
- Diakritika se odstraní (Řehoř → `Rehor`, Nguyễn → `Nguyen`)
- Tabulka náhrad zvláštních písmen (malá i velká):
  ł → l, ß → ss, ø → o, đ → d, æ → ae, œ → oe
- Ostatní nepovolené znaky se odstraní (O'Brien → `Obrien`)
- Pokud se vypustí PÍSMENO, které není v tabulce náhrad (např. þ v Þór → `Or`),
  → stav **Ke kontrole** (interpunkce, apostrofy apod. se vypouštějí bez kontroly)
- Delší než 20 znaků (limit sAMAccountName) → ponechat + stav **Ke kontrole**
- Prázdný výsledek po úpravě → stav **Ke kontrole**
- Je-li po úpravě prázdné jméno NEBO příjmení → celý username prázdný + stav **Ke kontrole**
  (ne `.Jan` ani `Jan`, to by vypadalo platně, ale neodpovídá pravidlu)
- Kolize: `Novak.Jan2`, `Novak.Jan3`… a student dostane stav **Ke kontrole**
  - přidělí se PRVNÍ VOLNÉ číslo (obsazeno `Novak.Jan` a `Novak.Jan3` → `Novak.Jan2`)
  - porovnání bez ohledu na velikost písmen (`novak.jan` = `Novak.Jan`), stejně jako v AD
  - limit 20 znaků se kontroluje VČETNĚ čísla (`Novak.Jan2` je o znak delší než `Novak.Jan`)
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

### Smazání studenta (budoucí krok)
- Exportovaného studenta NELZE smazat, jen deaktivovat –
  jeho username zůstane obsazené (nesmí se přidělit nikomu jinému).

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
- [ ] Rezervovaná jména z AD: import seznamu existujících účtů (Get-ADUser export),
      aby generátor nepřidělil jméno, které už v AD existuje.
      Musí být hotové před prvním ostrým exportem.

## Budoucí fáze (zatím neimplementovat)

- Fáze 2 – Active Directory: účty, OU, skupiny, domovské složky, NTFS, PowerShell
- Fáze 3 – Entra ID a Microsoft 365: synchronizace, skupiny, licence
- Fáze 4 – Životní cyklus: přesuny, postup do ročníků, přerušení, ukončení, deaktivace

## Postup vývoje

- Malé kroky, každý ověřit (`dotnet build`, spuštění) a uložit commitem.
- Kód a názvy v kódu anglicky, komentáře a rozhraní česky.