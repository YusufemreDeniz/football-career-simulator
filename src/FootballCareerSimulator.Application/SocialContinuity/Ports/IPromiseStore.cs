using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Application.SocialContinuity.Ports;

public interface IPromiseStore
{
    IReadOnlyList<Promise> Promises { get; }

    Promise? Get(PromiseId promiseId);

    void Upsert(Promise promise);

    void ReplaceAll(IEnumerable<Promise> promises);
}
