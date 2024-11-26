namespace FileFlows.Shared.Exceptions;

/// <summary>
/// Exception for protected paths
/// </summary>
/// <param name="message">The message</param>
public class ProtectedPathException(string message) : Exception(message)
{
    
    
}