namespace Krugos.Domain
{
    public sealed class SudokuPuzzle
    {
        private readonly SudokuValue?[] givens;

        private SudokuPuzzle(SudokuValue?[] givens)
        {
            this.givens = givens;
        }

        public static SudokuPuzzle Create(int?[] cells)
        {
            if (cells == null || cells.Length != 81)
                throw new PuzzleValidationException("A puzzle must contain exactly 81 cells in row-major order.");

            var givens = new SudokuValue?[81];
            var rows = new int[9];
            var columns = new int[9];
            var boxes = new int[9];

            for (var index = 0; index < cells.Length; index++)
            {
                var value = cells[index];
                if (!value.HasValue)
                    continue;
                if (value.Value < 1 || value.Value > 9)
                    throw new PuzzleValidationException($"Cell {index} must be empty (null) or contain a value between 1 and 9.");

                var row = index / 9;
                var column = index % 9;
                var box = row / 3 * 3 + column / 3;
                var mask = 1 << (value.Value - 1);
                if (((rows[row] | columns[column] | boxes[box]) & mask) != 0)
                    throw new PuzzleValidationException($"Given at row {row}, column {column} conflicts with another given.");

                rows[row] |= mask;
                columns[column] |= mask;
                boxes[box] |= mask;
                givens[index] = new SudokuValue(value.Value);
            }

            return new SudokuPuzzle(givens);
        }

        public SudokuValue? GetGiven(CellPosition position) => givens[position.Index];
        public bool IsGiven(CellPosition position) => GetGiven(position).HasValue;
    }
}
