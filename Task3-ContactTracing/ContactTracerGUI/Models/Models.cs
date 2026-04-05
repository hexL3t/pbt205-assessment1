namespace ContactTracerGui.Models
{
    public class PersonPosition
    {
        public string Name { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ContactEvent
    {
        public string Person1 { get; set; } = string.Empty;
        public string Person2 { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class QueryResponse
    {
        public string QueryName { get; set; } = string.Empty;
        public List<ContactEvent> Contacts { get; set; } = new();
    }
}
