using System;
using System.Linq;
using Krugos.Application;
using Krugos.Domain;
using Krugos.Infrastructure;
using NUnit.Framework;

namespace Krugos.Tests.EditMode
{
    public sealed class BuiltInSudokuPuzzleProviderTests
    {
        [Test]
        public void ProviderExposesFiveDistinctPuzzlesWithUniqueStableIds()
        {
            ISudokuPuzzleProvider provider = new BuiltInSudokuPuzzleProvider();
            Assert.That(provider.Count, Is.EqualTo(5));
            var definitions = Enumerable.Range(0, provider.Count).Select(provider.GetPuzzle).ToArray();
            var ids = definitions.Select(definition => definition.PuzzleId).ToArray();

            Assert.That(ids, Is.EqualTo(new[]
            {
                "built-in-001", "built-in-002", "built-in-003", "built-in-004", "built-in-005"
            }));
            Assert.That(ids.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(provider.Count));
            Assert.That(definitions.Select(definition => ReadGivens(definition.Puzzle)).Distinct().Count(),
                Is.EqualTo(provider.Count));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void EveryPuzzleHasExactlyOneMatchingSolutionAndPreservesItsGivens(int index)
        {
            ISudokuPuzzleProvider provider = new BuiltInSudokuPuzzleProvider();
            var definition = provider.GetPuzzle(index);
            var givens = ReadGivens(definition.Puzzle);
            var solution = ReadSolution(definition.Solution);
            Assert.That(SudokuSolver.CountSolutions(definition.Puzzle, 2), Is.EqualTo(1));
            Assert.That(SudokuSolver.TrySolve(definition.Puzzle, out var canonical), Is.True);
            Assert.That(solution, Is.EqualTo(ReadSolution(canonical)));

            var board = new SudokuBoard(definition.Puzzle);
            for (var cell = 0; cell < 81; cell++)
            {
                var position = new CellPosition(cell / 9, cell % 9);
                if (board.IsGiven(position))
                    Assert.That(definition.Solution.GetValue(position), Is.EqualTo(board.GetValue(position).Value));
                else
                    board.SetValue(position, definition.Solution.GetValue(position));
            }

            Assert.That(board.IsComplete, Is.True);
            Assert.That(ReadGivens(definition.Puzzle), Is.EqualTo(givens));
            Assert.That(ReadGivens(provider.GetPuzzle(index).Puzzle), Is.EqualTo(givens));
            Assert.That(ReadSolution(definition.Solution), Is.EqualTo(solution));
            Assert.That(new SudokuBoard(definition.Puzzle).IsComplete, Is.False);
        }

        [Test]
        public void FixedContentMatchesDocumentedInternalFixtureTransformations()
        {
            ISudokuPuzzleProvider provider = new BuiltInSudokuPuzzleProvider();
            var givens = SudokuFixtures.ClassicPuzzle;
            var solution = SudokuFixtures.ClassicSolution;
            for (var index = 0; index < provider.Count; index++)
            {
                Assert.That(ReadGivens(provider.GetPuzzle(index).Puzzle), Is.EqualTo(Transform(givens, index)));
                Assert.That(ReadSolution(provider.GetPuzzle(index).Solution), Is.EqualTo(Transform(solution, index)));
            }
        }

        [Test]
        public void FreshProvidersAndRepeatedRequestsReturnIdenticalContentRegardlessOfRequestOrder()
        {
            ISudokuPuzzleProvider first = new BuiltInSudokuPuzzleProvider();
            ISudokuPuzzleProvider second = new BuiltInSudokuPuzzleProvider();
            var snapshots = Enumerable.Range(0, first.Count).Select(index =>
            {
                var definition = first.GetPuzzle(index);
                return definition.PuzzleId + ":" + ReadGivens(definition.Puzzle) + ":" + ReadSolution(definition.Solution);
            }).ToArray();

            foreach (var index in new[] { 4, 0, 2, 1, 3, 4, 0 })
            foreach (var provider in new[] { first, second })
            {
                var definition = provider.GetPuzzle(index);
                var actual = definition.PuzzleId + ":" + ReadGivens(definition.Puzzle) + ":" + ReadSolution(definition.Solution);
                Assert.That(actual, Is.EqualTo(snapshots[index]));
            }
        }

        [TestCase(-1)]
        [TestCase(5)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void ProviderRejectsIndexOutsideItsRange(int index)
        {
            ISudokuPuzzleProvider provider = new BuiltInSudokuPuzzleProvider();
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.GetPuzzle(index));
        }

        private static string ReadGivens(SudokuPuzzle puzzle) => string.Concat(Enumerable.Range(0, 81)
            .Select(index => (char)('0' + (puzzle.GetGiven(new CellPosition(index / 9, index % 9))?.Value ?? 0))));

        private static string ReadSolution(SudokuSolution solution) => string.Concat(Enumerable.Range(0, 81)
            .Select(index => (char)('0' + solution.GetValue(new CellPosition(index / 9, index % 9)).Value)));

        private static string Transform(string source, int variant)
        {
            var cells = new char[81];
            for (var index = 0; index < cells.Length; index++)
            {
                var row = index / 9;
                var column = index % 9;
                if (variant == 1)
                    cells[index] = source[column * 9 + row];
                else if (variant == 2)
                    cells[index] = source[(row < 2 ? 1 - row : row) * 9 + column];
                else if (variant == 3)
                    cells[index] = source[row * 9 + (column < 2 ? 1 - column : column)];
                else if (variant == 4 && source[index] != '0')
                    cells[index] = (char)('1' + (source[index] - '0') % 9);
                else
                    cells[index] = source[index];
            }

            return new string(cells);
        }
    }
}
