using Krugos.Domain;
using NUnit.Framework;

namespace Krugos.Tests.EditMode
{
    public sealed class SudokuPuzzleTests
    {
        [TestCase(SudokuFixtures.ClassicPuzzle)]
        [TestCase(SudokuFixtures.SecondPuzzle)]
        [TestCase(SudokuFixtures.ClassicSolution)]
        [TestCase(SudokuFixtures.SecondSolution)]
        public void CreatesValidPuzzleAndPreservesEveryCell(string fixture)
        {
            var input = SudokuFixtures.Parse(fixture);
            var puzzle = SudokuPuzzle.Create(input);
            var board = new SudokuBoard(puzzle);

            for (var index = 0; index < input.Length; index++)
            {
                var position = new CellPosition(index / 9, index % 9);
                Assert.That(puzzle.GetGiven(position)?.Value, Is.EqualTo(input[index]));
                Assert.That(board.GetValue(position)?.Value, Is.EqualTo(input[index]));
                Assert.That(puzzle.IsGiven(position), Is.EqualTo(input[index].HasValue));
                Assert.That(board.IsGiven(position), Is.EqualTo(input[index].HasValue));
            }
        }

        [Test]
        public void AcceptsEmptyPuzzleWithoutRequiringUniqueSolution()
        {
            var puzzle = SudokuPuzzle.Create(new int?[81]);
            Assert.That(puzzle.IsGiven(new CellPosition(8, 8)), Is.False);
        }

        [Test]
        public void CopiesInputAndDoesNotExposeMutableGivens()
        {
            var input = SudokuFixtures.Parse(SudokuFixtures.ClassicPuzzle);
            var puzzle = SudokuPuzzle.Create(input);
            input[0] = 9;
            input[2] = 4;

            Assert.That(puzzle.GetGiven(new CellPosition(0, 0))?.Value, Is.EqualTo(5));
            Assert.That(puzzle.GetGiven(new CellPosition(0, 2)), Is.Null);
        }

        [Test]
        public void RejectsNullInputWithDomainValidationError()
        {
            Assert.Throws<PuzzleValidationException>(() => SudokuPuzzle.Create(null));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(80)]
        [TestCase(82)]
        [TestCase(162)]
        public void RejectsWrongLength(int length)
        {
            Assert.Throws<PuzzleValidationException>(() => SudokuPuzzle.Create(new int?[length]));
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(10)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void RejectsInvalidValueIncludingLastCell(int value)
        {
            var input = new int?[81];
            input[80] = value;
            Assert.Throws<PuzzleValidationException>(() => SudokuPuzzle.Create(input));
        }

        [TestCase(0, 0, 0, 8)]
        [TestCase(0, 0, 8, 0)]
        [TestCase(0, 0, 1, 1)]
        [TestCase(6, 6, 8, 8)]
        public void RejectsConflictingGivens(int firstRow, int firstColumn, int secondRow, int secondColumn)
        {
            var input = new int?[81];
            input[firstRow * 9 + firstColumn] = 7;
            input[secondRow * 9 + secondColumn] = 7;
            Assert.Throws<PuzzleValidationException>(() => SudokuPuzzle.Create(input));
        }

        [Test]
        public void AllowsRepeatedGivensInUnrelatedUnits()
        {
            var input = new int?[81];
            input[0] = 9;
            input[4 * 9 + 4] = 9;
            input[8 * 9 + 8] = 9;
            Assert.DoesNotThrow(() => SudokuPuzzle.Create(input));
        }
    }
}
