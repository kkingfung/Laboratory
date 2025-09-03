namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Required for init-only setters in C# 9.0+ when targeting older .NET versions.
    /// This is a compatibility shim for Unity projects.
    /// </summary>
    internal static class IsExternalInit 
    { 
        // No implementation required - this is a marker type for the compiler
    }
}
