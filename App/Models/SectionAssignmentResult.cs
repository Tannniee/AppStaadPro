namespace StaadSteelApp.Core
{
    public enum SectionAssignmentResultKind
    {
        Success,
        Info,
        Warning,
        Error
    }

    public sealed class SectionAssignmentResult
    {
        public bool Succeeded { get; set; }
        public SectionAssignmentResultKind Kind { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public int PropertyId { get; set; }
        public int BeamCount { get; set; }

        public static SectionAssignmentResult Success(string title, string message, int propertyId, int beamCount)
        {
            return new SectionAssignmentResult
            {
                Succeeded = true,
                Kind = SectionAssignmentResultKind.Success,
                Title = title,
                Message = message,
                PropertyId = propertyId,
                BeamCount = beamCount
            };
        }

        public static SectionAssignmentResult Warning(string title, string message)
        {
            return Create(false, SectionAssignmentResultKind.Warning, title, message);
        }

        public static SectionAssignmentResult Error(string title, string message)
        {
            return Create(false, SectionAssignmentResultKind.Error, title, message);
        }

        private static SectionAssignmentResult Create(bool succeeded, SectionAssignmentResultKind kind, string title, string message)
        {
            return new SectionAssignmentResult
            {
                Succeeded = succeeded,
                Kind = kind,
                Title = title,
                Message = message
            };
        }
    }
}
