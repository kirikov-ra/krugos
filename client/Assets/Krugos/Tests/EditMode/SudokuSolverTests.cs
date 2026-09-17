using System;
using System.Diagnostics;
using System.Linq;
using Krugos.Domain;
using NUnit.Framework;

namespace Krugos.Tests.EditMode
{
    public sealed class SudokuSolverTests
    {
        [TestCase(SudokuFixtures.ClassicPuzzle, SudokuFixtures.ClassicSolution)]
        [TestCase(SudokuFixtures.SecondPuzzle, SudokuFixtures.SecondSolution)]
        [TestCase(SudokuFixtures.ClassicSolution, SudokuFixtures.ClassicSolution)]
        [TestCase(SudokuFixtures.SecondSolution, SudokuFixtures.SecondSolution)]
        [TestCase(SudokuFixtures.TwoSolutionPuzzle, SudokuFixtures.ClassicSolution)]
        public void ReturnsExactCanonicalSolution(string fixture, string expected)
        {
            var puzzle = Create(fixture);
            Assert.That(SudokuSolver.TrySolve(puzzle, out var solution), Is.True);
            Assert.That(ReadSolution(solution), Is.EqualTo(SudokuFixtures.Parse(expected).Select(value => value.Value)));
            AssertValidSolution(puzzle, solution);
        }

        [TestCase(SudokuFixtures.ClassicPuzzle)]
        [TestCase(SudokuFixtures.SecondPuzzle)]
        [TestCase(SudokuFixtures.ClassicSolution)]
        [TestCase(SudokuFixtures.SecondSolution)]
        [TestCase(SudokuFixtures.HardPuzzle)]
        [TestCase(SudokuFixtures.SeventeenGivenPuzzle)]
        public void IdentifiesUniquePuzzlesAndExhaustsSearch(string fixture)
        {
            var puzzle = Create(fixture);
            Assert.That(SudokuSolver.HasSolution(puzzle), Is.True);
            Assert.That(SudokuSolver.HasUniqueSolution(puzzle), Is.True);
            Assert.That(SudokuSolver.CountSolutions(puzzle, 1), Is.EqualTo(1));
            Assert.That(SudokuSolver.CountSolutions(puzzle, 2), Is.EqualTo(1));
            Assert.That(SudokuSolver.CountSolutions(puzzle, int.MaxValue), Is.EqualTo(1));
            Assert.That(SudokuSolver.TrySolve(puzzle, out var solution), Is.True);
            AssertValidSolution(puzzle, solution);
        }

        [TestCase(SudokuFixtures.ImmediateUnsolvablePuzzle)]
        [TestCase(SudokuFixtures.UnsolvablePuzzle)]
        public void StructurallyValidUnsolvablePuzzleReturnsFalseZeroAndNull(string fixture)
        {
            var puzzle = Create(fixture);
            Assert.That(SudokuSolver.TrySolve(Create(SudokuFixtures.ClassicPuzzle), out var solution), Is.True);
            Assert.That(SudokuSolver.TrySolve(puzzle, out solution), Is.False);
            Assert.That(solution, Is.Null);
            Assert.That(SudokuSolver.HasSolution(puzzle), Is.False);
            Assert.That(SudokuSolver.HasUniqueSolution(puzzle), Is.False);
            Assert.That(SudokuSolver.CountSolutions(puzzle, 1), Is.Zero);
            Assert.That(SudokuSolver.CountSolutions(puzzle, int.MaxValue), Is.Zero);
        }

        [Test]
        public void UnsolvableFixtureRequiresSearchBeyondInitialCandidateCheck()
        {
            var board = new SudokuBoard(Create(SudokuFixtures.UnsolvablePuzzle));
            for (var index = 0; index < 81; index++)
            {
                var position = Position(index);
                if (!board.IsGiven(position))
                    Assert.That(board.GetCandidates(position), Is.Not.Empty);
            }

            Assert.That(SudokuSolver.HasSolution(board.Puzzle), Is.False);
        }

        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 2)]
        [TestCase(int.MaxValue, 2)]
        public void CountsExactlyTwoSolutionsWithClamping(int limit, int expected)
        {
            var puzzle = Create(SudokuFixtures.TwoSolutionPuzzle);
            Assert.That(SudokuSolver.CountSolutions(puzzle, limit), Is.EqualTo(expected));
            Assert.That(SudokuSolver.HasSolution(puzzle), Is.True);
            Assert.That(SudokuSolver.HasUniqueSolution(puzzle), Is.False);
        }

        // Exhaustively enumerating this puzzle is infeasible: bounded search must stop at the limit.
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(10)]
        public void EmptyPuzzleSearchStopsAtRequestedSolutionLimit(int limit)
        {
            var puzzle = SudokuPuzzle.Create(new int?[81]);
            Assert.That(SudokuSolver.CountSolutions(puzzle, limit), Is.EqualTo(limit));
        }

        [Test]
        public void EmptyPuzzleIsSolvableButNotUnique()
        {
            var puzzle = SudokuPuzzle.Create(new int?[81]);
            Assert.That(SudokuSolver.HasSolution(puzzle), Is.True);
            Assert.That(SudokuSolver.HasUniqueSolution(puzzle), Is.False);
            Assert.That(SudokuSolver.TrySolve(puzzle, out var solution), Is.True);
            AssertValidSolution(puzzle, solution);
        }

        [Test]
        public void SolvesSparsePuzzleWithOnlyLastCellGiven()
        {
            var cells = new int?[81];
            cells[80] = 9;
            var puzzle = SudokuPuzzle.Create(cells);
            Assert.That(SudokuSolver.TrySolve(puzzle, out var solution), Is.True);
            AssertValidSolution(puzzle, solution);
            Assert.That(SudokuSolver.CountSolutions(puzzle, 2), Is.EqualTo(2));
        }

        [Test]
        public void SolvesEverySingleMissingCellIncludingUnitBoundaries()
        {
            var expected = SudokuFixtures.Parse(SudokuFixtures.ClassicSolution);
            for (var missing = 0; missing < 81; missing++)
            {
                var cells = (int?[])expected.Clone();
                cells[missing] = null;
                var puzzle = SudokuPuzzle.Create(cells);
                Assert.That(SudokuSolver.TrySolve(puzzle, out var solution), Is.True, $"Missing cell {missing}");
                Assert.That(ReadSolution(solution), Is.EqualTo(expected.Select(value => value.Value)));
                Assert.That(SudokuSolver.HasUniqueSolution(puzzle), Is.True);
            }
        }

        [TestCase(SudokuFixtures.ClassicPuzzle)]
        [TestCase(SudokuFixtures.HardPuzzle)]
        [TestCase(SudokuFixtures.TwoSolutionPuzzle)]
        [TestCase(SudokuFixtures.UnsolvablePuzzle)]
        [TestCase(SudokuFixtures.ImmediateUnsolvablePuzzle)]
        public void AllOperationsPreservePuzzleAndExistingBoard(string fixture)
        {
            var cells = SudokuFixtures.Parse(fixture);
            var puzzle = SudokuPuzzle.Create(cells);
            var board = new SudokuBoard(puzzle);
            var editable = Position(Array.IndexOf(cells, null));
            board.SetValue(editable, new SudokuValue(9));

            SudokuSolver.TrySolve(puzzle, out _);
            SudokuSolver.HasSolution(puzzle);
            SudokuSolver.HasUniqueSolution(puzzle);
            SudokuSolver.CountSolutions(puzzle, 3);

            for (var index = 0; index < 81; index++)
            {
                var position = Position(index);
                Assert.That(puzzle.GetGiven(position)?.Value, Is.EqualTo(cells[index]));
                Assert.That(puzzle.IsGiven(position), Is.EqualTo(cells[index].HasValue));
                Assert.That(board.GetValue(position)?.Value, Is.EqualTo(position == editable ? 9 : cells[index]));
            }
        }

        [TestCase(SudokuFixtures.HardPuzzle)]
        [TestCase(SudokuFixtures.TwoSolutionPuzzle)]
        [TestCase("000000000000000000000000000000000000000000000000000000000000000000000000000000000")]
        public void CanonicalSolutionIsStableAcrossSearchesAndOwnsItsSnapshot(string fixture)
        {
            var puzzle = Create(fixture);
            Assert.That(SudokuSolver.TrySolve(puzzle, out var first), Is.True);
            var snapshot = ReadSolution(first);
            SudokuSolver.CountSolutions(puzzle, 3);
            SudokuSolver.TrySolve(Create(SudokuFixtures.UnsolvablePuzzle), out _);
            SudokuSolver.TrySolve(Create(SudokuFixtures.SecondPuzzle), out _);
            Assert.That(SudokuSolver.TrySolve(puzzle, out var second), Is.True);
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(ReadSolution(first), Is.EqualTo(snapshot));
            Assert.That(ReadSolution(second), Is.EqualTo(snapshot));
            AssertValidSolution(puzzle, second);
        }

        [Test]
        public void LegalPlacementCanDifferFromCanonicalValueAndBoardStillAcceptsIt()
        {
            var puzzle = Create(SudokuFixtures.ClassicPuzzle);
            var board = new SudokuBoard(puzzle);
            var position = new CellPosition(0, 2);
            var wrongValue = new SudokuValue(1);
            Assert.That(SudokuSolver.TrySolve(puzzle, out var solution), Is.True);
            Assert.That(board.IsPlacementValid(position, wrongValue), Is.True);
            Assert.That(solution.GetValue(position), Is.EqualTo(new SudokuValue(4)));
            Assert.That(solution.GetValue(position), Is.Not.EqualTo(wrongValue));
            board.SetValue(position, wrongValue);
            Assert.That(board.GetValue(position), Is.EqualTo(wrongValue));
            board.SetValue(position, new SudokuValue(5));
            Assert.That(board.GetConflicts(position), Is.Not.EqualTo(SudokuConflict.None));
            Assert.That(solution.GetValue(position).Value, Is.EqualTo(4));
        }

        [Test]
        public void AllEntryPointsRejectNullPuzzle()
        {
            Assert.Throws<ArgumentNullException>(() => SudokuSolver.TrySolve(null, out _));
            Assert.Throws<ArgumentNullException>(() => SudokuSolver.HasSolution(null));
            Assert.Throws<ArgumentNullException>(() => SudokuSolver.HasUniqueSolution(null));
            Assert.Throws<ArgumentNullException>(() => SudokuSolver.CountSolutions(null, 2));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void RejectsNonPositiveSolutionLimit(int limit)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SudokuSolver.CountSolutions(Create(SudokuFixtures.ClassicPuzzle), limit));
        }

        [Test]
        public void FixedCorpusPerformanceDiagnosticWithoutTimingThreshold()
        {
            var puzzles = new[]
            {
                Create(SudokuFixtures.ClassicPuzzle), Create(SudokuFixtures.SecondPuzzle),
                Create(SudokuFixtures.HardPuzzle), Create(SudokuFixtures.SeventeenGivenPuzzle),
                Create(SudokuFixtures.UnsolvablePuzzle), Create(SudokuFixtures.TwoSolutionPuzzle),
                SudokuPuzzle.Create(new int?[81])
            };
            var expectedCounts = new[] { 1, 1, 1, 1, 0, 2, 2 };
            SudokuSolver.CountSolutions(puzzles[0], 2);
            var stopwatch = Stopwatch.StartNew();
            for (var iteration = 0; iteration < 5; iteration++)
            for (var index = 0; index < puzzles.Length; index++)
                Assert.That(SudokuSolver.CountSolutions(puzzles[index], 2), Is.EqualTo(expectedCounts[index]));
            stopwatch.Stop();
            TestContext.WriteLine($"35 bounded searches over a fixed corpus: {stopwatch.Elapsed.TotalMilliseconds:F2} ms (diagnostic only).");
        }

        private static SudokuPuzzle Create(string fixture) => SudokuPuzzle.Create(SudokuFixtures.Parse(fixture));
        private static CellPosition Position(int index) => new CellPosition(index / 9, index % 9);

        private static int[] ReadSolution(SudokuSolution solution) =>
            Enumerable.Range(0, 81).Select(index => solution.GetValue(Position(index)).Value).ToArray();

        private static void AssertValidSolution(SudokuPuzzle puzzle, SudokuSolution solution)
        {
            var values = ReadSolution(solution);
            var expected = Enumerable.Range(1, 9).ToArray();
            for (var unit = 0; unit < 9; unit++)
            {
                Assert.That(Enumerable.Range(0, 9).Select(offset => values[unit * 9 + offset]), Is.EquivalentTo(expected));
                Assert.That(Enumerable.Range(0, 9).Select(offset => values[offset * 9 + unit]), Is.EquivalentTo(expected));
                var start = unit / 3 * 27 + unit % 3 * 3;
                Assert.That(Enumerable.Range(0, 9).Select(offset => values[start + offset / 3 * 9 + offset % 3]),
                    Is.EquivalentTo(expected));
            }

            var board = new SudokuBoard(puzzle);
            for (var index = 0; index < 81; index++)
            {
                var position = Position(index);
                if (puzzle.IsGiven(position))
                    Assert.That(values[index], Is.EqualTo(puzzle.GetGiven(position).Value.Value));
                else
                    board.SetValue(position, solution.GetValue(position));
            }

            Assert.That(board.IsComplete, Is.True);
        }
    }
}
