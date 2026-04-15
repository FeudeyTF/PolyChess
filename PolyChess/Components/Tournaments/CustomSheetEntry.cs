using PolyChess.Components.Data.Tables;

namespace PolyChess.Components.Tournaments
{
	internal class CustomSheetEntry
	{
        public int Score;

        public Student Student;
	
		public CustomSheetEntry(int score, Student student)
		{
			Score = score;
			Student = student;
		}
	}
}
