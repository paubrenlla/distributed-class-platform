namespace Common.DTOs;

public class LogMessageDTO
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Level { get; set; } // "Info", "Error", "Warning"
    public string Service { get; set; } = "ServerPrincipal";
    public string Username { get; set; }
    public string Action { get; set; } // "SeedData", "Login", "CreateClass", etc.
    public string Message { get; set; }
}