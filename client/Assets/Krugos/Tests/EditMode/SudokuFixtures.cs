using System;

namespace Krugos.Tests.EditMode
{
    internal static class SudokuFixtures
    {
        internal const string ClassicPuzzle =
            "530070000" +
            "600195000" +
            "098000060" +
            "800060003" +
            "400803001" +
            "700020006" +
            "060000280" +
            "000419005" +
            "000080079";

        internal const string ClassicSolution =
            "534678912" +
            "672195348" +
            "198342567" +
            "859761423" +
            "426853791" +
            "713924856" +
            "961537284" +
            "287419635" +
            "345286179";

        internal const string SecondPuzzle =
            "003020600" +
            "900305001" +
            "001806400" +
            "008102900" +
            "700000008" +
            "006708200" +
            "002609500" +
            "800203009" +
            "005010300";

        internal const string SecondSolution =
            "483921657" +
            "967345821" +
            "251876493" +
            "548132976" +
            "729564138" +
            "136798245" +
            "372689514" +
            "814253769" +
            "695417382";

        internal const string HardPuzzle =
            "100007090" +
            "030020008" +
            "009600500" +
            "005300900" +
            "010080002" +
            "600004000" +
            "300000010" +
            "040000007" +
            "007000300";

        internal const string SeventeenGivenPuzzle =
            "000000010" +
            "400000000" +
            "020000000" +
            "000050407" +
            "008000300" +
            "001090000" +
            "300400200" +
            "050100000" +
            "000806000";

        // Removing this 6/7 rectangle from ClassicSolution leaves exactly two completions.
        internal const string TwoSolutionPuzzle =
            "534008912" +
            "672195348" +
            "198342567" +
            "859001423" +
            "426853791" +
            "713924856" +
            "961537284" +
            "287419635" +
            "345286179";

        internal const string ImmediateUnsolvablePuzzle =
            "123456780" + "000000009" + "000000000" +
            "000000000" + "000000000" + "000000000" +
            "000000000" + "000000000" + "000000000";

        // The locally legal 1 at (0, 2) contradicts the unique ClassicSolution.
        internal const string UnsolvablePuzzle =
            "531070000" + "600195000" + "098000060" +
            "800060003" + "400803001" + "700020006" +
            "060000280" + "000419005" + "000080079";

        internal static int?[] Parse(string text)
        {
            if (text.Length != 81)
                throw new ArgumentException("A test fixture must contain 81 digits.", nameof(text));

            var cells = new int?[81];
            for (var index = 0; index < cells.Length; index++)
            {
                if (text[index] < '0' || text[index] > '9')
                    throw new ArgumentException("A test fixture must contain only digits.", nameof(text));

                cells[index] = text[index] == '0' ? (int?)null : text[index] - '0';
            }

            return cells;
        }
    }
}
