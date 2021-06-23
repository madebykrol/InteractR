using System.Collections.Generic;

namespace InteractR.Interactor
{
    public class UseCaseResult
    {
        public bool Success { get; }
        public IReadOnlyList<IUseCaseFailure> Failures { get; }

        public UseCaseResult(bool success, List<IUseCaseFailure> failures = default)
        {
            Success = success;
            Failures = failures;
        }
    }
}
