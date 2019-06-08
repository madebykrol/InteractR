using UseCaseMediator.Interactor;

namespace UseCaseMediator.Flow
{
    public interface IFlowController<in TRequest>
    {
        int Weight { get; set; }
        bool Intercept(TRequest request);
    }
}

