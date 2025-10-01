using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PolyChess.Components.Data
{
    internal class DateTimeConverter : ValueConverter<DateTime, long>
    {
        public DateTimeConverter() : base(date => date.Ticks, value => new DateTime(value))
        {
        }
    }
}
