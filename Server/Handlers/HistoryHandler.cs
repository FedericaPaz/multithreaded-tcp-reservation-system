using Common.Models;
using Common.Protocol;
using Server.Repositories;
using static Common.Protocol.StatusCode;

namespace Server.Handlers;

public static class HistoryHandler
{
    public static Frame GetHistory(Frame request)
    {
        FieldReader reader = new FieldReader(request.Data);
        string username = reader.ReadText();
        
        List<HistoryEvent> events = HistoryRepository.Instance.GetByUsername(username);
        
        FieldWriter writer = new FieldWriter().WriteInt((int)Ok).WriteInt(events.Count);

        foreach (HistoryEvent e in events)
        {
            writer.WriteString(e.Type.ToString()).WriteString(e.Date.ToString("yyyy-MM-dd HH:mm:ss"))
                .WriteString(e.Detail);
        }

        return new Frame { Command = request.Command, Data = writer.ToArray() };
    }
}