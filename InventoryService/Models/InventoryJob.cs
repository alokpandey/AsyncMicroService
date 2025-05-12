namespace InventoryService.Models
{
    public class InventoryJob
    {
        public string JobId { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = "Pending";
        public string? Result { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public bool HasError => !string.IsNullOrEmpty(ErrorCode);
    }

    public static class ErrorCodes
    {
        public const string ValidationError = "VALIDATION_ERROR";
        public const string NotFoundError = "NOT_FOUND_ERROR";
        public const string AuthorizationError = "AUTHORIZATION_ERROR";
        public const string DatabaseError = "DATABASE_ERROR";
        public const string ProcessingError = "PROCESSING_ERROR";
    }
}
