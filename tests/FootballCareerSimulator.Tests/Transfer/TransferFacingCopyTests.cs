using FootballCareerSimulator.Application.Transfer.Queries;

namespace FootballCareerSimulator.Tests.Transfer;

public sealed class TransferFacingCopyTests
{
    [Theory]
    [InlineData(1, 1, "Dosya açıldı")]
    [InlineData(5, 1, "Dosya açıldı")]
    [InlineData(6, 2, "Kulüple görüşme")]
    [InlineData(9, 3, "Oyuncu şartları")]
    [InlineData(11, 4, "Yönetim onayı")]
    [InlineData(13, 5, "İmza")]
    public void StageLabel_MapsStatusToPlayerFacingCopy(int statusCode, int stage, string label)
    {
        Assert.Equal(stage, TransferFacingCopy.StageNumber(statusCode));
        Assert.Equal(label, TransferFacingCopy.StageLabel(statusCode));
    }

    [Fact]
    public void DealLine_AvoidsTechnicalStatusNamesAndStatesNextAction()
    {
        var line = TransferFacingCopy.DealLine(
            "Ada Yılmaz",
            statusCode: 8,
            tensionPercent: 40,
            hasPendingOffer: true,
            hasPendingProposal: false);

        Assert.Contains("Ada Yılmaz", line, StringComparison.Ordinal);
        Assert.Contains("Aşama 2/5", line, StringComparison.Ordinal);
        Assert.Contains("Kulüple görüşme", line, StringComparison.Ordinal);
        Assert.Contains("Kulüp yanıtı bekliyor", line, StringComparison.Ordinal);
        Assert.DoesNotContain("ClubNegotiation", line, StringComparison.Ordinal);
        Assert.DoesNotContain("SportingApproval", line, StringComparison.Ordinal);
    }

    [Fact]
    public void OfferAndContractLabels_PreferPendingCopy()
    {
        Assert.Equal(
            "Kulüp yanıtı bekleniyor",
            TransferFacingCopy.OfferStatusLabel("Bekliyor", pending: true));
        Assert.Equal(
            "Oyuncu/menajer yanıtı bekleniyor",
            TransferFacingCopy.ContractStatusLabel("Bekliyor", pending: true));
        Assert.Equal(
            "Kulüp kabul etti",
            TransferFacingCopy.OfferStatusLabel("Kabul", pending: false));
    }

    [Fact]
    public void WindowLabel_UsesOpenClosedWithoutRawStatusName()
    {
        var label = TransferFacingCopy.WindowLabel(true, "01.07.2026", "31.08.2026");
        Assert.Contains("Açık", label, StringComparison.Ordinal);
        Assert.Contains("01.07.2026", label, StringComparison.Ordinal);
        Assert.DoesNotContain("Open", label, StringComparison.Ordinal);
    }
}
