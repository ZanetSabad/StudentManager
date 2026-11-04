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

    // --- Kolize ---

    /// <summary>Jméno je volné → použije se základ bez čísla a bez kontroly.</summary>
    // PROČ params: atribut neumí přijmout samotné pole string[] jako jediný údaj.
    //   S params se jména píšou rovnou za sebou a xUnit z nich pole složí.
    [Theory]
    [InlineData()]                    // prázdný seznam
    [InlineData("Svobodova.Petra")]   // jiné jméno
    [InlineData("Novak.Jan2")]        // obsazené jen číslo, základ volný
    public void Generate_NoCollision_ReturnsBaseUsername(params string[] existing)
    {
        var result = _generator.Generate("Jan", "Novák", existing);

        Assert.Equal("Novak.Jan", result.Username);
        Assert.False(result.NeedsReview);
    }

    /// <summary>Jméno je obsazené → první volné číslo + Ke kontrole.</summary>
    [Theory]
    [InlineData("Novak.Jan2", new[] { "Novak.Jan" })]
    [InlineData("Novak.Jan3", new[] { "Novak.Jan", "Novak.Jan2" })]
    [InlineData("Novak.Jan2", new[] { "novak.jan" })]                // velikost písmen nerozhoduje
    [InlineData("Novak.Jan3", new[] { "NOVAK.JAN", "novak.jan2" })]
    [InlineData("Novak.Jan2", new[] { "Novak.Jan", "Novak.Jan3" })]  // první volné, ne nejvyšší + 1
    public void Generate_Collision_ReturnsFirstFreeNumberAndNeedsReview(string expected, string[] existing)
    {
        var result = _generator.Generate("Jan", "Novák", existing);

        Assert.Equal(expected, result.Username);
        Assert.True(result.NeedsReview);
        Assert.Contains("Kolize", result.ReviewReason);
    }

    /// <summary>Číslo z kolize prodlouží jméno přes limit → dva důvody ke kontrole.</summary>
    [Fact]
    public void Generate_CollisionMakesItLongerThan20_NeedsReviewForBoth()
    {
        // Kowalczykowa.Jolanta má přesně 20 znaků, s číslem 21.
        var result = _generator.Generate("Jolanta", "Kowalczykowa", new[] { "Kowalczykowa.Jolanta" });

        Assert.Equal("Kowalczykowa.Jolanta2", result.Username);
        Assert.True(result.NeedsReview);
        Assert.Contains("Kolize", result.ReviewReason);
        Assert.Contains("20", result.ReviewReason);
    }

    /// <summary>Prázdný username se kolizí neřeší (žádné "2").</summary>
    [Fact]
    public void Generate_EmptyUsername_SkipsCollision()
    {
        var result = _generator.Generate("Jan", "'''", new[] { "" });

        Assert.Equal(string.Empty, result.Username);
        Assert.True(result.NeedsReview);
        Assert.DoesNotContain("Kolize", result.ReviewReason);
    }
}
