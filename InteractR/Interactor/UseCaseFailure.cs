using System;

namespace InteractR.Interactor;

public class UseCaseFailure(string code, string details, string message = "", Exception causingException = null)
    : IUseCaseFailure
{
    public string Code { get; } = code;
    public string Details { get; } = details;
    public string Message { get; } = message;
    public Exception CausingException { get; } = causingException;
}