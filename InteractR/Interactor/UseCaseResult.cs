namespace InteractR.Interactor
{
    public class UseCaseResult
    {
        public bool Success { get; }

        public UseCaseResult(bool success)
        {
            Success = success;
        }
    }
}
