using PolyChess.Components.Data;
using PolyChess.Components.Data.Tables;

namespace PolyChess.Components
{
	internal class PuzzlesComponent : IComponent
	{
		public Puzzle? CurrentPuzzle { get; private set; }

		public Dictionary<int, bool> StudentAnswers { get; private set; }

		private readonly PolyContext _context;

		public PuzzlesComponent(PolyContext context)
		{
			_context = context;
			StudentAnswers = [];
		}

		public async Task StartAsync()
		{
		}

		public void StopCurrentPuzzle()
		{
			CurrentPuzzle = null;
			StudentAnswers.Clear();
		}

		public Puzzle? SetCurrentPuzzle(string name)
		{
			foreach (var puzzle in _context.Puzzles)
			{
				if (puzzle.Name == name)
				{
					CurrentPuzzle = puzzle;
					StudentAnswers.Clear();
					return puzzle;
				}
			}
			return default;
		}

		public async Task AddPuzzle(Puzzle puzzle)
		{
			_context.Puzzles.Add(puzzle);
			await _context.SaveChangesAsync();
		}

		public async Task DisposeAsync()
		{
			StudentAnswers.Clear();
		}
	}
}
