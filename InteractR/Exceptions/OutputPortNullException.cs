﻿using System;

namespace InteractR.Exceptions;

public class OutputPortNullException : ArgumentException
{
    public OutputPortNullException()
    {
            
    }

    public OutputPortNullException(string message) : base(message)
    {
            
    }

    public OutputPortNullException(string message, Exception innerException) : base(message, innerException)
    {
            
    }
}