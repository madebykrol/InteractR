namespace UseCaseMediator.Flow
{
    public interface IValidator<TCommand>
    {
        int Weight { get; set; }
        bool Validate(TCommand command);
    }
}
