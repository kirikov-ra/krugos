using System;

namespace Krugos.Domain
{
    public sealed class PuzzleValidationException : Exception
    {
        public PuzzleValidationException(string message) : base(message)
        {
        }
    }
}
