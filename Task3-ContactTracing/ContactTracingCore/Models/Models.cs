namespace ContactTracingCore.Models
{
    // Matches ContactTracerGui.Models.PersonPosition exactly.
    // Published to the 'position' topic by PersonApp.
    // Consumed by TrackerApp and the GUI's PositionListenerService.
    public class PersonPosition
    {
        public string Name      { get; set; } = string.Empty;
        public int    X         { get; set; }
        public int    Y         { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // Matches ContactTracerGui.Models.ContactEvent exactly.
    // Recorded internally by TrackerApp when two people share a square.
    public class ContactEvent
    {
        public string Person1   { get; set; } = string.Empty;
        public string Person2   { get; set; } = string.Empty;
        public int    X         { get; set; }
        public int    Y         { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // Matches ContactTracerGui.Models.QueryResponse exactly.
    // Published to the 'query-response' topic by TrackerApp
    // in response to a name appearing on the 'query' topic.
    public class QueryResponse
    {
        public string QueryName           { get; set; } = string.Empty;
        public List<ContactEvent> Contacts { get; set; } = new();
    }

    // Published to the 'query' topic by QueryApp.
    // TrackerApp subscribes and responds with a QueryResponse.
    public class QueryRequest
    {
        public string Name { get; set; } = string.Empty;
    }
}
