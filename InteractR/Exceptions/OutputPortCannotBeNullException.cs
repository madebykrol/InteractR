using System;
using System.Collections.Generic;
using System.Text;

namespace InteractR.Exceptions
{
    public class OutputPortCannotBeNullException : ArgumentException
    {
        public OutputPortCannotBeNullException(string message) : base(message)
        {
            
        }

        public OutputPortCannotBeNullException(string message, Exception innerException) : base(message, innerException)
        {
            
        }
    }
}
