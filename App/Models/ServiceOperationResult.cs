namespace StaadSteelApp.Core
{
    public enum ServiceOperationResultKind
    {
        Success,
        Info,
        Warning,
        Error
    }

    public sealed class ServiceOperationResult
    {
        public bool Succeeded { get; set; }
        public ServiceOperationResultKind Kind { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }

        public static ServiceOperationResult Success(string title = "", string message = "")
        {
            return Create(true, ServiceOperationResultKind.Success, title, message);
        }

        public static ServiceOperationResult Info(string title, string message)
        {
            return Create(false, ServiceOperationResultKind.Info, title, message);
        }

        public static ServiceOperationResult Warning(string title, string message)
        {
            return Create(false, ServiceOperationResultKind.Warning, title, message);
        }

        public static ServiceOperationResult Error(string title, string message)
        {
            return Create(false, ServiceOperationResultKind.Error, title, message);
        }

        private static ServiceOperationResult Create(bool succeeded, ServiceOperationResultKind kind, string title, string message)
        {
            return new ServiceOperationResult
            {
                Succeeded = succeeded,
                Kind = kind,
                Title = title,
                Message = message
            };
        }
    }
}
