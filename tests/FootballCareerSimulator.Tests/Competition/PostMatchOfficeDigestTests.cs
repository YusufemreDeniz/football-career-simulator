using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.Interaction.Queries;

namespace FootballCareerSimulator.Tests.Competition;

public sealed class PostMatchOfficeDigestTests
{
    [Fact]
    public void Quiet_WhenNoManagedNarrative()
    {
        var digest = PostMatchOfficeDigest.Compose(
            narrative: null,
            DecisionDeskDigest.Clear(),
            hasManagedMatch: false);

        Assert.Equal(PostMatchOfficeDigest.Brand, digest.BrandTitle);
        Assert.Contains("Ofis sakin", digest.Headline, StringComparison.Ordinal);
    }

    [Fact]
    public void PressAndHardDesk_LeadsCrisisHeadline()
    {
        var narrative = MatchNightNarrative.Compose(
            "A 0-2 B",
            0,
            2,
            managedIsHome: true,
            hasManagedMatch: true,
            tacticNote: null,
            dayNumber: 20,
            beatLines: Array.Empty<string>(),
            afterWhistleLines: ["Yönetim güveni -5 → 40 (İncelemede)", "Basın sorusu açıldı."],
            otherScorelines: Array.Empty<string>(),
            kickoffLines: ["Ev vs B · bugün", "Maça söz riskiyle girdin."],
            enteredWithPromiseRisk: true);

        var desk = DecisionDeskDigest.Compose(
            new PendingDecisionsReadModel(
                1,
                [
                    new DecisionRequestLineReadModel(
                        7,
                        "Kritik basın sorusu",
                        42,
                        1,
                        "Open",
                        IsHardBlocker: true,
                        20,
                        22,
                        null),
                ]),
            currentDayNumber: 20);

        var digest = PostMatchOfficeDigest.Compose(narrative, desk, hasManagedMatch: true);

        Assert.Equal("Ofiste kriz — cevap vermeden ilerleyemezsin.", digest.Headline);
        Assert.Contains(digest.BeatLines, b => b.Contains("Basın sorusu", StringComparison.Ordinal));
        Assert.Contains(digest.BeatLines, b => b.Contains("Masada zorunlu", StringComparison.Ordinal));
        Assert.Contains(digest.BeatLines, b => b.Contains("söz gerilimi", StringComparison.OrdinalIgnoreCase));

        var text = digest.ToStatusMessage();
        Assert.Contains("Ofiste", text, StringComparison.Ordinal);
        Assert.Contains("· ", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ComfortableWin_RelaxesOffice()
    {
        var narrative = MatchNightNarrative.Compose(
            "A 3-0 B",
            3,
            0,
            managedIsHome: true,
            hasManagedMatch: true,
            tacticNote: "taktik +1",
            dayNumber: 5,
            beatLines: Array.Empty<string>(),
            afterWhistleLines: ["Yönetim güveni +3 → 70 (Güvenli)"],
            otherScorelines: Array.Empty<string>());

        var digest = PostMatchOfficeDigest.Compose(
            narrative,
            DecisionDeskDigest.Clear(),
            hasManagedMatch: true);

        Assert.Equal("Ofis rahatladı — gece senindi.", digest.Headline);
        Assert.Contains(digest.BeatLines, b => b.Contains("Masada yeni zorunlu dosya yok", StringComparison.Ordinal));
    }
}
