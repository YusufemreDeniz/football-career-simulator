using FootballCareerSimulator.Application.ClubGovernance.Queries;
using FootballCareerSimulator.Application.ClubGovernance.Services;
using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.ContractRegistration.Queries;
using FootballCareerSimulator.Application.Interaction.Queries;
using FootballCareerSimulator.Application.ManagerCareer.Queries;
using FootballCareerSimulator.Application.PlayerCareer.Queries;
using FootballCareerSimulator.Application.SocialContinuity.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Application.Transfer.Queries;
using FootballCareerSimulator.Application.WorldCalendar.Queries;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;
using System.Linq;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Presentation katmanının (özellikle CareerHubScreen) domain/application sorgularına
/// doğrudan PresentationHost yerine temiz, amaca yönelik metodlarla erişmesini sağlayan sorgu cephesi.
/// </summary>
public sealed partial class CareerSessionController
{
    // ── Menajer ve Kulüp Kimliği ──────────────────────────────────────────────

    /// <summary>Menajerin şu an aktif bir kulüpte çalışıp çalışmadığını bildirir.</summary>
    public bool IsManagerEmployed => Host.ManagerModule.Queries.GetCareer().EmployedClubId is not null;

    /// <summary>Menajerin çalıştırdığı kulübün kimliğini döner (boştaysa null).</summary>
    public long? GetEmployedClubId() => Host.ManagerModule.Queries.GetCareer().EmployedClubId;

    /// <summary>Menajer kariyer profilini döner.</summary>
    public ManagerCareerReadModel GetManagerCareer() => Host.ManagerModule.Queries.GetCareer();

    /// <summary>Belirtilen kulübün detayını sorgular.</summary>
    public ClubReadModel? GetClub(long clubId) => Host.ClubModule.Queries.GetClub(clubId);

    /// <summary>Menajerin çalıştırdığı kulüp kaydını döner.</summary>
    public ClubReadModel? GetEmployedClub()
    {
        var clubId = GetEmployedClubId();
        return clubId.HasValue ? GetClub(clubId.Value) : null;
    }

    /// <summary>Mevcut kulüpteki göreve başlama gününü döner.</summary>
    public int? GetEmploymentStartedDayNumber() =>
        Host.ManagerModule.Store.Career.ActiveEmployment?.StartedAt.DayNumber;

    // ── Takvim ve Simülasyon Zamanı ──────────────────────────────────────────

    /// <summary>Mevcut oyun takvim tarihini döner.</summary>
    public CurrentGameDateReadModel GetCurrentGameDate() => Host.WorldModule.Queries.GetCurrentGameDate();

    /// <summary>Mevcut planlama dönemini döner.</summary>
    public CurrentPlanningPeriodReadModel? GetCurrentPlanningPeriod() => Host.WorldModule.Queries.GetCurrentPlanningPeriod();

    /// <summary>Transfer penceresi durumunu döner.</summary>
    public TransferWindowReadModel GetTransferWindow() => Host.WorldModule.Queries.GetTransferWindow();

    /// <summary>Transfer penceresinin şu an açık olup olmadığını döner.</summary>
    public bool IsTransferWindowOpen => GetTransferWindow().IsOpen;

    /// <summary>Zaman ilerletme koşullarının uygun olup olmadığını döner.</summary>
    public bool CanAdvanceTime() => Host.WorldModule.Queries.GetTimeAdvanceEligibility().CanAdvance;

    /// <summary>Dünya zaman çizgisini döner.</summary>
    public WorldTimeline GetTimeline() => Host.WorldModule.TimelineStore.Timeline;

    // ── Transfer ve Finans ───────────────────────────────────────────────────

    /// <summary>Yönetilen kulübe gelen transfer tekliflerini döner.</summary>
    public ManagedClubOffersReadModel GetManagedClubOffers() => Host.TransferModule.Queries.GetManagedClubOffers();

    /// <summary>Yönetilen kulübün oyuncu sözleşme tekliflerini döner.</summary>
    public ManagedClubContractProposalsReadModel GetManagedContractProposals() => Host.TransferModule.Queries.GetManagedContractProposals();

    /// <summary>Yönetilen kulübün aktif transfer süreçlerini döner.</summary>
    public ManagedClubTransferProcessesReadModel GetManagedClubProcesses() => Host.TransferModule.Queries.GetManagedClubProcesses();

    /// <summary>Yönetilen kulübün transfer kısa listesini döner.</summary>
    public ManagedClubShortlistTargetsReadModel GetManagedClubShortlistTargets() => Host.TransferModule.Queries.GetManagedClubShortlistTargets();

    /// <summary>Yönetilen kulübün transfer ihtiyaçlarını döner.</summary>
    public ManagedClubTransferNeedsReadModel GetManagedClubNeeds() => Host.TransferModule.Queries.GetManagedClubNeeds();

    /// <summary>Kulübün transfer bütçesini sorgular.</summary>
    public ClubTransferBudgetSnapshot GetTransferBudget(long clubId) => Host.ClubModule.TransferBudget.Get(new ClubId(clubId));

    /// <summary>Kulübün maaş bütçesini sorgular.</summary>
    public ClubWageBudgetSnapshot? GetWageBudget(long clubId, GameDate date) => Host.ClubModule.WageBudget?.Get(new ClubId(clubId), date);

    /// <summary>Belirli bir transfer sürecinin teklif turlarını listeler.</summary>
    public IReadOnlyList<ClubOffer> GetOfferRounds(long processId) =>
        Host.TransferModule.OfferStore.Offers.Where(o => o.ProcessId.Value == processId).ToList();

    /// <summary>Belirli bir transfer sürecinin sözleşme teklif turlarını listeler.</summary>
    public IReadOnlyList<PlayerContractProposal> GetContractProposalRounds(long processId) =>
        Host.TransferModule.ProposalStore.Proposals.Where(p => p.ProcessId.Value == processId).ToList();

    /// <summary>Yönetilen kulübe transfer edilebilecek serbest oyuncu olup olmadığını sorgular.</summary>
    public bool HasSignableFreeAgent() =>
        Host.ContractModule.Queries.GetNextSignableFreeAgentForManagedClub() is not null;

    // ── Takım Hazırlığı ve Kadro ─────────────────────────────────────────────

    /// <summary>Yönetilen kulübün taktik planını döner.</summary>
    public TacticPlanReadModel? GetManagedClubTacticPlan() => Host.TeamPreparationModule.TacticQueries.GetManagedClubPlan();

    /// <summary>Sıradaki oynanacak fikstür kadro seçimini döner.</summary>
    public ManagedFixtureSelectionStatusReadModel? GetNextDueManagedFixture(int? dayNumber = null)
    {
        var day = dayNumber ?? GetCurrentGameDate().DayNumber;
        return Host.TeamPreparationModule.SelectionQueries.GetNextDueManagedFixture(day);
    }

    /// <summary>Kadro seçimine ilişkin söz baskısı/gerilimi durumunu döner.</summary>
    public PreMatchPromiseTensionReadModel? GetPromiseTensionForNextDueMatch(int? dayNumber = null)
    {
        var day = dayNumber ?? GetCurrentGameDate().DayNumber;
        return Host.TeamPreparationModule.PromiseTension.GetForNextDueMatch(day);
    }

    /// <summary>Kulübün kalıcı kadro şablonunu döner.</summary>
    public ClubSquad? GetPersistedSquad(long clubId) =>
        Host.TeamPreparationModule.SquadStore.Get(new ClubId(clubId));

    /// <summary>Kadro sözleşme özetini döner.</summary>
    public ClubContractSummaryReadModel GetManagedClubContractSummary() =>
        Host.ContractModule.Queries.GetManagedClubSummary();

    /// <summary>Kadro gelişim özetini döner.</summary>
    public ClubDevelopmentSummaryReadModel GetManagedClubDevelopmentSummary() =>
        Host.PlayerCareerModule.Queries.GetManagedClubSummary();

    // ── Karar Masası ve Diyalog ──────────────────────────────────────────────

    /// <summary>Bekleyen karar taleplerini döner.</summary>
    public PendingDecisionsReadModel GetPendingDecisions(int take = 5) =>
        Host.InteractionModule.Queries.GetPending(take);

    /// <summary>Bir karar talebine ilişkin diyalog seçeneklerini döner.</summary>
    public DialogueOptionsReadModel GetDialogueOptionsForDecision(long decisionId) =>
        Host.InteractionModule.DialogueOptions.GetForDecision(new DecisionRequestId(decisionId));

    /// <summary>Menajer cevabı bekleyen diyalog oturumu sayısını döner.</summary>
    public int GetAwaitingDecisionSessionCount() =>
        Host.InteractionModule.DialogueSessionStore.Sessions.Count(s => s.IsAwaitingPlayer);

    // ── Sosyal Hafıza ve İlişkiler ───────────────────────────────────────────

    /// <summary>Menajerin aktif hafıza kayıtlarını döner.</summary>
    public ActorMemoriesReadModel GetActiveMemoriesForManager(int take = 5)
    {
        var career = GetManagerCareer();
        return Host.SocialContinuityModule.Queries.GetActiveForActor(
            ActorKind.Manager,
            career.ManagerId,
            take);
    }

    /// <summary>Menajerin verdiği aktif söz kayıtlarını döner.</summary>
    public ActorPromisesReadModel GetActivePromisesForManager(int take = 5)
    {
        var career = GetManagerCareer();
        return Host.SocialContinuityModule.PromiseQueries.GetActiveForPromisor(
            ActorKind.Manager,
            career.ManagerId,
            take);
    }

    /// <summary>Menajere yönelik aktif ilişki kayıtlarını döner.</summary>
    public ManagerRelationshipsReadModel GetActiveRelationshipsForManager(int take = 5)
    {
        var career = GetManagerCareer();
        return Host.SocialContinuityModule.RelationshipQueries.GetActiveForManager(
            career.ManagerId,
            take);
    }

    // ── Lig ve Müsabaka ──────────────────────────────────────────────────────

    /// <summary>Mevcut lig sezonunu döner.</summary>
    public CurrentSeasonReadModel? GetCurrentSeason() => Host.CompetitionModule.Queries.GetCurrentSeason();

    /// <summary>Sezon ilerleme durumunu döner.</summary>
    public SeasonProgressReadModel? GetSeasonProgress(long seasonId) =>
        Host.CompetitionModule.Queries.GetSeasonProgress(seasonId);

    /// <summary>Belirli bir haftanın fikstür maçlarını döner.</summary>
    public IReadOnlyList<FixtureReadModel> GetFixturesByRound(long seasonId, int roundNumber) =>
        Host.CompetitionModule.Queries.GetFixturesByRound(seasonId, roundNumber);

    /// <summary>Belirli bir gün veya öncesinde planlanmış müsabaka sayısını döner.</summary>
    public int GetDueFixtureCount(long seasonId, int dayNumber) =>
        Host.CompetitionModule.Queries
            .GetSeasonFixtures(seasonId)
            .Count(fixture =>
                fixture.ScheduledDayNumber <= dayNumber
                && string.Equals(fixture.Status, nameof(FixtureStatus.Planned), StringComparison.Ordinal));

    /// <summary>Güncel puan durumunu döner. Sezon başında henüz maç oynanmamışsa katılımcı kulüplerden 0 puanlı tablo üretir.</summary>
    public IReadOnlyList<StandingEntryReadModel> GetStandings(long seasonId)
    {
        var entries = Host.CompetitionModule.Queries.GetStandings(seasonId);
        if (entries is { Count: > 0 })
        {
            return entries;
        }

        var participants = Host.CompetitionModule.Queries.GetSeasonParticipants(seasonId);
        if (participants is null || participants.Count == 0)
        {
            return Array.Empty<StandingEntryReadModel>();
        }

        return participants
            .Select(p =>
            {
                var club = Host.ClubModule.Queries.GetClub(p.ClubId);
                return (
                    p.ClubId,
                    Strength: club?.SportiveStrength ?? 50,
                    Name: club?.DisplayName ?? GetClubDisplayName(p.ClubId));
            })
            .OrderByDescending(x => x.Strength)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => new StandingEntryReadModel(
                x.ClubId,
                Played: 0,
                Won: 0,
                Drawn: 0,
                Lost: 0,
                GoalsFor: 0,
                GoalsAgainst: 0,
                Points: 0,
                GoalDifference: 0))
            .ToArray();
    }

    public FanPulseDigest BuildFanPulseDigest()
    {
        var manager = GetManagerCareer();
        if (manager.EmployedClubId is not long clubId || GetCurrentSeason() is not { } season)
        {
            return FanPulseDigest.Empty;
        }

        var standings = GetStandings(season.SeasonId);
        var tableIndex = standings
            .Select((entry, index) => (entry, index))
            .FirstOrDefault(item => item.entry.ClubId == clubId);
        if (tableIndex.entry is null)
        {
            return FanPulseDigest.Empty;
        }

        var recent = GetSeasonFixtures(season.SeasonId)
            .Where(fixture =>
                fixture.HomeGoals.HasValue
                && (fixture.HomeClubId == clubId || fixture.AwayClubId == clubId))
            .OrderBy(fixture => fixture.Round)
            .TakeLast(5)
            .ToArray();
        var recentPoints = recent.Sum(fixture =>
        {
            var managedGoals = fixture.HomeClubId == clubId ? fixture.HomeGoals!.Value : fixture.AwayGoals!.Value;
            var opponentGoals = fixture.HomeClubId == clubId ? fixture.AwayGoals!.Value : fixture.HomeGoals!.Value;
            return managedGoals > opponentGoals ? 3 : managedGoals == opponentGoals ? 1 : 0;
        });
        var lastMargin = recent.LastOrDefault() is { } last
            ? (last.HomeClubId == clubId
                ? last.HomeGoals!.Value - last.AwayGoals!.Value
                : last.AwayGoals!.Value - last.HomeGoals!.Value)
            : 0;

        return FanPulseDigest.Compose(
            GetClubDisplayName(clubId),
            tableIndex.index + 1,
            standings.Count,
            recentPoints,
            recent.Length,
            tableIndex.entry.GoalDifference,
            lastMargin,
            manager.ManagerReputation,
            manager.BoardConfidence ?? 50);
    }

    /// <summary>Puan durumu özet şeridini döner.</summary>
    public StandingStripReadModel GetStandingsStrip(long seasonId, long? managedClubId = null, int topCount = 5)
    {
        var strip = Host.CompetitionModule.Queries.GetStandingsStrip(seasonId, managedClubId, topCount);
        if (strip.Entries.Count > 0)
        {
            return strip;
        }

        var standings = GetStandings(seasonId);
        if (standings.Count == 0)
        {
            return strip;
        }

        var ranked = standings
            .Select((entry, index) => (
                Rank: index + 1,
                entry.ClubId,
                entry.Points,
                entry.Played,
                IsManaged: managedClubId is long managed && managed == entry.ClubId))
            .ToArray();

        var top = ranked
            .Take(topCount)
            .Select(entry => new StandingStripEntryReadModel(
                entry.Rank,
                entry.ClubId,
                entry.Points,
                entry.Played,
                entry.IsManaged))
            .ToList();

        var managedOutsideTop = false;
        if (managedClubId is long clubId)
        {
            var managed = ranked.FirstOrDefault(entry => entry.ClubId == clubId);
            if (managed.Rank > 0 && managed.Rank > topCount)
            {
                managedOutsideTop = true;
                top.Add(new StandingStripEntryReadModel(
                    managed.Rank,
                    managed.ClubId,
                    managed.Points,
                    managed.Played,
                    IsManaged: true));
            }
        }

        return new StandingStripReadModel(top, managedOutsideTop);
    }

    /// <summary>Tüm sezon fikstür maçlarını döner.</summary>
    public IReadOnlyList<FixtureReadModel> GetSeasonFixtures(long seasonId) =>
        Host.CompetitionModule.Queries.GetSeasonFixtures(seasonId) ?? Array.Empty<FixtureReadModel>();

    /// <summary>Belirtilen kulüp için kulüp/rakip istihbarat dosyasını döner.</summary>
    public OpponentDossierDigest? BuildClubDossier(long clubId)
    {
        var season = Host.CompetitionModule.Queries.GetCurrentSeason();
        if (season is null)
        {
            return null;
        }

        var targetClub = Host.ClubModule.Queries.GetClub(clubId);
        if (targetClub is null)
        {
            return null;
        }

        var managedClubId = GetEmployedClubId();
        var managedClub = managedClubId.HasValue ? Host.ClubModule.Queries.GetClub(managedClubId.Value) : null;
        var managedStrength = managedClub?.SportiveStrength ?? 50;

        var currentDay = Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var nextManaged = Host.TeamPreparationModule.SelectionQueries
            .GetNextPlannedManagedFixture(currentDay);
        var isNextOpponent = nextManaged is not null && nextManaged.OpponentClubId == clubId;
        var isHome = !isNextOpponent || nextManaged!.IsHome;

        return OpponentDossierDigest.Compose(
            clubId,
            targetClub.DisplayName,
            isHome,
            managedStrength,
            targetClub.SportiveStrength,
            GetStandings(season.SeasonId),
            Host.CompetitionModule.Queries.GetSeasonFixtures(season.SeasonId));
    }

    public TeamChemistryProfile GetTeamChemistry()
    {
        var clubId = GetEmployedClubId();
        if (!clubId.HasValue)
        {
            return new TeamChemistryProfile(50, "Kulüp Yok", 1.0, "Bir kulüpte görev almadığınız için takım kimyası hesaplanamaz.");
        }

        var cid = new Domain.Shared.ClubId(clubId.Value);
        var club = Host.ClubModule.Queries.GetClub(clubId.Value);
        var facilityLevel = club?.FacilityLevel ?? 1;
        var board = BuildSquadSelectionBoard();
        var starters = board.StartingXi;
        var starterCount = starters.Count;

        // Doğal mevki uyumu: maç motorunun kullandığı rol uyumu hesabıyla aynı (formasyon rollerine doğal oturan oyuncu sayısı).
        var formation = Host.TeamPreparationModule.TacticPlanStore.Get(cid)?.Formation ?? Formation.F442;
        var profiles = GetSquadProfiles(cid);
        var starterProfiles = starters
            .Where(player => player.SlotIndex >= 0 && player.SlotIndex < profiles.Count)
            .Select(player => profiles[player.SlotIndex])
            .ToList();
        var naturalCount = starterProfiles.Count == 0
            ? 0
            : Simulation.TeamPreparation.MvpLineupRoleFitCalculator.Evaluate(formation, starterProfiles).NaturalFitCount;

        // Yaş dengesi sahadaki ilk 11 üzerinden ölçülür; ilk 11 yoksa tüm kadro kullanılır.
        var management = BuildPlayerManagementDigest();
        var starterSlots = starters.Select(player => player.SlotIndex).ToHashSet();
        var ageSource = management.Players.Where(player => starterSlots.Contains(player.SlotIndex)).ToList();
        if (ageSource.Count == 0)
        {
            ageSource = management.Players.ToList();
        }

        var avgAge = ageSource.Count > 0 ? (int)Math.Round(ageSource.Average(p => p.Age)) : 25;

        var form = BuildDressingRoomEcho();
        var winStreak = form is not null && string.Equals(form.MomentumCode, "WIN_STREAK", StringComparison.OrdinalIgnoreCase)
            ? form.MomentumLength
            : 0;

        return TeamChemistryCalculator.Evaluate(starterCount, naturalCount, avgAge, winStreak, facilityLevel);
    }

    public Application.TrainingPhysicalState.Queries.FixtureCongestionForecastDigest BuildFixtureCongestionForecast()
    {
        var clubId = GetEmployedClubId();
        var season = Host.CompetitionModule.Queries.GetCurrentSeason();
        if (!clubId.HasValue || season is null)
        {
            return Application.TrainingPhysicalState.Queries.FixtureCongestionForecastDigest.Unavailable();
        }

        var currentDay = GetCurrentGameDate().DayNumber;
        var scheduledDays = Host.CompetitionModule.Queries
            .GetSeasonFixtures(season.SeasonId)
            .Where(f => string.Equals(f.Status, "Planned", StringComparison.Ordinal)
                        && (f.HomeClubId == clubId.Value || f.AwayClubId == clubId.Value))
            .Select(f => f.ScheduledDayNumber)
            .ToArray();

        var states = Host.TrainingModule.Store.GetPhysicalStatesForClub(new ClubId(clubId.Value));
        return Application.TrainingPhysicalState.Queries.FixtureCongestionForecastDigest.Compose(
            currentDay,
            scheduledDays,
            states);
    }

    public PreMatchReadinessChecklistDigest BuildPreMatchReadinessChecklist()
    {
        var clubId = GetEmployedClubId();
        if (!clubId.HasValue)
        {
            return PreMatchReadinessChecklistDigest.NoMatch();
        }

        var board = BuildSquadSelectionBoard();
        if (!board.HasMatch)
        {
            return PreMatchReadinessChecklistDigest.NoMatch();
        }

        var cid = new ClubId(clubId.Value);
        var tactic = GetManagedClubTacticPlan();
        var hasTactic = tactic is not null && !string.Equals(tactic.FormationName, "yok", StringComparison.OrdinalIgnoreCase);
        var training = GetTrainingSummary();
        var hasTraining = training.HasPlan;

        var unavailableInXi = board.StartingXi.Count(p => !p.IsAvailable);
        var fatiguedInXi = board.StartingXi.Count(p => p.Fitness < 70 || p.Fatigue >= 65);

        var activePromises = Host.SocialContinuityModule.PromiseStore.GetActiveByManagerAndClub(GetManagerCareer().ManagerId, cid);
        var promisedPlayerIds = activePromises
            .Where(pr => pr.Promisee.Kind == ActorKind.Player)
            .Select(pr => pr.Promisee.Id)
            .ToHashSet();

        var starterSlotSet = board.StartingXi.Select(p => p.SlotIndex).ToHashSet();
        var promisedStarters = 0;
        var promisedBenched = 0;

        var management = BuildPlayerManagementDigest();
        foreach (var player in management.Players)
        {
            if (promisedPlayerIds.Contains(player.PlayerId))
            {
                if (starterSlotSet.Contains(player.SlotIndex))
                {
                    promisedStarters++;
                }
                else
                {
                    promisedBenched++;
                }
            }
        }

        return PreMatchReadinessChecklistDigest.Compose(
            hasUpcomingMatch: true,
            isSelectionApproved: board.IsApproved,
            startingXiCount: board.StartingXi.Count,
            unavailableInXiCount: unavailableInXi,
            fatiguedInXiCount: fatiguedInXi,
            hasTacticPlan: hasTactic,
            hasTrainingPlan: hasTraining,
            activePromiseStarterCount: promisedStarters,
            activePromiseBenchedCount: promisedBenched);
    }

    public TacticalIntelligenceDigest BuildLeagueTacticalIntelligence()
    {
        var season = Host.CompetitionModule.Queries.GetCurrentSeason();
        var seasonYear = 2025 + (int)(season?.SeasonId ?? 1);
        var service = new Application.TeamPreparation.Services.TacticalEcosystemService();

        foreach (var club in Host.ClubModule.Queries.GetAllClubs())
        {
            var cid = new ClubId(club.ClubId);
            var plan = Host.TeamPreparationModule.TacticPlanStore.Get(cid);
            var form = plan?.Formation ?? Formation.F442;
            var app = plan?.Approach ?? TacticalApproach.Balanced;
            service.RecordMatchTactics("TR-SL", seasonYear, form, app, form, app);
        }

        return service.BuildDigest("TR-SL", seasonYear);
    }

    public ManagerBehavioralProfileReadModel BuildManagerBehavioralDigest()
    {
        var manager = GetManagerCareer();
        var clubId = manager.EmployedClubId;
        var youthScore = Math.Clamp(manager.YouthDevelopment, 20, 95);
        var promiseScore = 55;
        var supportScore = Math.Clamp(manager.ManManagement, 20, 95);
        var negotiationScore = Math.Clamp(manager.NegotiationSkill, 20, 95);
        var rotationScore = Math.Clamp(manager.TacticalAcumen, 20, 95);
        var deadlineScore = 50;
        var observations = new List<Domain.ManagerCareer.BehavioralObservation>();

        if (clubId.HasValue)
        {
            var cid = new ClubId(clubId.Value);
            var promises = Host.SocialContinuityModule.PromiseStore.Promises
                .Where(pr => pr.ClubId == cid)
                .ToList();
            var fulfilled = promises.Count(pr => pr.Status == PromiseStatus.Fulfilled);
            var broken = promises.Count(pr => pr.Status == PromiseStatus.Broken);
            promiseScore = Math.Clamp(60 + (fulfilled * 8) - (broken * 14), 10, 98);

            if (fulfilled > 0)
            {
                observations.Add(new Domain.ManagerCareer.BehavioralObservation(
                    "PROMISE_KEPT",
                    Domain.ManagerCareer.BehavioralEvidenceCategory.PromiseReliability,
                    fulfilled * 5,
                    GetCurrentGameDate().DayNumber,
                    "Verilen forma sözleri tutuldu"));
            }
            if (broken > 0)
            {
                observations.Add(new Domain.ManagerCareer.BehavioralObservation(
                    "PROMISE_BROKEN",
                    Domain.ManagerCareer.BehavioralEvidenceCategory.PromiseReliability,
                    -broken * 10,
                    GetCurrentGameDate().DayNumber,
                    "Bozulan söz kaydı"));
            }

            var management = BuildPlayerManagementDigest();
            var u21Count = management.Players.Count(pl => pl.Age <= 21);
            if (u21Count >= 4)
            {
                youthScore = Math.Clamp(youthScore + 12, 0, 100);
                observations.Add(new Domain.ManagerCareer.BehavioralObservation(
                    "YOUTH_TRUST",
                    Domain.ManagerCareer.BehavioralEvidenceCategory.YouthOpportunity,
                    10,
                    GetCurrentGameDate().DayNumber,
                    "A takımda U21 rotasyonu aktif"));
            }
        }

        var profile = new Domain.ManagerCareer.ManagerBehavioralProfile(
            youthOpportunity: youthScore,
            promiseReliability: promiseScore,
            publicSupport: supportScore,
            negotiationPersistence: negotiationScore,
            rotationConsistency: rotationScore,
            deadlineAggression: deadlineScore,
            observations: observations);

        return new Application.ManagerCareer.Services.ManagerBehavioralQueryService().GetProfile(profile);
    }

    public DressingRoomClimateDigest? BuildDressingRoomSocialClimate()
    {
        var clubId = GetEmployedClubId();
        if (!clubId.HasValue)
        {
            return null;
        }

        var cid = new ClubId(clubId.Value);
        var service = new Application.SocialContinuity.Services.SocialContagionService();
        var climate = service.GetOrCreateClimate(cid);

        var rels = Host.SocialContinuityModule.RelationshipStore.GetByManagerSubject(GetManagerCareer().ManagerId);
        var lowTrustCount = rels.Count(r => r.Trust < 40);
        var highTrustCount = rels.Count(r => r.Trust >= 70);

        if (lowTrustCount > 0)
        {
            climate.ApplyCrisis(lowTrustCount * 8);
        }
        else if (highTrustCount >= 3)
        {
            climate.ApplyHarmonyBoost(highTrustCount * 3);
        }

        return service.BuildDigest(cid);
    }

    public LockerRoomRumorDigest BuildActiveLockerRoomRumors()
    {
        var day = GetTimeline().CurrentDate;
        var service = new Application.Interaction.Services.EpistemicKnowledgeService();
        var clubId = GetEmployedClubId();
        if (clubId.HasValue)
        {
            var cid = new ClubId(clubId.Value);
            var brokenPromises = Host.SocialContinuityModule.PromiseStore.Promises
                .Where(pr => pr.ClubId == cid && pr.Status == PromiseStatus.Broken)
                .Take(2)
                .ToList();
            foreach (var broken in brokenPromises)
            {
                var pName = GetPlayerDisplayName(broken.Promisee.Id);
                service.SpreadRumor(new RumorRecord(
                    $"RUMOR_PROMISE_{broken.PromiseId.Value}",
                    "LockerRoom",
                    "PromiseFallout",
                    $"Kulis: {pName} verilen sözün tutulmaması nedeniyle ayrılık düşünüyor.",
                    1,
                    day));
            }

            var activeProcesses = GetManagedClubProcesses().ActiveProcesses;
            foreach (var proc in activeProcesses.Take(1))
            {
                var pName = GetPlayerDisplayName(proc.PlayerId);
                service.SpreadRumor(new RumorRecord(
                    $"RUMOR_TRANSFER_{proc.ProcessId}",
                    "Media",
                    "TransferTarget",
                    $"Basın İddiası: Kulüp {pName} transferinde sona yaklaştı.",
                    2,
                    day));
            }
        }

        return service.BuildLockerRoomRumorDigest(day);
    }

    public SeasonChronicleDigest? BuildCurrentSeasonChronicle()
    {
        var clubId = GetEmployedClubId();
        var season = Host.CompetitionModule.Queries.GetCurrentSeason();
        if (!clubId.HasValue || season is null)
        {
            return null;
        }

        var clubName = GetClubDisplayName(clubId.Value);
        var seasonYear = 2025 + (int)season.SeasonId;
        var standings = GetStandings(season.SeasonId);
        var myStanding = standings.FirstOrDefault(s => s.ClubId == clubId.Value);
        var pos = myStanding is not null ? standings.ToList().IndexOf(myStanding) + 1 : 1;
        var wonTitle = pos == 1 && (myStanding?.Played ?? 0) >= 10;

        var compiler = new Application.Interaction.Services.CareerChronicleCompiler();
        var day = GetTimeline().CurrentDate;

        if (myStanding is not null && myStanding.Played > 0)
        {
            compiler.RecordFact(
                seasonYear,
                new NarrativeFactRecord(
                    $"FACT_FORM_{season.SeasonId}",
                    "LeagueCampaign",
                    80,
                    [clubName],
                    day,
                    $"{clubName} ligde {myStanding.Played} maçta {myStanding.Points} puan topladı ({myStanding.Won}G {myStanding.Drawn}B {myStanding.Lost}M)."));
        }

        var completedTransfers = Host.TransferModule.ProcessStore.GetForBuyingClub(new ClubId(clubId.Value))
            .Where(p => p.IsCompleted)
            .Take(2)
            .ToList();
        foreach (var tr in completedTransfers)
        {
            var pName = GetPlayerDisplayName(tr.PlayerId.Value);
            compiler.RecordFact(
                seasonYear,
                new NarrativeFactRecord(
                    $"FACT_SIGN_{tr.ProcessId.Value}",
                    "KeySigning",
                    75,
                    [pName, clubName],
                    tr.TerminalOn ?? day,
                    $"{pName} transferi tamamlanarak kadro güçlendirildi."));
        }

        return compiler.CompileSeasonChronicle(seasonYear, clubName, pos, wonTitle);
    }

    private int _cardsUsedTodayCount;
    private int _lastCardUseDayNumber = -1;

    public MobilePortalAndDeckDigest BuildMobilePortalAndDeck()
    {
        var day = GetTimeline().CurrentDate;
        if (_lastCardUseDayNumber != day.DayNumber)
        {
            _lastCardUseDayNumber = day.DayNumber;
            _cardsUsedTodayCount = 0;
        }

        var clubId = GetEmployedClubId();
        var nextMatch = BuildNextMatchBriefing();
        var board = BuildSquadSelectionBoard();
        var training = GetTrainingSummary();
        var tactic = GetManagedClubTacticPlan();
        var hasTactic = tactic is not null && !string.Equals(tactic.FormationName, "yok", StringComparison.OrdinalIgnoreCase);

        var avgFitness = board.StartingXi.Count > 0
            ? (int)Math.Round(board.StartingXi.Average(p => p.Fitness))
            : 82;
        var fatiguedCount = board.StartingXi.Count(p => p.Fitness < 75 || p.Fatigue >= 60);

        var decisions = GetPendingDecisions(take: 3);
        var decisionSummary = decisions.OpenCount > 0
            ? FormatActiveBlockerSummary()
            : null;
        var openNeeds = GetManagedClubNeeds().OpenCount;
        var facilityLevel = clubId.HasValue ? (GetClub(clubId.Value)?.FacilityLevel ?? 1) : 1;

        var upcoming = GetUpcomingManagedMatch();
        var hasMatch = upcoming is not null || nextMatch.HasMatch;
        var oppName = upcoming?.OpponentName ?? (nextMatch.HasMatch ? nextMatch.Headline : "Fikstür Bekleniyor");
        var isHome = upcoming?.IsHome ?? true;
        int? daysUntil = upcoming?.DaysUntil ?? (nextMatch.HasMatch ? 0 : null);

        return MobilePortalAndDeckDigest.Compose(
            currentDate: day,
            hasUpcomingMatch: hasMatch,
            nextMatchDaysUntil: daysUntil,
            nextOpponentName: oppName,
            isHomeMatch: isHome,
            isSelectionApproved: board.IsApproved,
            hasTrainingPlan: training.HasPlan,
            hasTacticPlan: hasTactic,
            averageSquadFitness: avgFitness,
            fatiguedPlayerCount: fatiguedCount,
            pendingDecisionCount: decisions.OpenCount,
            pendingDecisionSummary: decisionSummary,
            openTransferNeedCount: openNeeds,
            facilityLevel: facilityLevel,
            cardsUsedTodayCount: _cardsUsedTodayCount);
    }

    public UiActionResult PlayTactileManagerCard(string cardCode)
    {
        var day = GetTimeline().CurrentDate;
        if (_lastCardUseDayNumber != day.DayNumber)
        {
            _lastCardUseDayNumber = day.DayNumber;
            _cardsUsedTodayCount = 0;
        }

        _cardsUsedTodayCount++;

        switch (cardCode)
        {
            case MobilePortalAndDeckDigest.CardEnergyPack:
            {
                var res = ApplyLightRegeneration();
                return UiActionResult.Ok(
                    $"⚡ Enerji Paketi Kartı oynandı! Kadro kondisyonu tazelendi ve günlük kart görevi ilerledi. ({res.Message})");
            }
            case MobilePortalAndDeckDigest.CardPepTalk:
            {
                var decisions = GetPendingDecisions(take: 1);
                if (decisions.OpenCount > 0)
                {
                    var ans = AnswerOldestPendingDecision(grantPromise: true);
                    return UiActionResult.Ok(
                        $"🔥 Motivasyon Konuşması Kartı oynandı! Bekleyen oyuncu talebine olumlu yanıt verildi ({ans.Message}).");
                }

                return UiActionResult.Ok(
                    "🔥 Motivasyon Konuşması Kartı oynandı! Soyunma odası uyumu ve maç odak seviyesi yükseldi (+50 Menajer XP).");
            }
            case MobilePortalAndDeckDigest.CardPhysioSpa:
            {
                var res = RestEntireTeam();
                return UiActionResult.Ok(
                    $"🩺 Fizyo & Spa Kartı oynandı! Yorgun oyuncular korumaya alındı, sakatlık riski düşürüldü. ({res.Message})");
            }
            case MobilePortalAndDeckDigest.CardScoutPack:
            {
                var needRes = RefreshTransferNeedSuggestions();
                return UiActionResult.Ok(
                    $"🔭 Elit Scout Dosyası Kartı oynandı! Kadro ihtiyaçları tarandı ve yeni adaylar hazırlandı. ({needRes.Message})");
            }
            default:
                return UiActionResult.Ok("🃏 Menajer kartı uygulandı.");
        }
    }
}
