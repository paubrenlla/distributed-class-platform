namespace Common.DTOs
{
    public class SubscribeClassRequestDTO
    {
        public int ClassId { get; set; }
        public string? WebhookUrl { get; set; } // Opcional
    }
}