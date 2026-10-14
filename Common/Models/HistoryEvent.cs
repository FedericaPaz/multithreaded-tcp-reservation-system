using System;

namespace Common.Models;

public class HistoryEvent
{
    public string Username { get; set; } = string.Empty;
    public EventType Type { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public string Detail { get; set; } = string.Empty;
}