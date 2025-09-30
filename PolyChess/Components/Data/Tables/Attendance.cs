namespace PolyChess.Components.Data.Tables
{
    internal class Attendance
    {
        public int Id { get; set; }

        public Student Student { get; set; } = null!;

        public Lesson Lesson { get; set; } = null!;
    }
}
