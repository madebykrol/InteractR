namespace InteractorHub.Flow
{
    public interface IFlowController<in TRequest>
    {
        int Weight { get; set; }
        bool Intercept(TRequest request);
    }
}

