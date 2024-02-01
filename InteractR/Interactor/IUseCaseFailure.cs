using System;

namespace InteractR.Interactor;

public interface IUseCaseFailure
{
    string Code { get; }
    string Details { get; }
    public string Message { get; }
    Exception CausingException { get; }
}