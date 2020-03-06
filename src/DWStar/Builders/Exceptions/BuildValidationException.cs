using System;

namespace DWStar.Builders.Exceptions
{
    public class BuildValidationException : Exception
    {
        public BuildValidationException()
        {
        }

        public BuildValidationException(string message) : base(message)
        {
        }

        public BuildValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}