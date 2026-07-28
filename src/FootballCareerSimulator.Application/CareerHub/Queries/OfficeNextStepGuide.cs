namespace FootballCareerSimulator.Application.CareerHub.Queries;

/// <summary>
/// Ofis / Kariyere Dönüş sonrası Presentation kısayolu — fokus → hedef sayfa.
/// Diyalog/gazeteci açmaz; yalnızca mevcut hub yüzeylerine yönlendirir.
/// </summary>
public static class OfficeNextStepGuide
{
    public const string TargetToday = "Today";
    public const string TargetClub = "Club";
    public const string TargetTransfer = "Transfer";
    public const string TargetPrep = "Prep";
    public const string TargetWorld = "World";

    public static OfficeNextStep? Resolve(string? focusCode)
    {
        if (string.IsNullOrWhiteSpace(focusCode))
        {
            return null;
        }

        return focusCode switch
        {
            TodayPulseDigest.FocusDesk => new OfficeNextStep(
                "Masada'ya Git",
                TargetToday,
                TodayPulseDigest.FocusDesk),
            TodayPulseDigest.FocusMatch => new OfficeNextStep(
                "Bugün / Sıradaki Maç",
                TargetToday,
                TodayPulseDigest.FocusMatch),
            TodayPulseDigest.FocusSquad => new OfficeNextStep(
                "Kulüp / Kadro",
                TargetClub,
                TodayPulseDigest.FocusSquad),
            TodayPulseDigest.FocusTransfer => new OfficeNextStep(
                "Transfer Masası",
                TargetTransfer,
                TodayPulseDigest.FocusTransfer),
            TodayPulseDigest.FocusPrep => new OfficeNextStep(
                "Hazırlık Masası",
                TargetPrep,
                TodayPulseDigest.FocusPrep),
            TodayPulseDigest.FocusLeague => new OfficeNextStep(
                "Lig Masası",
                TargetWorld,
                TodayPulseDigest.FocusLeague),
            _ => null,
        };
    }
}

public sealed record OfficeNextStep(
    string ButtonLabel,
    string TargetPageCode,
    string FocusCode);
