using StudentManager.Web.Models;
using StudentManager.Web.Services;

namespace StudentManager.Tests.Services;

/// <summary>
/// Testy schválení studenta (docs/ROZHODNUTI.md, sekce Schválení studenta).
/// </summary>
public class StudentApprovalServiceTests
{
    private readonly StudentApprovalService _service = new(new UsernameValidator());

    // Pomocná metoda: student s nejnutnějšími údaji.
    private static Student CreateStudent(string username, StudentStatus status, string? reviewReason = null) => new()
    {
        FirstName = "Jan",
        LastName = "Novák",
        Username = username,
        Status = status,
        ReviewReason = reviewReason,
        AdminNote = "Poznámka admina"
    };

    /// <summary>Koncept → Připraven.</summary>
    [Fact]
    public void Approve_Draft_BecomesReady()
    {
        var student = CreateStudent("Novak.Jan", StudentStatus.Draft);

        var result = _service.Approve(student);

        Assert.True(result.Approved);
        Assert.Null(result.Error);
        Assert.Equal(StudentStatus.Ready, student.Status);
    }

    /// <summary>Ke kontrole s kolizí → Připraven, důvod se smaže, poznámka admina zůstane.</summary>
    [Fact]
    public void Approve_NeedsReviewWithCollision_BecomesReadyAndClearsReason()
    {
        var student = CreateStudent("Novak.Jan2", StudentStatus.NeedsReview,
            "Kolize: Novak.Jan je obsazené, přiděleno Novak.Jan2.");

        var result = _service.Approve(student);

        Assert.True(result.Approved);
        Assert.Equal(StudentStatus.Ready, student.Status);
        Assert.Null(student.ReviewReason);
        Assert.Equal("Poznámka admina", student.AdminNote);
    }

    /// <summary>Přesně 20 znaků → ještě lze schválit.</summary>
    [Fact]
    public void Approve_Exactly20_IsApproved()
    {
        var student = CreateStudent("Kowalczykowa.Jolanta", StudentStatus.Draft); // 20 znaků

        Assert.True(_service.Approve(student).Approved);
    }

    /// <summary>Kolizní číslo se počítá do délky: přesně 20 znaků i s číslem → lze schválit.</summary>
    [Fact]
    public void Approve_CollisionNumberExactly20_IsApproved()
    {
        var student = CreateStudent("Kowalczykowa.Jolana2", StudentStatus.NeedsReview, "Kolize…"); // 20 znaků

        Assert.True(_service.Approve(student).Approved);
    }

    /// <summary>Kolizní číslo se počítá do délky: 21 znaků i s číslem → nelze schválit.</summary>
    [Fact]
    public void Approve_CollisionNumberMakesItLongerThan20_IsRejected()
    {
        var student = CreateStudent("Kowalczykowa.Jolanta2", StudentStatus.NeedsReview, "Kolize…"); // 21 znaků

        var result = _service.Approve(student);

        Assert.False(result.Approved);
        Assert.Contains("delší než 20", result.Error);
    }

    /// <summary>Nad 20 znaků → odmítnuto, stav ani důvod se nezmění.</summary>
    [Fact]
    public void Approve_LongerThan20_IsRejectedAndStudentUnchanged()
    {
        var student = CreateStudent("Krejcirikova.Kristyna", StudentStatus.NeedsReview,
            "Delší než 20 znaků (limit sAMAccountName)."); // 21 znaků

        var result = _service.Approve(student);

        Assert.False(result.Approved);
        Assert.Contains("delší než 20", result.Error);
        Assert.Equal(StudentStatus.NeedsReview, student.Status);
        Assert.Equal("Delší než 20 znaků (limit sAMAccountName).", student.ReviewReason);
    }

    /// <summary>Dlouhé A zároveň špatné jméno → hláška o délce (délka se kontroluje dřív než tvar).</summary>
    [Fact]
    public void Approve_LongerThan20AndBadFormat_ReportsLength()
    {
        var student = CreateStudent("Krejčiříková.Kristýna", StudentStatus.Draft); // 21 znaků, diakritika

        var result = _service.Approve(student);

        Assert.False(result.Approved);
        Assert.Contains("delší než 20", result.Error);
    }

    /// <summary>Koncept se špatným tvarem (do 20 znaků) → odmítnuto, stav ani důvod se nezmění.</summary>
    [Theory]
    [InlineData("Novák.Jan")]
    [InlineData(".Novak.Jan")]
    [InlineData("Novak..Jan")]
    public void Approve_DraftWithBadFormat_IsRejectedAndStudentUnchanged(string username)
    {
        var student = CreateStudent(username, StudentStatus.Draft);

        var result = _service.Approve(student);

        Assert.False(result.Approved);
        Assert.NotNull(result.Error);
        Assert.Equal(StudentStatus.Draft, student.Status);
        Assert.Null(student.ReviewReason);
    }

    /// <summary>Už schválený (dvojklik, dva admini) → přátelská hláška, nic se nezmění.</summary>
    [Fact]
    public void Approve_AlreadyReady_ReturnsFriendlyMessage()
    {
        var student = CreateStudent("Novak.Jan", StudentStatus.Ready);

        var result = _service.Approve(student);

        Assert.False(result.Approved);
        Assert.Equal("Student Novák Jan už je schválen.", result.Error);
        Assert.Equal(StudentStatus.Ready, student.Status);
    }

    /// <summary>Exportovaný → vlastní hláška, nic se nezmění.</summary>
    [Fact]
    public void Approve_Exported_ReturnsExportedMessage()
    {
        var student = CreateStudent("Novak.Jan", StudentStatus.Exported);

        var result = _service.Approve(student);

        Assert.False(result.Approved);
        Assert.Contains("exportován", result.Error);
        Assert.Equal(StudentStatus.Exported, student.Status);
    }
}
