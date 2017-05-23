using System;

namespace SlidingTemporaryCache
{
    /// <summary>
    /// Exception being thrown when the value factory throws an exception.
    /// </summary>
    public sealed class LazyInitializationException : Exception
    {
        internal LazyInitializationException(Exception e)
            : base("Error occured while calling factory method for Lazy instance.", e)
        {
        }
    }
}
