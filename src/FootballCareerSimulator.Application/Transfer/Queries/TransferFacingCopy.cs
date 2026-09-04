namespace FootballCareerSimulator.Application.Transfer.Queries;

/// <summary>
/// Domain status kodlarını oyuncu yüzü aşama/CTA metnine çevirir.
/// </summary>
public static class TransferFacingCopy
{
    public static int StageNumber(int statusCode) =>
        statusCode switch
        {
            1 or 5 => 1,   // UnderEvaluation / SportingApprovalPending
            6 or 8 => 2,   // SportingApproved / ClubNegotiation
            9 or 10 => 3,  // ClubAgreement / PlayerNegotiation
            11 or 12 => 4, // PlayerAgreement / FinancialApprovalPending
            _ => 5,
        };

    public static string StageLabel(int statusCode) =>
        StageNumber(statusCode) switch
        {
            1 => "Dosya açıldı",
            2 => "Kulüple görüşme",
            3 => "Oyuncu şartları",
            4 => "Yönetim onayı",
            _ => "İmza",
        };

    public static string NextActionHint(int statusCode, bool hasPendingOffer, bool hasPendingProposal) =>
        hasPendingOffer
            ? "Kulüp yanıtı bekliyor — teklifi ilerlet veya güncelle"
            : hasPendingProposal
                ? "Oyuncu/menajer yanıtı bekliyor — sözleşmeyi ilerlet"
                : statusCode switch
                {
                    1 or 5 => "Görüşmeyi ilerlet — kulüp masasına geç",
                    6 or 8 => "Kulüp teklifi sun veya yanıtı al",
                    9 or 10 => "Sözleşme teklifini sun veya yanıtı al",
                    11 or 12 => "Yönetim onayını tamamla",
                    13 or 14 => "İmzayı tamamla",
                    _ => "Sıradaki adımı uygula",
                };

    public static string DealLine(
        string playerName,
        int statusCode,
        int tensionPercent,
        bool hasPendingOffer,
        bool hasPendingProposal)
    {
        var stage = StageNumber(statusCode);
        return $"{playerName} · Aşama {stage}/5 — {StageLabel(statusCode)}"
            + $"\nSenin işin: {NextActionHint(statusCode, hasPendingOffer, hasPendingProposal)}"
            + $"\nGerilim %{tensionPercent}";
    }

    public static string OfferStatusLabel(string? statusName, bool pending) =>
        pending
            ? "Kulüp yanıtı bekleniyor"
            : statusName switch
            {
                "Kabul" => "Kulüp kabul etti",
                "Ret" => "Kulüp reddetti",
                "Geçersiz" => "Teklif geçersiz (güncellendi)",
                "Bekliyor" => "Kulüp yanıtı bekleniyor",
                _ => string.IsNullOrWhiteSpace(statusName) ? "Teklif durumu belirsiz" : statusName.Trim(),
            };

    public static string ContractStatusLabel(string? statusName, bool pending) =>
        pending
            ? "Oyuncu/menajer yanıtı bekleniyor"
            : statusName switch
            {
                "Kabul" => "Şartlar kabul edildi",
                "Ret" => "Şartlar reddedildi",
                "Geçersiz" => "Teklif geçersiz (güncellendi)",
                "Bekliyor" => "Oyuncu/menajer yanıtı bekleniyor",
                _ => string.IsNullOrWhiteSpace(statusName) ? "Sözleşme durumu belirsiz" : statusName.Trim(),
            };

    public static string WindowLabel(bool isOpen, string? openDate, string? closeDate)
    {
        var state = isOpen ? "Açık" : "Kapalı";
        var openText = string.IsNullOrWhiteSpace(openDate) ? string.Empty : $" · açılış {openDate}";
        var closeText = string.IsNullOrWhiteSpace(closeDate) ? string.Empty : $" · kapanış {closeDate}";
        return $"Transfer penceresi: {state}{openText}{closeText}";
    }
}
