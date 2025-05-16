namespace PolyChessTGBot.Database
{
    internal class Lesson
    {
        public int ID;

        public DateTime LessonDate;

        public Lesson(int id, DateTime lessonDate)
        {
            ID = id;
            LessonDate = lessonDate;
        }
    }
}
