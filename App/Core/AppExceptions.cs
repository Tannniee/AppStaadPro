using System;

namespace StaadSteelApp.Core
{
    public class StaadConnectionException : Exception
    {
        public StaadConnectionException(string message)
            : base(message)
        {
        }

        public StaadConnectionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public class StaadInteropException : Exception
    {
        public StaadInteropException(string message)
            : base(message)
        {
        }

        public StaadInteropException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public class SectionPropertyException : Exception
    {
        public SectionPropertyException(string message)
            : base(message)
        {
        }

        public SectionPropertyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public class SectionDetectionException : Exception
    {
        public SectionDetectionException(string message)
            : base(message)
        {
        }

        public SectionDetectionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
