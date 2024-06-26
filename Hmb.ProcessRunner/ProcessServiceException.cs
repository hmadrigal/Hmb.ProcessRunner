﻿namespace Hmb.ProcessRunner;

/// <summary>
/// Represents errors that occur during process execution.
/// </summary>
[Serializable]
public class ProcessServiceException : ApplicationException
{
    public ProcessServiceException() { }
    public ProcessServiceException(string message) : base(message) { }
    public ProcessServiceException(string message, Exception inner) : base(message, inner) { }
}