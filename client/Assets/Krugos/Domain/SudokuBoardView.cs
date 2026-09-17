using System.Collections.Generic;

namespace Krugos.Domain
{
    // A live read-only view; the mutable board cannot be obtained through this API.
    public sealed class SudokuBoardView
    {
        private readonly SudokuBoard board;

        internal SudokuBoardView(SudokuBoard board)
        {
            this.board = board;
        }

        public SudokuPuzzle Puzzle => board.Puzzle;
        public bool IsComplete => board.IsComplete;
        public SudokuValue? GetValue(CellPosition position) => board.GetValue(position);
        public bool IsGiven(CellPosition position) => board.IsGiven(position);
        public SudokuConflict GetConflicts(CellPosition position) => board.GetConflicts(position);
        public SudokuConflict GetConflicts(CellPosition position, SudokuValue value) => board.GetConflicts(position, value);
        public bool IsPlacementValid(CellPosition position, SudokuValue value) => board.IsPlacementValid(position, value);
        public IReadOnlyList<SudokuValue> GetCandidates(CellPosition position) => board.GetCandidates(position);
    }
}
