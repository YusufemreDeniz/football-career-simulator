using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Queries;

namespace FootballCareerSimulator.Presentation;

public static class FtueChampionshipMatchDirector
{
    public const string FtueHomeClubName = "YILDIZLAR KULÜBÜ";
    public const string FtueAwayClubName = "MİLANO MAVİ-SİYAH";

    public static PlayMatchesUiResult CreateChampionshipFinalResult()
    {
        var moments = new List<MatchKeyMomentReadModel>
        {
            new("Foul", 85, IsHomeSide: false, PrimarySlotIndex: 6, PrimaryPlayerName: "Barella"),
            new("Save", 86, IsHomeSide: false, PrimarySlotIndex: 0, PrimaryPlayerName: "Sommer"),
            new("Woodwork", 88, IsHomeSide: true, PrimarySlotIndex: 9, AssistSlotIndex: 7, PrimaryPlayerName: "Kaptan Forvet", AssistPlayerName: "Oyun Kurucu"),
            new("Goal", 89, IsHomeSide: true, PrimarySlotIndex: 9, AssistSlotIndex: 7, PrimaryPlayerName: "Kaptan Forvet", AssistPlayerName: "Oyun Kurucu"),
            new("Save", 90, IsHomeSide: true, PrimarySlotIndex: 0, PrimaryPlayerName: "Uğurcan"),
        };

        var matchLines = new List<string>
        {
            "AVRUPA ŞAMPİYONLUK FİNALİ · DEV STADYUM",
            $"{FtueHomeClubName} 2 - 1 {FtueAwayClubName}",
            "89' GOL! Kaptan Forvet (Asist: Oyun Kurucu)",
        };

        var narrative = new MatchNightNarrative(
            BrandTitle: "AVRUPA ŞAMPİYONLUK FİNALİ",
            Scoreline: "2 - 1",
            OutcomeTone: "KUPA SENİN! TARİHİ ZAFER!",
            SupportingLine: "89. dakikada verdiğin cesur taktik kararı ve doksana giden vuruş kulübe Avrupa kupasını getirdi!",
            BeatLines: new[] { "85' Skor 1-1, kupa bir adım uzakta.", "88' Kritik Pozisyon Kararı Uygulandı", "89' GOL! Kaptan Forvet" },
            AfterWhistleLines: new[] { "Stadyumda 85.000 taraftar şampiyonluğu kutluyor.", "Avrupa kupası ellerinde yükseliyor!" },
            OtherScorelines: Array.Empty<string>(),
            KickoffLines: new[] { "Büyük stadyumda tarihi gece.", "İki dev takım şampiyonluk için sahada." },
            LineupBridge: null,
            ManagedGoalMargin: 1,
            Atmosphere: new StadiumAtmosphereDigest("ŞAMPİYONLUK COŞKUSU", "Tribünler 'Şampiyon!' tezahüratlarıyla inliyor.", "Final 85.000"));

        var dressingRoom = new CaptainReactionDigest(
            "SOYUNMA ODASI ÇILGINLIĞI",
            "Kaptan: 'Hocam, o 88. dakikadaki hamleniz maçı bize getirdi! Bu kupa sizin!'");

        var formVerdict = new FormStreakVerdictDigest(
            "KUPA FORM ZİRVESİ",
            "Avrupa'nın zirvesindesin · Tüm gözler yeni transfer dönemine ve kariyerine çevrildi.",
            FormStreakVerdictDigest.WinningExtended);

        return new PlayMatchesUiResult(
            Succeeded: true,
            Message: "Avrupa Şampiyonluk Finali tamamlandı!",
            MatchLines: matchLines,
            ConsequenceLines: new[] { "Avrupa Şampiyonluğu kazanıldı!", "Menajerlik piyasa değeri tavan yaptı." },
            KeyMomentLines: new[] { "88' Kritik Pozisyon Kararı Uygulandı", "89' Şampiyonluk Golü!" },
            Narrative: narrative,
            DressingRoom: dressingRoom,
            FormStreakVerdict: formVerdict,
            KeyMoments: moments,
            MatchSequenceSeed: 2026,
            ManagedGoals: 2,
            OpponentGoals: 1);
    }
}
