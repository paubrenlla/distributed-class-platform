using System.Collections.Concurrent;
using Common.DTOs;

namespace LogsConsumer;

public class LogStorageService
{
    private static readonly LogStorageService _instance = new();
    private readonly ConcurrentBag<LogMessageDTO> _logs = new();

    public static LogStorageService Instance => _instance;
    private LogStorageService() { }

    public void AddLog(LogMessageDTO log)
    {
        _logs.Add(log);
    }

    // SLRF2: Lógica de filtrado
    public IEnumerable<LogMessageDTO> GetLogs(string? username, string? level, string? action)
    {
        IEnumerable<LogMessageDTO> query = _logs;

        if (!string.IsNullOrEmpty(username))
        {
            query = query.Where(l => l.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrEmpty(level))
        {
            query = query.Where(l => l.Level.Equals(level, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrEmpty(action))
        {
            query = query.Where(l => l.Action.Equals(action, StringComparison.OrdinalIgnoreCase));
        }
            
        return query.OrderByDescending(l => l.Timestamp).ToList();
    }
}