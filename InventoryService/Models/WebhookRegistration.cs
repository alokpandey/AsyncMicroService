namespace InventoryService.Models
{
    public class WebhookRegistration
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CallbackUrl { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    }
}
