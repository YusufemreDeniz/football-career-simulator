using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.CareerHub.Queries;

public sealed record CalendarDayTile(
    int DayOffset,
    string DayBadge,
    string Icon,
    string Title,
    string Subtitle,
    bool IsMatchDay,
    bool IsHighLoad);

public sealed record DailyObjectiveTile(
    string ObjectiveCode,
    string Icon,
    string Title,
    string Description,
    string RewardBadge,
    bool IsCompleted,
    string ActionButtonLabel);

public sealed record TactileManagerCard(
    string CardCode,
    string Icon,
    string Title,
    string RarityTier,
    string EffectHeadline,
    string FacilitySource,
    int AvailableCharges,
    string ButtonLabel);

public sealed record PortalFeedTile(
    string CategoryCode,
    string RoleBadge,
    string Icon,
    string Headline,
    string Detail,
    string ActionCode,
    string ActionLabel,
    bool IsUrgent);

public sealed record MobilePortalAndDeckDigest(
    IReadOnlyList<CalendarDayTile> CalendarStrip,
    IReadOnlyList<DailyObjectiveTile> DailyObjectives,
    int CompletedObjectivesCount,
    int TotalObjectivesCount,
    IReadOnlyList<TactileManagerCard> TactileDeck,
    IReadOnlyList<PortalFeedTile> PortalStream)
{
    public const string FilterAll = "ALL";
    public const string FilterMustRespond = "MUST_RESPOND";
    public const string FilterTasks = "TASKS";
    public const string FilterAdvice = "ADVICE";

    public const string CardEnergyPack = "CARD_ENERGY_PACK";
    public const string CardPepTalk = "CARD_PEP_TALK";
    public const string CardPhysioSpa = "CARD_PHYSIO_SPA";
    public const string CardScoutPack = "CARD_SCOUT_PACK";

    public static MobilePortalAndDeckDigest Compose(
        GameDate currentDate,
        bool hasUpcomingMatch,
        int? nextMatchDaysUntil,
        string nextOpponentName,
        bool isHomeMatch,
        bool isSelectionApproved,
        bool hasTrainingPlan,
        bool hasTacticPlan,
        int averageSquadFitness,
        int fatiguedPlayerCount,
        int pendingDecisionCount,
        string? pendingDecisionSummary,
        int openTransferNeedCount,
        int facilityLevel,
        int cardsUsedTodayCount)
    {
        // 1. 7-Day Visual Calendar Strip (2-Week / 7-Tile Snapshot)
        var calendar = new List<CalendarDayTile>(7);
        for (var offset = 0; offset < 7; offset++)
        {
            var date = currentDate.AddDays(offset);
            var dayBadge = offset == 0
                ? "BUGÜN"
                : offset == 1
                    ? "YARIN"
                    : $"+{offset}G ({date.ToIsoDateString()[5..]})";

            var isMatch = hasUpcomingMatch && nextMatchDaysUntil == offset;
            if (isMatch)
            {
                var venue = isHomeMatch ? "İÇ SAHA" : "DEPLASMAN";
                calendar.Add(new CalendarDayTile(
                    offset,
                    dayBadge,
                    "ball",
                    $"vs {nextOpponentName}",
                    $"MAÇ GÜNÜ · {venue}",
                    IsMatchDay: true,
                    IsHighLoad: true));
            }
            else if (hasUpcomingMatch && nextMatchDaysUntil.HasValue && offset == nextMatchDaysUntil.Value + 1)
            {
                calendar.Add(new CalendarDayTile(
                    offset,
                    dayBadge,
                    "medical",
                    "Rejenerasyon",
                    "Maç ertesi toparlanma",
                    IsMatchDay: false,
                    IsHighLoad: false));
            }
            else if (fatiguedPlayerCount >= 3 && offset <= 1)
            {
                calendar.Add(new CalendarDayTile(
                    offset,
                    dayBadge,
                    "medical",
                    "Dinlenme Önerisi",
                    $"{fatiguedPlayerCount} yorgun oyuncu",
                    IsMatchDay: false,
                    IsHighLoad: true));
            }
            else
            {
                var isTacticalDay = hasUpcomingMatch && nextMatchDaysUntil == offset + 1;
                calendar.Add(new CalendarDayTile(
                    offset,
                    dayBadge,
                    isTacticalDay ? "target" : "bolt",
                    isTacticalDay ? "Taktik Prova" : "Antrenman Drili",
                    isTacticalDay ? "Son maç provası" : "Kondisyon & form",
                    IsMatchDay: false,
                    IsHighLoad: false));
            }
        }

        // 2. 5-Minute Daily Objectives (SM27 / Top Eleven Loop)
        var objTrainingDone = hasTrainingPlan && averageSquadFitness >= 74;
        var objSquadDone = isSelectionApproved || !hasUpcomingMatch;
        var objTacticDone = hasTacticPlan && cardsUsedTodayCount > 0;

        var objectives = new List<DailyObjectiveTile>
        {
            new(
                "OBJ_TRAINING",
                "bolt",
                "Günlük İdman & Kondisyon Drili",
                objTrainingDone
                    ? "Takım antrenman temposu ve kondisyon barı hazır."
                    : "Antrenman drili seç veya yorgun kadroya rejenerasyon uygula.",
                "+40 XP · +%8 Hazırlık",
                objTrainingDone,
                objTrainingDone ? "Tamamlandı" : "İdman Uygula"),
            new(
                "OBJ_ROTATION",
                "target",
                "Maç Kadrosu & Akıllı Rotasyon",
                objSquadDone
                    ? "İlk 11 onaylandı, söz verilen oyuncular kontrol edildi."
                    : "Sıradaki maç için Akıllı Rotasyon 11'i kur ve kadroyu onayla.",
                "+45 XP · +Güven",
                objSquadDone,
                objSquadDone ? "Tamamlandı" : "Rotasyon & Onay"),
            new(
                "OBJ_TACTIC_CARD",
                "cards",
                "Dokunsal Menajer Kartı Kullan",
                objTacticDone
                    ? "Bugünün menajer kartı hamlesi sahaya yansıdı."
                    : "Desteden bir Enerji, Motivasyon, Spa veya Scout kartı oyna.",
                "+50 XP · +Moral Bonus",
                objTacticDone,
                objTacticDone ? "Tamamlandı" : "Kart Oyna"),
        };

        var completedCount = objectives.Count(o => o.IsCompleted);

        // Kural 3: Kart Seviye Atlama (Günlük hedefler tamamlandıkça Bronz -> Gümüş -> Altın)
        var cardTier = completedCount switch
        {
            0 => "BRONZ KART (Sv.1)",
            1 or 2 => "GÜMÜŞ KART (Sv.2)",
            _ => "ALTIN KART (Sv.3)",
        };
        var energyValue = completedCount switch
        {
            0 => 8,
            1 or 2 => 12,
            _ => 16,
        };
        var moraleValue = completedCount switch
        {
            0 => 6,
            1 or 2 => 10,
            _ => 15,
        };

        // 3. Tactile Manager Action Deck (New Star Manager / Top Drives Style)
        var baseCharges = Math.Max(1, Math.Min(4, 1 + (facilityLevel / 2)));
        var remainingEnergy = Math.Max(1, baseCharges - (cardsUsedTodayCount / 2));
        var deck = new List<TactileManagerCard>
        {
            new(
                CardEnergyPack,
                "bolt",
                "ENERJİ PAKETİ",
                cardTier,
                $"Yorgun oyunculara +{energyValue} Kondisyon & -15 Yorgunluk",
                $"Antrenman Tesisi (Sv.{facilityLevel})",
                remainingEnergy,
                "Enerji Bas"),
            new(
                CardPepTalk,
                "flame",
                "MOTİVASYON KONUŞMASI",
                cardTier,
                $"Soyunma odası güveni +{moraleValue} & İlk 11 odak patlaması",
                "Teknik Ekip & Kaptan",
                Math.Max(1, baseCharges),
                "Konuşma Yap"),
            new(
                CardPhysioSpa,
                "medical",
                "FİZYO & SPA BAKIMI",
                cardTier,
                "Tüm kadroda sakatlık riski -%35 & hızlı toparlanma",
                $"Sağlık Merkezi (Sv.{facilityLevel})",
                remainingEnergy,
                "Spa Uygula"),
            new(
                CardScoutPack,
                "radar",
                "ELİT SCOUT DOSYASI",
                cardTier,
                "Eksik mevkiye anında 3 karşılaştırmalı transfer adayı",
                "Baş Gözlemci Ağı",
                Math.Max(1, baseCharges),
                "Rapor Aç"),
        };

        // 4. FM26 Portal Stream (Must Respond / Tasks / Staff Advice)
        var stream = new List<PortalFeedTile>();

        if (pendingDecisionCount > 0)
        {
            stream.Add(new PortalFeedTile(
                FilterMustRespond,
                "ZORUNLU KARAR",
                "warning",
                $"Masada {pendingDecisionCount} bekleyen kritik karar var",
                string.IsNullOrWhiteSpace(pendingDecisionSummary)
                    ? "Oyuncu veya yönetim talebini yanıtlamadan hafta kapanmaz."
                    : pendingDecisionSummary,
                "ACTION_ANSWER_DECISION",
                "Kararı Yanıtla",
                IsUrgent: true));
        }

        if (hasUpcomingMatch && !isSelectionApproved)
        {
            stream.Add(new PortalFeedTile(
                FilterTasks,
                "MAÇ GÖREVİ",
                "pitch",
                $"{nextOpponentName} maçı için İlk 11 onayı bekleniyor",
                "Akıllı Rotasyon ile yorgunları dinlendirip tek dokunuşla kadroyu onaylayabilirsin.",
                "ACTION_SMART_ROTATION",
                "Akıllı 11 & Onayla",
                IsUrgent: true));
        }

        if (fatiguedPlayerCount > 0 || averageSquadFitness < 78)
        {
            stream.Add(new PortalFeedTile(
                FilterAdvice,
                "BAŞ FİZYOTERAPİST",
                "medical",
                $"Kadro zindeliği %{averageSquadFitness} ({fatiguedPlayerCount} yorgun oyuncu)",
                "Maç temposu düşmeden Enerji Paketi veya Fizyo Spa kartı kullanmanız öneriliyor.",
                CardEnergyPack,
                "Enerji Kartı Kullan",
                IsUrgent: fatiguedPlayerCount >= 3));
        }
        else
        {
            stream.Add(new PortalFeedTile(
                FilterAdvice,
                "YARDIMCI ANTRENÖR",
                "bolt",
                $"Takım zindeliği yüksek (%{averageSquadFitness})",
                "Oyuncular yüksek tempolu taktik idmana ve önde baskı planına hazır.",
                "ACTION_OPEN_TACTICS",
                "Taktik Sahayı Aç",
                IsUrgent: false));
        }

        if (openTransferNeedCount > 0)
        {
            stream.Add(new PortalFeedTile(
                FilterAdvice,
                "BAŞ GÖZLEMCİ",
                "radar",
                $"{openTransferNeedCount} açık kadro ihtiyacı için adaylar hazır",
                "3 adaylı karşılaştırma masasında güven kademesi yüksek oyuncuları incele.",
                CardScoutPack,
                "3 Adayı Karşılaştır",
                IsUrgent: false));
        }
        else
        {
            stream.Add(new PortalFeedTile(
                FilterTasks,
                "KULÜP KAMPÜSÜ",
                "campus",
                $"Tesis Seviyesi Sv.{facilityLevel} · Kampüs Kart Üretimi Aktif",
                "Stadyum, antrenman ve akademi kampüsünü geliştirerek haftalık kart ve gelir bonusunu artır.",
                "ACTION_OPEN_CAMPUS",
                "Kampüsü Aç",
                IsUrgent: false));
        }

        return new MobilePortalAndDeckDigest(
            calendar,
            objectives,
            completedCount,
            objectives.Count,
            deck,
            stream);
    }
}
