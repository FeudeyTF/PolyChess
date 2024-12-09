namespace PolyChessTGBot.Database
{
    public class Event(string name, string description, DateTime startDate, DateTime endDate)
    {
        public string Name { get; } = name;

        public string Description { get; } = description;

        public DateTime Start { get; } = startDate;

        public DateTime End { get; } = endDate;
    }
}
