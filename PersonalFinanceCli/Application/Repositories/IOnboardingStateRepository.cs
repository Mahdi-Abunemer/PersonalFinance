namespace PersonalFinanceCli.Application.Repositories;

public interface IOnboardingStateRepository
{
    DateOnly? GetLastCushionDeclinedDate();

    void SetLastCushionDeclinedDate(DateOnly? date);

    bool HasSeenOnboarding();

    void SetHasSeenOnboarding(bool value);
}
