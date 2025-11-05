using Microsoft.EntityFrameworkCore;

namespace PolyChess.Components.Data.Tables
{
    [PrimaryKey(nameof(Name), nameof(Surname), nameof(Patronymic))]
    internal class Student
    {
        public string Name { get; set; } = null!;

        public string Surname { get; set; } = null!;

        public string Patronymic { get; set; } = null!;

        public string? LichessId { get; set; } = null;

        public long TelegramId { get; set; }

        public long Year { get; set; }

        public string Institute { get; set; } = null!;

        public string Group { get; set; } = null!;

        public string? RecordBookId { get; set; }

        public bool CreativeTaskCompleted { get; set; }

        public string? LichessToken { get; set; }

        public int AdditionalTournamentsScore { get; set; }

        public override string ToString()
        {
            return $"Имя: <b>{Name}</b>, Lichess: <b>{LichessId}</b>, Telegram: <b>{TelegramId}</b>, Курс: <b>{Year}</b>";
        }
    }
}
