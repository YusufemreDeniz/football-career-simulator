using FootballCareerSimulator.Domain.Shared;

namespace FootballCareerSimulator.Domain.Transfer;

/// <summary>
/// Bir kulübün bir teknik direktörün transfer pazarlık tarzı hakkında edindiği davranışsal hafıza dosyası (Yenilik 9: Pazarlık Belleği).
/// Epistemik kural gereği her kulüp yalnızca kendi müzakerelerini veya kamuya açık deadline hamlelerini tutar.
/// </summary>
public sealed class NegotiationBehaviorDossier
{
    public const int MinScore = 0;
    public const int MaxScore = 100;
    public const int DefaultNeutralScore = 50;

    public NegotiationBehaviorDossier(
        ClubId observingClubId,
        long managerId,
        int openingBidAggression = DefaultNeutralScore,
        int deadlineDesperation = DefaultNeutralScore,
        int walkawayTendency = DefaultNeutralScore,
        int persistenceIndex = DefaultNeutralScore,
        int totalInteractionsCount = 0)
    {
        ObservingClubId = observingClubId;
        ManagerId = managerId;
        OpeningBidAggression = Math.Clamp(openingBidAggression, MinScore, MaxScore);
        DeadlineDesperation = Math.Clamp(deadlineDesperation, MinScore, MaxScore);
        WalkawayTendency = Math.Clamp(walkawayTendency, MinScore, MaxScore);
        PersistenceIndex = Math.Clamp(persistenceIndex, MinScore, MaxScore);
        TotalInteractionsCount = Math.Max(0, totalInteractionsCount);
    }

    public ClubId ObservingClubId { get; }

    public long ManagerId { get; }

    /// <summary>
    /// Masaya piyasa değerinin çok altında teklifle oturma eğilimi (0–100).
    /// </summary>
    public int OpeningBidAggression { get; private set; }

    /// <summary>
    /// Transferin son saatlerinde değerinin üzerinde panik teklifi yapma eğilimi (0–100).
    /// </summary>
    public int DeadlineDesperation { get; private set; }

    /// <summary>
    /// Karşı teklif beğenilmediğinde masadan çekilme ve görüşmeyi kesme sıklığı (0–100).
    /// </summary>
    public int WalkawayTendency { get; private set; }

    /// <summary>
    /// Reddedilmesine rağmen aynı oyuncu için tekrar tekrar geri gelme inatçılığı (0–100).
    /// </summary>
    public int PersistenceIndex { get; private set; }

    public int TotalInteractionsCount { get; private set; }

    public static NegotiationBehaviorDossier CreateDefault(ClubId observingClubId, long managerId) =>
        new(observingClubId, managerId);

    public void RecordBid(int offeredFee, int estimatedMarketValue, bool isDeadlineDay)
    {
        if (estimatedMarketValue > 0)
        {
            var ratio = (double)offeredFee / estimatedMarketValue;
            if (ratio < 0.65)
            {
                // Değerinin %65'inin altı: Agresif düşük açılış
                OpeningBidAggression = Math.Clamp(OpeningBidAggression + 8, MinScore, MaxScore);
            }
            else if (ratio >= 0.95)
            {
                // Adil veya yüksek açılış
                OpeningBidAggression = Math.Clamp(OpeningBidAggression - 4, MinScore, MaxScore);
            }

            if (isDeadlineDay && ratio > 1.20)
            {
                // Son gün değerinin %20 üstü: Deadline paniği
                DeadlineDesperation = Math.Clamp(DeadlineDesperation + 12, MinScore, MaxScore);
            }
        }

        TotalInteractionsCount++;
    }

    public void RecordWalkaway()
    {
        WalkawayTendency = Math.Clamp(WalkawayTendency + 10, MinScore, MaxScore);
        TotalInteractionsCount++;
    }

    public void RecordRepeatedInquiry()
    {
        PersistenceIndex = Math.Clamp(PersistenceIndex + 8, MinScore, MaxScore);
        TotalInteractionsCount++;
    }
}
