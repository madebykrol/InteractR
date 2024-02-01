using System.Collections.Generic;

namespace InteractR.Interactor;

public class UseCaseResult(bool success, List<IUseCaseFailure> failures = default)
{
    public bool Success { get; } = success;
    public IReadOnlyList<IUseCaseFailure> Failures { get; } = failures;
}