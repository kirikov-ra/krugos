using System;
using System.Linq;
using Krugos.Domain;
using NUnit.Framework;

namespace Krugos.Tests.EditMode
{
    public sealed class SudokuBoardTests
    {
        [Test]
        public void RejectsNullPuzzle()
        {
            Assert.Throws<ArgumentNullException>(() => new SudokuBoard(null));
        }

        [TestCase(5)]
        [TestCase(9)]
        public void GivenCannotBeSetEvenToSameValue(int number)
        {
            var board = CreateClassicBoard();
            var position = new CellPosition(0, 0);
            Assert.Throws<InvalidOperationException>(() => board.SetValue(position, new SudokuValue(number)));
            Assert.That(board.GetValue(position)?.Value, Is.EqualTo(5));
            Assert.That(board.IsPlacementValid(position, new SudokuValue(number)), Is.False);
        }

        [Test]
        public void GivenCannotBeCleared()
        {
            var board = CreateClassicBoard();
            var position = new CellPosition(0, 0);
            Assert.Throws<InvalidOperationException>(() => board.ClearValue(position));
            Assert.That(board.GetValue(position)?.Value, Is.EqualTo(5));
            Assert.That(board.IsGiven(position), Is.True);
        }

        [Test]
        public void EditableCellCanBeSetReplacedAndCleared()
        {
            var board = CreateClassicBoard();
            var position = new CellPosition(0, 2);
            board.SetValue(position, new SudokuValue(1));
            Assert.That(board.GetValue(position)?.Value, Is.EqualTo(1));
            board.SetValue(position, new SudokuValue(4));
            Assert.That(board.GetValue(position)?.Value, Is.EqualTo(4));
            Assert.That(board.IsGiven(position), Is.False);
            board.ClearValue(position);
            Assert.That(board.GetValue(position), Is.Null);
            Assert.DoesNotThrow(() => board.ClearValue(position));
        }

        [Test]
        public void BoardsFromSamePuzzleAreIndependent()
        {
            var puzzle = SudokuPuzzle.Create(SudokuFixtures.Parse(SudokuFixtures.ClassicPuzzle));
            var first = new SudokuBoard(puzzle);
            var second = new SudokuBoard(puzzle);
            var position = new CellPosition(0, 2);
            first.SetValue(position, new SudokuValue(4));

            Assert.That(first.Puzzle, Is.SameAs(puzzle));
            Assert.That(first.GetValue(position)?.Value, Is.EqualTo(4));
            Assert.That(second.GetValue(position), Is.Null);
            Assert.That(puzzle.GetGiven(position), Is.Null);
        }

        [TestCase(0, 8, SudokuConflict.Row)]
        [TestCase(8, 0, SudokuConflict.Column)]
        [TestCase(1, 1, SudokuConflict.Box)]
        [TestCase(0, 1, SudokuConflict.Row | SudokuConflict.Box)]
        [TestCase(1, 0, SudokuConflict.Column | SudokuConflict.Box)]
        [TestCase(3, 3, SudokuConflict.None)]
        public void DetectsExactConflictUnitsAndTheirRemoval(int row, int column, SudokuConflict expected)
        {
            var board = CreateEmptyBoard();
            var first = new CellPosition(0, 0);
            var second = new CellPosition(row, column);
            var value = new SudokuValue(7);
            board.SetValue(first, value);

            Assert.That(board.GetConflicts(second, value), Is.EqualTo(expected));
            Assert.That(board.IsPlacementValid(second, value), Is.EqualTo(expected == SudokuConflict.None));
            Assert.That(board.GetValue(second), Is.Null);

            board.SetValue(second, value);
            Assert.That(board.GetConflicts(first), Is.EqualTo(expected));
            Assert.That(board.GetConflicts(second), Is.EqualTo(expected));
            board.ClearValue(second);
            Assert.That(board.GetConflicts(first), Is.EqualTo(SudokuConflict.None));
            Assert.That(board.GetConflicts(second), Is.EqualTo(SudokuConflict.None));
        }

        [Test]
        public void ReportsAllConflictFlagsTogether()
        {
            var board = CreateEmptyBoard();
            var value = new SudokuValue(9);
            board.SetValue(new CellPosition(4, 8), value);
            board.SetValue(new CellPosition(8, 4), value);
            board.SetValue(new CellPosition(3, 3), value);

            Assert.That(board.GetConflicts(new CellPosition(4, 4), value),
                Is.EqualTo(SudokuConflict.Row | SudokuConflict.Column | SudokuConflict.Box));
        }

        [Test]
        public void DetectsBoxConflictInEveryBox()
        {
            for (var row = 0; row < 9; row += 3)
            for (var column = 0; column < 9; column += 3)
            {
                var board = CreateEmptyBoard();
                board.SetValue(new CellPosition(row, column), new SudokuValue(2));
                Assert.That(board.GetConflicts(new CellPosition(row + 2, column + 2), new SudokuValue(2)),
                    Is.EqualTo(SudokuConflict.Box), $"Box starting at ({row}, {column})");
            }
        }

        [TestCase(2, 2, 3, 3)]
        [TestCase(5, 5, 6, 6)]
        [TestCase(2, 5, 3, 6)]
        public void DoesNotConfuseAdjacentBoxes(int firstRow, int firstColumn, int secondRow, int secondColumn)
        {
            var board = CreateEmptyBoard();
            board.SetValue(new CellPosition(firstRow, firstColumn), new SudokuValue(9));
            Assert.That(board.GetConflicts(new CellPosition(secondRow, secondColumn), new SudokuValue(9)),
                Is.EqualTo(SudokuConflict.None));
        }

        [Test]
        public void ValidPlacementAndReplacementExcludeTargetCell()
        {
            var board = CreateClassicBoard();
            var position = new CellPosition(0, 2);
            Assert.That(board.IsPlacementValid(position, new SudokuValue(4)), Is.True);
            board.SetValue(position, new SudokuValue(4));
            Assert.That(board.GetConflicts(position), Is.EqualTo(SudokuConflict.None));
            Assert.That(board.IsPlacementValid(position, new SudokuValue(4)), Is.True);
            Assert.That(board.IsPlacementValid(position, new SudokuValue(1)), Is.True);
            Assert.That(board.GetValue(position)?.Value, Is.EqualTo(4));
        }

        [Test]
        public void ConflictingMoveIsStoredAndCanBeCorrected()
        {
            var board = CreateClassicBoard();
            var position = new CellPosition(0, 2);
            var given = new CellPosition(0, 0);
            Assert.That(board.IsPlacementValid(position, new SudokuValue(5)), Is.False);
            board.SetValue(position, new SudokuValue(5));
            Assert.That(board.GetValue(position)?.Value, Is.EqualTo(5));
            Assert.That(board.GetConflicts(position), Is.EqualTo(SudokuConflict.Row | SudokuConflict.Box));
            Assert.That(board.GetConflicts(given), Is.EqualTo(SudokuConflict.Row | SudokuConflict.Box));
            Assert.That(board.IsComplete, Is.False);
            board.SetValue(position, new SudokuValue(4));
            Assert.That(board.GetConflicts(position), Is.EqualTo(SudokuConflict.None));
            Assert.That(board.GetConflicts(given), Is.EqualTo(SudokuConflict.None));
        }

        [TestCase(SudokuFixtures.ClassicPuzzle, 0, 2, new[] { 1, 2, 4 })]
        [TestCase(SudokuFixtures.ClassicPuzzle, 4, 4, new[] { 5 })]
        [TestCase(SudokuFixtures.SecondPuzzle, 0, 0, new[] { 4, 5 })]
        public void CalculatesExpectedCandidates(string fixture, int row, int column, int[] expected)
        {
            var board = new SudokuBoard(SudokuPuzzle.Create(SudokuFixtures.Parse(fixture)));
            Assert.That(board.GetCandidates(new CellPosition(row, column)).Select(value => value.Value),
                Is.EqualTo(expected));
        }

        [Test]
        public void EmptyBoardOffersAllNineCandidates()
        {
            Assert.That(CreateEmptyBoard().GetCandidates(new CellPosition(8, 8)).Select(value => value.Value),
                Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
        }

        [Test]
        public void FilledCellsHaveNoCandidates()
        {
            var board = CreateClassicBoard();
            var editable = new CellPosition(0, 2);
            Assert.That(board.GetCandidates(new CellPosition(0, 0)), Is.Empty);
            board.SetValue(editable, new SudokuValue(4));
            Assert.That(board.GetCandidates(editable), Is.Empty);
        }

        [Test]
        public void CandidatesReflectEditsAndPreviouslyReturnedSnapshotStaysUnchanged()
        {
            var board = CreateClassicBoard();
            var target = new CellPosition(0, 2);
            var peer = new CellPosition(0, 3);
            var before = board.GetCandidates(target);
            board.SetValue(peer, new SudokuValue(2));
            Assert.That(board.GetCandidates(target).Select(value => value.Value), Is.EqualTo(new[] { 1, 4 }));
            Assert.That(before.Select(value => value.Value), Is.EqualTo(new[] { 1, 2, 4 }));
            board.ClearValue(peer);
            Assert.That(board.GetCandidates(target).Select(value => value.Value), Is.EqualTo(new[] { 1, 2, 4 }));
        }

        [Test]
        public void LocallyConsistentPuzzleCanHaveCellWithNoCandidates()
        {
            var input = new int?[81];
            for (var column = 0; column < 8; column++)
                input[column] = column + 1;
            input[1 * 9 + 8] = 9;
            var board = new SudokuBoard(SudokuPuzzle.Create(input));

            Assert.That(board.GetCandidates(new CellPosition(0, 8)), Is.Empty);
            Assert.That(board.IsComplete, Is.False);
        }

        [Test]
        public void UnrelatedConflictDoesNotInvalidateLocalPlacement()
        {
            var board = CreateEmptyBoard();
            board.SetValue(new CellPosition(0, 0), new SudokuValue(1));
            board.SetValue(new CellPosition(0, 8), new SudokuValue(1));
            var position = new CellPosition(4, 4);
            Assert.That(board.IsPlacementValid(position, new SudokuValue(1)), Is.True);
            Assert.That(board.GetCandidates(position).Count, Is.EqualTo(9));
            Assert.That(board.IsComplete, Is.False);
        }

        [TestCase(SudokuFixtures.ClassicPuzzle)]
        [TestCase(SudokuFixtures.SecondPuzzle)]
        public void IncompletePuzzleIsNotComplete(string fixture)
        {
            var board = new SudokuBoard(SudokuPuzzle.Create(SudokuFixtures.Parse(fixture)));
            Assert.That(board.IsComplete, Is.False);
        }

        [Test]
        public void EmptyBoardIsNotComplete()
        {
            Assert.That(CreateEmptyBoard().IsComplete, Is.False);
        }

        [TestCase(SudokuFixtures.ClassicSolution)]
        [TestCase(SudokuFixtures.SecondSolution)]
        public void FullyGivenValidPuzzleIsComplete(string fixture)
        {
            var board = new SudokuBoard(SudokuPuzzle.Create(SudokuFixtures.Parse(fixture)));
            Assert.That(board.IsComplete, Is.True);
            for (var index = 0; index < 81; index++)
                Assert.That(board.GetConflicts(new CellPosition(index / 9, index % 9)), Is.EqualTo(SudokuConflict.None));
        }

        [TestCase(SudokuFixtures.ClassicPuzzle, SudokuFixtures.ClassicSolution)]
        [TestCase(SudokuFixtures.SecondPuzzle, SudokuFixtures.SecondSolution)]
        public void FillingKnownSolutionCompletesBoardAndClearingReopensIt(string puzzle, string solution)
        {
            var board = new SudokuBoard(SudokuPuzzle.Create(SudokuFixtures.Parse(puzzle)));
            FillEditableCells(board, solution);
            Assert.That(board.IsComplete, Is.True);
            var emptyIndex = puzzle.IndexOf('0');
            var position = new CellPosition(emptyIndex / 9, emptyIndex % 9);
            board.ClearValue(position);
            Assert.That(board.IsComplete, Is.False);
            Assert.That(board.GetCandidates(position).Select(value => value.Value),
                Is.EqualTo(new[] { solution[emptyIndex] - '0' }));
        }

        [Test]
        public void FullyFilledInvalidBoardIsNotCompleteAndCanBeRepaired()
        {
            var board = CreateClassicBoard();
            FillEditableCells(board, SudokuFixtures.ClassicSolution);
            var position = new CellPosition(0, 2);
            board.SetValue(position, new SudokuValue(5));
            for (var index = 0; index < 81; index++)
                Assert.That(board.GetValue(new CellPosition(index / 9, index % 9)).HasValue, Is.True);

            Assert.That(board.IsComplete, Is.False);
            board.SetValue(position, new SudokuValue(4));
            Assert.That(board.IsComplete, Is.True);
        }

        [Test]
        public void FullBoardWithValidRowsAndColumnsButInvalidBoxesIsNotComplete()
        {
            const string invalidSolution =
                "123456789" + "234567891" + "345678912" +
                "456789123" + "567891234" + "678912345" +
                "789123456" + "891234567" + "912345678";
            var board = CreateEmptyBoard();
            FillEditableCells(board, invalidSolution);
            for (var index = 0; index < 81; index++)
            {
                var conflicts = board.GetConflicts(new CellPosition(index / 9, index % 9));
                Assert.That(conflicts & (SudokuConflict.Row | SudokuConflict.Column), Is.EqualTo(SudokuConflict.None));
            }

            Assert.That(board.GetConflicts(new CellPosition(1, 1)), Is.EqualTo(SudokuConflict.Box));
            Assert.That(board.IsComplete, Is.False);
        }

        private static SudokuBoard CreateClassicBoard() =>
            new SudokuBoard(SudokuPuzzle.Create(SudokuFixtures.Parse(SudokuFixtures.ClassicPuzzle)));

        private static SudokuBoard CreateEmptyBoard() => new SudokuBoard(SudokuPuzzle.Create(new int?[81]));

        private static void FillEditableCells(SudokuBoard board, string solution)
        {
            var values = SudokuFixtures.Parse(solution);
            for (var index = 0; index < values.Length; index++)
            {
                var position = new CellPosition(index / 9, index % 9);
                if (!board.IsGiven(position))
                    board.SetValue(position, new SudokuValue(values[index].Value));
            }
        }
    }
}
