using System;

namespace InteractR.Interactor
{
    public class UseCaseFailure : IUseCaseFailure
    {
        public string Code { get; }
        public string Details { get; }
        public string Message { get; }
        public Exception CausingException { get; }

        public UseCaseFailure(string code, string details, string message = "", Exception causingException = null)
        {
            Code = code;
            Details = details;
            Message = message;
            CausingException = causingException;
        }
    }
}
