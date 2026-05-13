namespace StaadSteelApp.Core
{
    public sealed class SectionAssignmentResult : IOperationResult
    {
        public bool Succeeded { get; set; }
        public ServiceOperationResultKind Kind { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public int PropertyId { get; set; }
        public int BeamCount { get; set; }

        public static SectionAssignmentResult Success(string title, string message, int propertyId, int beamCount)
        {
            return new SectionAssignmentResult
            {
                Succeeded = true,
                Kind = ServiceOperationResultKind.Success,
                Title = title,
                Message = message,
                PropertyId = propertyId,
                BeamCount = beamCount
            };
        }

        public static SectionAssignmentResult Warning(string title, string message)
        {
            return Create(false, ServiceOperationResultKind.Warning, title, message);
        }

        public static SectionAssignmentResult SuccessWarning(string title, string message, int propertyId, int beamCount)
        {
            return new SectionAssignmentResult
            {
                Succeeded = true,
                Kind = ServiceOperationResultKind.Warning,
                Title = title,
                Message = message,
                PropertyId = propertyId,
                BeamCount = beamCount
            };
        }

        public static SectionAssignmentResult Error(string title, string message)
        {
            return Create(false, ServiceOperationResultKind.Error, title, message);
        }

        private static SectionAssignmentResult Create(bool succeeded, ServiceOperationResultKind kind, string title, string message)
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
