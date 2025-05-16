namespace PolyChessTGBot.Database
{
    internal class Attendance
    {
        public User User;

        public Lesson Lesson;

        public Attendance(User user, Lesson lesson)
        {
            User = user;
            Lesson = lesson;
        }
    }
}
