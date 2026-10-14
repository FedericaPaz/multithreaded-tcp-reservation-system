using Common.Models;

namespace Server.Repositories;

public class HistoryRepository
{
    private static readonly Lazy<HistoryRepository> _instance = new(() => new HistoryRepository());
    public static HistoryRepository Instance => _instance.Value;
    
    private readonly List<HistoryEvent> _events = new();
    private readonly object _lock = new();
    
    private  HistoryRepository() {}

    public void Register(HistoryEvent historyEvent)
    {
        lock (_lock)
            _events.Add(historyEvent);
    }
    
    public List<HistoryEvent> GetByUsername(string username)
    {
        lock (_lock)
        {
            return _events.Where(e => e.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.Date)
                .ToList();
        }
    }
}