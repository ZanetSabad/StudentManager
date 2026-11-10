using StudentManager.Web.Services;

namespace StudentManager.Tests.Services;

/// <summary>
/// Testy kontroly tvaru uživatelského jména (docs/ROZHODNUTI.md, sekce Uživatelské jméno).
/// </summary>
public class UsernameValidatorTests
{
    private readonly UsernameValidator _validator = new();

    /// <summary>Platná jména – kontrola vrátí null (žádná chyba).</summary>
    [Theory]
    [InlineData("Novak.Jan")]
    [InlineData("Novak.Jan2")]
    [InlineData("Novak-Svoboda.Jan")]           // pomlčka je povolená
    [InlineData("novak.jan")]                   // malá písmena jsou povolená
    [InlineData("Novak")]                       // tečka není povinná
    [InlineData("Krejcirikova.Kristyna")]       // délka nad 20 není chyba (řeší se stavem)
    public void Validate_ValidUsername_ReturnsNull(string username)
    {
        Assert.Null(_validator.Validate(username));
    }

    /// <summary>Prázdné jméno je chyba.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Empty_ReturnsError(string? username)
    {
        Assert.NotNull(_validator.Validate(username));
    }

    /// <summary>Nepovolené znaky: diakritika, mezera, podtržítko, zavináč, apostrof…</summary>
    [Theory]
    [InlineData("Novák.Jan")]
    [InlineData("Novak Jan")]
    [InlineData("Novak_Jan")]
    [InlineData("Novak.Jan@sou-sos.cz")]
    [InlineData("O'Brien.Patrick")]
    [InlineData("Nowak.Łukasz")]
    public void Validate_ForbiddenCharacter_ReturnsError(string username)
    {
        var error = _validator.Validate(username);

        Assert.NotNull(error);
        Assert.Contains("Nepovolený znak", error);
    }

    /// <summary>Tečka na začátku nebo na konci (pravidla UPN).</summary>
    [Theory]
    [InlineData(".Jan")]
    [InlineData("Novak.")]
    [InlineData(".")]
    public void Validate_DotAtStartOrEnd_ReturnsError(string username)
    {
        var error = _validator.Validate(username);

        Assert.NotNull(error);
        Assert.Contains("začínat ani končit", error);
    }

    /// <summary>Dvě tečky za sebou (pravidla UPN).</summary>
    [Fact]
    public void Validate_TwoDotsInRow_ReturnsError()
    {
        var error = _validator.Validate("Novak..Jan");

        Assert.NotNull(error);
        Assert.Contains("dvě tečky", error);
    }
}
