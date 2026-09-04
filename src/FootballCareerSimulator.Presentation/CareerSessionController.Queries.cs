using FootballCareerSimulator.Application.ClubGovernance.Queries;
using FootballCareerSimulator.Application.ClubGovernance.Services;
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

    /// <summary>Güncel puan durumunu döner.</summary>
    public IReadOnlyList<StandingEntryReadModel> GetStandings(long seasonId) =>
        Host.CompetitionModule.Queries.GetStandings(seasonId) ?? Array.Empty<StandingEntryReadModel>();
}
