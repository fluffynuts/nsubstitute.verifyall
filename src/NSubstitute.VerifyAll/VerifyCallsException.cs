using System;

namespace NSubstitute.VerifyAll
{
    /// <summary>
    /// Thrown when call verification fails
    /// </summary>
    /// <param name="message"></param>
    public class VerifyCallsException(string message)
        : Exception(message)
    {
    }
}