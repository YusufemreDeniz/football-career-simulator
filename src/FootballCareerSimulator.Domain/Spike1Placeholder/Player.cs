namespace FootballCareerSimulator.Domain.Spike1Placeholder;

/// <summary>
/// Spike 1 için oluşturulmuş yer tutucu futbolcu temsili. Yalnızca dünya ölçeğini, sezon başına
/// yapılan minimal işi (yaş artışı) ve Spike 2'nin RNG akışını tüketen bir alanı (Form) simüle etmek
/// için gereken en az alanı taşır; kesin futbolcu domain modeli `docs/03_DOMAIN_MODEL.md` ve
/// `docs/11_PLAYER_CAREER.md` kapsamında ayrıca tasarlanacaktır. Gerçek yaşlanma/form/gelişim/emeklilik
/// formülleri burada uygulanmaz; "Form" tamamen kurgusal, sınırlı bir tam sayıdır.
/// </summary>
public sealed class Player
{
    private const int MinForm = -10;
    private const int MaxForm = 10;

    public PlayerId Id { get; }

    public ClubId ClubId { get; }

    public int Age { get; private set; }

    public int Form { get; private set; }

    public Player(PlayerId id, ClubId clubId, int age)
        : this(id, clubId, age, form: 0)
    {
    }

    private Player(PlayerId id, ClubId clubId, int age, int form)
    {
        if (age < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(age), age, "Age cannot be negative.");
        }

        if (form < MinForm || form > MaxForm)
        {
            throw new ArgumentOutOfRangeException(nameof(form), form, $"Form must be between {MinForm} and {MaxForm}.");
        }

        Id = id;
        ClubId = clubId;
        Age = age;
        Form = form;
    }

    /// <summary>
    /// Save/load round-trip senaryolarında (bkz. Spike 2), daha önce persist edilmiş bir state'ten
    /// açık bir rehydration sözleşmesiyle yeniden kurar; normal oyun akışında kullanılmaz
    /// (`docs/15_DECISION_LOG.md` D-289 ile uyumlu).
    /// </summary>
    public static Player Rehydrate(PlayerId id, ClubId clubId, int age, int form) => new(id, clubId, age, form);

    public void AgeOneYear() => Age++;

    public void AdjustForm(int delta) => Form = Math.Clamp(Form + delta, MinForm, MaxForm);
}
