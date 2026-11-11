using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.ClubGovernance;

public enum ClubLedgerCategory
{
    SponsorRevenue = 1,
    MatchdayTicketRevenue = 2,
    WageExpense = 3,
    FootballOperationsExpense = 4,
}

public readonly record struct ClubLedgerEntryId
{
    public string Value { get; }

    public ClubLedgerEntryId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Ledger entry id is required.", nameof(value));
        }

        Value = value.Trim();
    }

    public override string ToString() => Value;
}

/// <summary>
/// Gerçekleşmiş tek muhasebe hareketi. Tutar gelirde pozitif, giderde negatiftir.
/// </summary>
public sealed record ClubLedgerEntry
{
    private ClubLedgerEntry(
        ClubLedgerEntryId id,
        ClubId clubId,
        GameDate occurredOn,
        ClubLedgerCategory category,
        long signedAmount,
        string reference)
    {
        Id = id;
        ClubId = clubId;
        OccurredOn = occurredOn;
        Category = category;
        SignedAmount = signedAmount;
        Reference = reference;
    }

    public ClubLedgerEntryId Id { get; }

    public ClubId ClubId { get; }

    public GameDate OccurredOn { get; }

    public ClubLedgerCategory Category { get; }

    public long SignedAmount { get; }

    public string Reference { get; }

    public static ClubLedgerEntry Create(
        ClubLedgerEntryId id,
        ClubId clubId,
        GameDate occurredOn,
        ClubLedgerCategory category,
        long signedAmount,
        string reference)
    {
        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(nameof(category));
        }

        if (signedAmount == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(signedAmount), "Ledger amount cannot be zero.");
        }

        var isRevenue = category is ClubLedgerCategory.SponsorRevenue
            or ClubLedgerCategory.MatchdayTicketRevenue;
        if (isRevenue != signedAmount > 0)
        {
            throw new ArgumentException("Revenue must be positive and expenses must be negative.", nameof(signedAmount));
        }

        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new ArgumentException("Ledger reference is required.", nameof(reference));
        }

        return new ClubLedgerEntry(id, clubId, occurredOn, category, signedAmount, reference.Trim());
    }
}

/// <summary>
/// Kulübün gerçekleşmiş finansal hareketlerini tutan değişmez aggregate.
/// Aynı hareket kimliğinin yeniden eklenmesi değişiklik üretmez.
/// </summary>
public sealed class ClubFinanceLedger
{
    private readonly IReadOnlyList<ClubLedgerEntry> _entries;
    private readonly IReadOnlySet<ClubLedgerEntryId> _entryIds;

    private ClubFinanceLedger(
        ClubId clubId,
        long openingBalance,
        IReadOnlyList<ClubLedgerEntry> entries)
    {
        ClubId = clubId;
        OpeningBalance = openingBalance;
        _entries = entries;
        _entryIds = entries.Select(entry => entry.Id).ToHashSet();
    }

    public ClubId ClubId { get; }

    public long OpeningBalance { get; }

    public IReadOnlyList<ClubLedgerEntry> Entries => _entries;

    public long Revenue => _entries.Where(entry => entry.SignedAmount > 0).Sum(entry => entry.SignedAmount);

    public long Expenses => -_entries.Where(entry => entry.SignedAmount < 0).Sum(entry => entry.SignedAmount);

    public long OperatingResult => Revenue - Expenses;

    public long Balance => checked(OpeningBalance + OperatingResult);

    public static ClubFinanceLedger Open(ClubId clubId, long openingBalance = 0) =>
        new(clubId, openingBalance, []);

    public static ClubFinanceLedger Restore(
        ClubId clubId,
        long openingBalance,
        IReadOnlyList<ClubLedgerEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Any(entry => entry.ClubId != clubId))
        {
            throw new ArgumentException("All ledger entries must belong to the ledger club.", nameof(entries));
        }

        if (entries.Select(entry => entry.Id).Distinct().Count() != entries.Count)
        {
            throw new ArgumentException("Ledger entry ids must be unique.", nameof(entries));
        }

        return new ClubFinanceLedger(
            clubId,
            openingBalance,
            entries.OrderBy(entry => entry.OccurredOn.DayNumber).ThenBy(entry => entry.Id.Value).ToArray());
    }

    public bool Contains(ClubLedgerEntryId entryId) => _entryIds.Contains(entryId);

    public ClubFinanceLedger Post(ClubLedgerEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.ClubId != ClubId)
        {
            throw new ArgumentException("Ledger entry belongs to another club.", nameof(entry));
        }

        if (Contains(entry.Id))
        {
            return this;
        }

        return Restore(ClubId, OpeningBalance, [.. _entries, entry]);
    }
}
