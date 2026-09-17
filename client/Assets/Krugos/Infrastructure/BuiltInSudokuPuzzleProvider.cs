using System;
using Krugos.Application;
using Krugos.Domain;

namespace Krugos.Infrastructure
{
    public sealed class BuiltInSudokuPuzzleProvider : ISudokuPuzzleProvider
    {
        private readonly SudokuPuzzleDefinition[] puzzles;

        public int Count => puzzles.Length;

        public BuiltInSudokuPuzzleProvider()
        {
            // Fixed variants of the internal ClassicPuzzle fixture; see PUZZLE_CONTENT.md.
            puzzles = new[]
            {
                Create("built-in-001",
                    "530070000" +
                    "600195000" +
                    "098000060" +
                    "800060003" +
                    "400803001" +
                    "700020006" +
                    "060000280" +
                    "000419005" +
                    "000080079"),
                Create("built-in-002",
                    "560847000" +
                    "309000600" +
                    "008000000" +
                    "010080040" +
                    "790602018" +
                    "050030090" +
                    "000000200" +
                    "006000807" +
                    "000316059"),
                Create("built-in-003",
                    "600195000" +
                    "530070000" +
                    "098000060" +
                    "800060003" +
                    "400803001" +
                    "700020006" +
                    "060000280" +
                    "000419005" +
                    "000080079"),
                Create("built-in-004",
                    "350070000" +
                    "060195000" +
                    "908000060" +
                    "080060003" +
                    "040803001" +
                    "070020006" +
                    "600000280" +
                    "000419005" +
                    "000080079"),
                Create("built-in-005",
                    "640080000" +
                    "700216000" +
                    "019000070" +
                    "900070004" +
                    "500904002" +
                    "800030007" +
                    "070000390" +
                    "000521006" +
                    "000090081")
            };
        }

        public SudokuPuzzleDefinition GetPuzzle(int index)
        {
            if (index < 0 || index >= puzzles.Length)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Puzzle index must be within the provider's range.");

            return puzzles[index];
        }

        private static SudokuPuzzleDefinition Create(string puzzleId, string givens)
        {
            var cells = new int?[81];
            for (var index = 0; index < cells.Length; index++)
                cells[index] = givens[index] == '0' ? (int?)null : givens[index] - '0';

            return SudokuPuzzleDefinition.Create(puzzleId, SudokuPuzzle.Create(cells));
        }
    }
}
