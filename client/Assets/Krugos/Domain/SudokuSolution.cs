namespace Krugos.Domain
{
    public sealed class SudokuSolution
    {
        private readonly SudokuValue[] cells;

        internal SudokuSolution(int[] solvedCells)
        {
            cells = new SudokuValue[81];
            for (var index = 0; index < cells.Length; index++)
                cells[index] = new SudokuValue(solvedCells[index]);
        }

        public SudokuValue GetValue(CellPosition position) => cells[position.Index];
    }
}
