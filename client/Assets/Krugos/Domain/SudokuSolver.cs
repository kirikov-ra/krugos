using System;

namespace Krugos.Domain
{
    public static class SudokuSolver
    {
        // The first solution in deterministic MRV search order is canonical.
        public static bool TrySolve(SudokuPuzzle puzzle, out SudokuSolution solution)
        {
            var search = new Search(puzzle, 1, true);
            search.Run();
            solution = search.FirstSolution;
            return solution != null;
        }

        public static bool HasSolution(SudokuPuzzle puzzle) => CountSolutions(puzzle, 1) != 0;

        public static bool HasUniqueSolution(SudokuPuzzle puzzle) => CountSolutions(puzzle, 2) == 1;

        // Returns min(actual solution count, maxCount), not an exhaustive count at the limit.
        public static int CountSolutions(SudokuPuzzle puzzle, int maxCount)
        {
            if (maxCount < 1)
                throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, "The solution limit must be positive.");

            var search = new Search(puzzle, maxCount, false);
            search.Run();
            return search.Count;
        }

        private sealed class Search
        {
            private const int AllValues = (1 << 9) - 1;
            private readonly int[] cells = new int[81];
            private readonly int[] rows = new int[9];
            private readonly int[] columns = new int[9];
            private readonly int[] boxes = new int[9];
            private readonly int maxCount;
            private readonly bool captureSolution;

            internal int Count { get; private set; }
            internal SudokuSolution FirstSolution { get; private set; }

            internal Search(SudokuPuzzle puzzle, int maxCount, bool captureSolution)
            {
                if (puzzle == null)
                    throw new ArgumentNullException(nameof(puzzle));

                this.maxCount = maxCount;
                this.captureSolution = captureSolution;
                for (var index = 0; index < cells.Length; index++)
                {
                    var row = index / 9;
                    var column = index % 9;
                    var given = puzzle.GetGiven(new CellPosition(row, column));
                    if (!given.HasValue)
                        continue;

                    var value = given.Value.Value;
                    cells[index] = value;
                    var mask = 1 << (value - 1);
                    rows[row] |= mask;
                    columns[column] |= mask;
                    boxes[row / 3 * 3 + column / 3] |= mask;
                }
            }

            internal void Run()
            {
                var nextIndex = -1;
                var nextCandidates = 0;
                var fewestCandidates = 10;
                for (var index = 0; index < cells.Length; index++)
                {
                    if (cells[index] != 0)
                        continue;

                    var row = index / 9;
                    var column = index % 9;
                    var candidates = AllValues & ~(rows[row] | columns[column] | boxes[row / 3 * 3 + column / 3]);
                    if (candidates == 0)
                        return;

                    var candidateCount = CountBits(candidates);
                    // Strict comparison preserves row-major order when MRV counts tie.
                    if (candidateCount < fewestCandidates)
                    {
                        nextIndex = index;
                        nextCandidates = candidates;
                        fewestCandidates = candidateCount;
                    }
                }

                if (nextIndex == -1)
                {
                    Count++;
                    if (captureSolution && FirstSolution == null)
                        FirstSolution = new SudokuSolution(cells);
                    return;
                }

                var nextRow = nextIndex / 9;
                var nextColumn = nextIndex % 9;
                var nextBox = nextRow / 3 * 3 + nextColumn / 3;
                for (var value = 1; value <= 9; value++)
                {
                    var mask = 1 << (value - 1);
                    if ((nextCandidates & mask) == 0)
                        continue;

                    cells[nextIndex] = value;
                    rows[nextRow] |= mask;
                    columns[nextColumn] |= mask;
                    boxes[nextBox] |= mask;
                    Run();

                    // Restore every branch before returning, including early success.
                    cells[nextIndex] = 0;
                    rows[nextRow] &= ~mask;
                    columns[nextColumn] &= ~mask;
                    boxes[nextBox] &= ~mask;
                    if (Count >= maxCount)
                        return;
                }
            }

            private static int CountBits(int mask)
            {
                var count = 0;
                while (mask != 0)
                {
                    mask &= mask - 1;
                    count++;
                }

                return count;
            }
        }
    }
}
