using System;

namespace InteractR.Exceptions;

public class UseCaseNullException : ArgumentException
{
    public UseCaseNullException()
    {
            
    }

    public UseCaseNullException(string message) : base(message)
    {
            
    }

    public UseCaseNullException(string message, Exception innerException) : base(message, innerException)
    {
            
    }
}