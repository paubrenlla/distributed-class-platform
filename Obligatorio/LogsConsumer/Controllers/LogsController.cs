using Microsoft.AspNetCore.Mvc;

namespace LogsConsumer.Controllers;

[ApiController]
[Route("api/logs")] 
public class LogsController : ControllerBase
{
    private readonly LogStorageService _logStorage;

    public LogsController(LogStorageService logStorage)
    {
        _logStorage = logStorage;
    }

    [HttpGet]
    public IActionResult GetLogs(
        [FromQuery] string? username, 
        [FromQuery] string? level, 
        [FromQuery] string? action) // SLRF2: Nuestros 3 filtros
    {
        try
        {
            var logs = _logStorage.GetLogs(username, level, action);
            return Ok(logs); 
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }
}