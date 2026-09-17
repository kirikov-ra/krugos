using System;
using System.Collections.Generic;

namespace Krugos.Domain
{
    public sealed class SudokuBoard
    {
        private readonly SudokuValue?[] cells = new SudokuValue?[81];

        public SudokuPuzzle Puzzle { get; }

        public SudokuBoard(SudokuPuzzle puzzle)
        {
            Puzzle = puzzle ?? throw new ArgumentNullException(nameof(puzzle));
            for (var index = 0; index < cells.Length; index++)
                cells[index] = puzzle.GetGiven(new CellPosition(index / 9, index % 9));
        }

        public SudokuValue? GetValue(CellPosition position) => cells[position.Index];
        public bool IsGiven(CellPosition position) => Puzzle.IsGiven(position);

        public void SetValue(CellPosition position, SudokuValue value)
        {
            EnsureEditable(position);
            cells[position.Index] = value;
        }

        public void ClearValue(CellPosition position)
        {
            EnsureEditable(position);
            cells[position.Index] = null;
        }

        public SudokuConflict GetConflicts(CellPosition position)
        {
            var value = GetValue(position);
            return value.HasValue ? GetConflicts(position, value.Value) : SudokuConflict.None;
        }

        public SudokuConflict GetConflicts(CellPosition position, SudokuValue value)
        {
            var conflicts = SudokuConflict.None;
            for (var index = 0; index < cells.Length; index++)
            {
                if (index == position.Index || cells[index] != value)
                    continue;

                var row = index / 9;
                var column = index % 9;
                if (row == position.Row)
                    conflicts |= SudokuConflict.Row;
                if (column == position.Column)
                    conflicts |= SudokuConflict.Column;
                if (row / 3 == position.Row / 3 && column / 3 == position.Column / 3)
                    conflicts |= SudokuConflict.Box;
            }

            return conflicts;
        }

        public bool IsPlacementValid(CellPosition position, SudokuValue value) =>
            !IsGiven(position) && GetConflicts(position, value) == SudokuConflict.None;

        public IReadOnlyList<SudokuValue> GetCandidates(CellPosition position)
        {
            if (IsGiven(position) || GetValue(position).HasValue)
                return Array.Empty<SudokuValue>();

            var candidates = new List<SudokuValue>();
            for (var number = 1; number <= 9; number++)
            {
                var value = new SudokuValue(number);
                if (IsPlacementValid(position, value))
                    candidates.Add(value);
            }

            return candidates.AsReadOnly();
        }

        public bool IsComplete
        {
            get
            {
                for (var index = 0; index < cells.Length; index++)
                {
                    var position = new CellPosition(index / 9, index % 9);
                    if (!cells[index].HasValue || GetConflicts(position) != SudokuConflict.None)
                        return false;
                }

                return true;
            }
        }

        private void EnsureEditable(CellPosition position)
        {
            if (IsGiven(position))
                throw new InvalidOperationException("Given cells cannot be changed or cleared.");
        }
    }
}
