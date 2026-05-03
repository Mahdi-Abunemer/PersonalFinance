using PersonalFinanceCli.Application.Repositories;

namespace PersonalFinanceCli.Infrastructure.Persistence;

public sealed class JsonOnboardingStateRepository : IOnboardingStateRepository
{
    private readonly JsonDataStore _store;

    public JsonOnboardingStateRepository(JsonDataStore store)
    {
        _store = store;
    }

    public DateOnly? GetLastCushionDeclinedDate()
    {
        return _store.Load().LastCushionDeclinedDate;
    }

    public void SetLastCushionDeclinedDate(DateOnly? date)
    {
        var dataStore = _store.Load();
        dataStore.LastCushionDeclinedDate = date;
        _store.Save(dataStore);
    }

    public bool HasSeenOnboarding()
    {
        return _store.Load().HasSeenOnboarding;
    }

    public void SetHasSeenOnboarding(bool hasSeenOnboarding)
    {
        var dataStore = _store.Load();
        dataStore.HasSeenOnboarding = hasSeenOnboarding;
        _store.Save(dataStore);
    }
}
