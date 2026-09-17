using System;
using System.Linq;
using Krugos.Domain;
using NUnit.Framework;

namespace Krugos.Tests.EditMode
{
    public sealed class SudokuPuzzleDefinitionTests
    {
        [TestCase(SudokuFixtures.ClassicPuzzle, SudokuFixtures.ClassicSolution)]
        [TestCase(SudokuFixtures.SecondPuzzle, SudokuFixtures.SecondSolution)]
        [TestCase(SudokuFixtures.ClassicSolution, SudokuFixtures.ClassicSolution)]
        public void FactoryCreatesDefinitionWithMatchingCanonicalSolution(string fixture, string expected)
        {
            var puzzle = SudokuPuzzle.Create(SudokuFixtures.Parse(fixture));
            var definition = SudokuPuzzleDefinition.Create("fixture-001", puzzle);

            Assert.That(definition.Puzzle, Is.SameAs(puzzle));
            Assert.That(definition.PuzzleId, Is.EqualTo("fixture-001"));
            Assert.That(SudokuSolver.HasUniqueSolution(definition.Puzzle), Is.True);
            Assert.That(SudokuSolver.TrySolve(puzzle, out var canonical), Is.True);
            var expectedCells = SudokuFixtures.Parse(expected);
            var board = new SudokuBoard(puzzle);
            for (var index = 0; index < 81; index++)
            {
                var position = new CellPosition(index / 9, index % 9);
                var value = definition.Solution.GetValue(position);
                Assert.That(value.Value, Is.EqualTo(expectedCells[index]));
                Assert.That(value, Is.EqualTo(canonical.GetValue(position)));
                if (puzzle.IsGiven(position))
                    Assert.That(value, Is.EqualTo(puzzle.GetGiven(position).Value));
                else
                    board.SetValue(position, value);
            }

            Assert.That(board.IsComplete, Is.True);
            AssertGivens(puzzle, SudokuFixtures.Parse(fixture));
        }

        [TestCase("a")]
        [TestCase("0")]
        [TestCase("fixture-001")]
        [TestCase("level9-part2")]
        public void PuzzleIdIsPreservedAcrossIndependentCreations(string puzzleId)
        {
            var first = SudokuPuzzleDefinition.Create(puzzleId,
                SudokuPuzzle.Create(SudokuFixtures.Parse(SudokuFixtures.ClassicPuzzle)));
            var second = SudokuPuzzleDefinition.Create(puzzleId,
                SudokuPuzzle.Create(SudokuFixtures.Parse(SudokuFixtures.ClassicPuzzle)));

            Assert.That(first.PuzzleId, Is.EqualTo(puzzleId));
            Assert.That(second.PuzzleId, Is.EqualTo(first.PuzzleId));
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase("\t")]
        [TestCase(" fixture-001")]
        [TestCase("fixture-001 ")]
        [TestCase("fixture-001\n")]
        [TestCase("fixture 001")]
        [TestCase("Fixture-001")]
        [TestCase("fixture/001")]
        [TestCase("fixture_001")]
        [TestCase("fixture.001")]
        [TestCase("-fixture")]
        [TestCase("fixture-")]
        [TestCase("fixture--001")]
        [TestCase("головоломка-001")]
        [TestCase("fixture-\0")]
        public void FactoryRejectsInvalidPuzzleId(string puzzleId)
        {
            var puzzle = SudokuPuzzle.Create(SudokuFixtures.Parse(SudokuFixtures.ClassicPuzzle));
            var exception = Assert.Throws<ArgumentException>(() => SudokuPuzzleDefinition.Create(puzzleId, puzzle));
            Assert.That(exception.ParamName, Is.EqualTo("puzzleId"));
        }

        [Test]
        public void FactoryRejectsNullPuzzleId()
        {
            var puzzle = SudokuPuzzle.Create(SudokuFixtures.Parse(SudokuFixtures.ClassicPuzzle));
            Assert.Throws<ArgumentNullException>(() => SudokuPuzzleDefinition.Create(null, puzzle));
        }

        [Test]
        public void FactoryRejectsNullPuzzle()
        {
            Assert.Throws<ArgumentNullException>(() => SudokuPuzzleDefinition.Create("fixture-001", null));
        }

        [TestCase(SudokuFixtures.ImmediateUnsolvablePuzzle)]
        [TestCase(SudokuFixtures.UnsolvablePuzzle)]
        public void FactoryRejectsUnsolvablePuzzleWithoutMutatingGivens(string fixture)
        {
            var cells = SudokuFixtures.Parse(fixture);
            var puzzle = SudokuPuzzle.Create(cells);
            Assert.That(SudokuSolver.CountSolutions(puzzle, 2), Is.Zero);
            Assert.Throws<PuzzleValidationException>(() => SudokuPuzzleDefinition.Create("unsolvable", puzzle));
            AssertGivens(puzzle, cells);
        }

        [TestCase(SudokuFixtures.TwoSolutionPuzzle)]
        [TestCase("000000000000000000000000000000000000000000000000000000000000000000000000000000000")]
        public void FactoryRejectsAmbiguousPuzzleWithoutMutatingGivens(string fixture)
        {
            var cells = SudokuFixtures.Parse(fixture);
            var puzzle = SudokuPuzzle.Create(cells);
            Assert.That(SudokuSolver.CountSolutions(puzzle, 2), Is.EqualTo(2));
            Assert.Throws<PuzzleValidationException>(() => SudokuPuzzleDefinition.Create("ambiguous", puzzle));
            AssertGivens(puzzle, cells);
        }

        [Test]
        public void DefinitionRemainsUnchangedAfterInputAndBoardMutations()
        {
            var cells = SudokuFixtures.Parse(SudokuFixtures.ClassicPuzzle);
            var puzzle = SudokuPuzzle.Create(cells);
            var board = new SudokuBoard(puzzle);
            var editable = new CellPosition(0, 2);
            board.SetValue(editable, new SudokuValue(1));
            var definition = SudokuPuzzleDefinition.Create("fixture-001", puzzle);
            Assert.That(board.GetValue(editable)?.Value, Is.EqualTo(1));

            cells[0] = 9;
            cells[2] = 9;
            board.SetValue(editable, new SudokuValue(9));
            SudokuSolver.TrySolve(SudokuPuzzle.Create(SudokuFixtures.Parse(SudokuFixtures.SecondPuzzle)), out _);

            AssertGivens(definition.Puzzle, SudokuFixtures.Parse(SudokuFixtures.ClassicPuzzle));
            Assert.That(Enumerable.Range(0, 81).Select(index =>
                definition.Solution.GetValue(new CellPosition(index / 9, index % 9)).Value),
                Is.EqualTo(SudokuFixtures.Parse(SudokuFixtures.ClassicSolution).Select(value => value.Value)));
            Assert.That(new SudokuBoard(definition.Puzzle).GetValue(editable), Is.Null);
        }

        private static void AssertGivens(SudokuPuzzle puzzle, int?[] expected)
        {
            for (var index = 0; index < 81; index++)
            {
                var position = new CellPosition(index / 9, index % 9);
                Assert.That(puzzle.GetGiven(position)?.Value, Is.EqualTo(expected[index]));
                Assert.That(puzzle.IsGiven(position), Is.EqualTo(expected[index].HasValue));
            }
        }
    }
}
