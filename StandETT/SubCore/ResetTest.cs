using System;

namespace StandETT.SubCore;

public class ResetTestException : Exception
{
    public ResetTestException(string message)
        : base(message)
    { }
}