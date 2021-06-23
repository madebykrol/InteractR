namespace InteractR.Interactor
{
    public interface IUseCaseFailure
    {
        string Title { get; }
        string Description { get; }
        int Status { get; }
    }
}
