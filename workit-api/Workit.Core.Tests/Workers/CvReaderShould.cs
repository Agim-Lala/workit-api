using Shouldly;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using Workit.Core.Workers;

namespace Workit.Core.Tests.Workers;

public sealed class CvReaderShould
{
    [Fact]
    public void ReturnTheTextOfAPdf()
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4)
            .AddText("Experienced Waiter at Hotel Tirana", 12, new UglyToad.PdfPig.Core.PdfPoint(50, 700), font);

        var text = CvReader.ExtractText(builder.Build());

        text.ShouldNotBeNull();
        text.ShouldContain("Waiter");
    }

    [Fact]
    public void ReturnNullForBytesThatAreNotAPdf()
    {
        CvReader.ExtractText("%PDF-1.4 fake cv content"u8.ToArray()).ShouldBeNull();
    }

    [Fact]
    public void DeriveRolesLanguagesAndYearsFromCvText()
    {
        var insights = CvReader.Analyze(
            "Kamariere me 5 vjet përvojë. Previously bartending for 2 years. Gjuhët: Shqip, Anglisht, Italian.");

        insights.Roles.ShouldBe(["waiter", "bartender"], ignoreOrder: true);
        insights.Languages.ShouldBe(["albanian", "english", "italian"], ignoreOrder: true);
        insights.YearsOfExperience.ShouldBe(5);
    }

    [Fact]
    public void MapJobRolesToTheSameCanonicalRoles()
    {
        CvReader.RolesIn("Server").ShouldBe(["waiter"]);
    }

    [Fact]
    public void KeepSpacesBetweenSeparatelyPlacedWords()
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var page = builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4);
        page.AddText("Experienced", 12, new UglyToad.PdfPig.Core.PdfPoint(50, 700), font);
        page.AddText("Waiter", 12, new UglyToad.PdfPig.Core.PdfPoint(150, 700), font);

        CvReader.Analyze(CvReader.ExtractText(builder.Build())).Roles.ShouldBe(["waiter"]);
    }

    [Fact]
    public void MatchAlbanianKeywordsTypedWithoutDiacritics()
    {
        var insights = CvReader.Analyze("Arketar dhe pastiqier. Gjuhet: anglisht, frengjisht.");

        insights.Roles.ShouldBe(["pastry", "cashier"], ignoreOrder: true);
        insights.Languages.ShouldBe(["english", "french"], ignoreOrder: true);
    }

    [Fact]
    public void NotMatchRolesInsideUnrelatedWords()
    {
        CvReader.Analyze("Eventually stayed at a hostel, baked a cookie, read a magazine.").Roles.ShouldBeEmpty();
    }

    [Fact]
    public void NotReadAgeAsYearsOfExperience()
    {
        CvReader.Analyze("I am 25 years old with 3 years of experience.").YearsOfExperience.ShouldBe(3);
    }
}
