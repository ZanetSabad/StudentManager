using StudentManager.Web.Services;

namespace StudentManager.Tests.Services;

/// <summary>
/// Testy pravidel pro uživatelské jméno (docs/ROZHODNUTI.md, sekce Uživatelské jméno).
/// </summary>
public class UsernameGeneratorTests
{
    private readonly UsernameGenerator _generator = new();

    // PROČ [Theory] + [InlineData]: jeden test, který se spustí pro každý řádek dat zvlášť.
    //   Ve výsledcích se každý řádek ukáže jako samostatný test.

    /// <summary>Jména, která projdou bez kontroly.</summary>
    [Theory]
    // Příklady z ROZHODNUTI.md
    [InlineData("Jan", "Novák", "Novak.Jan")]
    [InlineData("Jana", "Nováková Svobodová", "Novakova.Jana")]
    [InlineData("Jan", "Novák-Svoboda", "Novak.Jan")]
    [InlineData("Jan Pavel", "Novák", "Novak.Jan")]
    [InlineData("Jan", "NOVÁK", "Novak.Jan")]
    [InlineData("Patrick", "O'Brien", "Obrien.Patrick")]
    // Další zadaná jména
    [InlineData("Patrick", "O’Brien", "Obrien.Patrick")] // typografický apostrof
    [InlineData("Řehoř", "Ščerbová", "Scerbova.Rehor")]
    [InlineData("Łukasz", "Nowak", "Nowak.Lukasz")]
    [InlineData("Văn Đức", "Nguyễn", "Nguyen.Van")]      // ễ = e + dvě znaménka
    [InlineData("Đức", "Nguyễn Văn", "Nguyen.Duc")]      // Đ z tabulky náhrad
    [InlineData("Anders", "Ærø", "Aero.Anders")]         // Æ → AE, ø → o
    // Tabulka náhrad
    [InlineData("Hans", "Groß", "Gross.Hans")]
    [InlineData("Søren", "Larsen", "Larsen.Soren")]
    [InlineData("Œdipe", "Martin", "Martin.Oedipe")]
    // Hranice: přesně 20 znaků je ještě v pořádku
    [InlineData("Jolanta", "Kowalczykowa", "Kowalczykowa.Jolanta")]
    public void Generate_ValidName_ReturnsUsernameWithoutReview(string firstName, string lastName, string expected)
    {
        var result = _generator.Generate(firstName, lastName);

        Assert.Equal(expected, result.Username);
        Assert.False(result.NeedsReview);
        Assert.Null(result.ReviewReason);
    }

    /// <summary>Delší než 20 znaků → jméno se ponechá, ale chce kontrolu.</summary>
    [Fact]
    public void Generate_LongerThan20_KeepsUsernameAndNeedsReview()
    {
        var result = _generator.Generate("Kristina", "Kowalczykowa"); // 21 znaků

        Assert.Equal("Kowalczykowa.Kristina", result.Username);
        Assert.True(result.NeedsReview);
        Assert.Contains("20", result.ReviewReason);
    }

    /// <summary>Písmeno mimo tabulku náhrad (þ) se vypustí a výsledek chce kontrolu.</summary>
    [Fact]
    public void Generate_LetterNotInReplacementTable_DropsItAndNeedsReview()
    {
        var result = _generator.Generate("Þór", "Jónsson");

        Assert.Equal("Jonsson.Or", result.Username);
        Assert.True(result.NeedsReview);
        Assert.Contains("tabulce náhrad", result.ReviewReason);
    }

    /// <summary>Prázdné jméno nebo příjmení po úpravě → prázdný username + kontrola.</summary>
    [Theory]
    [InlineData("Jan", "'''")]
    [InlineData("'''", "Novák")]
    [InlineData("Jan", "")]
    [InlineData("", "Novák")]
    [InlineData("   ", "   ")]
    [InlineData(null, null)]
    public void Generate_EmptyPartAfterCleanup_ReturnsEmptyAndNeedsReview(string? firstName, string? lastName)
    {
        var result = _generator.Generate(firstName, lastName);

        Assert.Equal(string.Empty, result.Username);
        Assert.True(result.NeedsReview);
        Assert.Contains("prázdné", result.ReviewReason);
    }
}
