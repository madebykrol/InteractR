using System;
using System.Collections.Generic;
using System.Text;

namespace InteractR.Exceptions
{
    public class UseCaseCannotBeNullException : ArgumentException
    {
        public UseCaseCannotBeNullException(string message) : base(message)
        {
            
        }

        public UseCaseCannotBeNullException(string message, Exception innerException) : base(message, innerException)
        {
            
        }
    }
}
