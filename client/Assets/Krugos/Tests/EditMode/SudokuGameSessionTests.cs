using System;
using System.Collections.Generic;
using System.Linq;
using Krugos.Domain;
using Krugos.Infrastructure;
using NUnit.Framework;

namespace Krugos.Tests.EditMode
{
    public sealed class SudokuGameSessionTests
    {
        private static readonly CellPosition Editable = new CellPosition(0, 2);
        private static readonly CellPosition Peer = new CellPosition(0, 3);
        private static readonly CellPosition Given = new CellPosition(0, 0);
        private SudokuGameSession session;

        [SetUp]
        public void SetUp()
        {
            session = new SudokuGameSession(SudokuPuzzleDefinition.Create("session-fixture",
                SudokuPuzzle.Create(SudokuFixtures.Parse(SudokuFixtures.ClassicPuzzle))), TimeSpan.FromSeconds(10));
        }

        [Test]
        public void StartsActiveWithThreeLivesAndPuzzleBoard()
        {
            Assert.That(session.State, Is.EqualTo(SudokuSessionState.Active));
            Assert.That(session.RemainingLives, Is.EqualTo(3));
            Assert.That(session.MistakeCount, Is.Zero);
            Assert.That(session.Board.Puzzle, Is.SameAs(session.Definition.Puzzle));
            Assert.That(session.Board.IsComplete, Is.False);
            Assert.That(session.ElapsedTime, Is.EqualTo(TimeSpan.Zero));
            Assert.That(session.RemainingBonusTime, Is.EqualTo(TimeSpan.FromSeconds(10)));
            Assert.That(session.IsSpeedBonusAvailable, Is.True);
            for (var index = 0; index < 81; index++)
            {
                var position = new CellPosition(index / 9, index % 9);
                Assert.That(session.Board.GetValue(position), Is.EqualTo(session.Definition.Puzzle.GetGiven(position)));
                Assert.That(session.Board.IsGiven(position), Is.EqualTo(session.Definition.Puzzle.IsGiven(position)));
                Assert.That(session.GetNotes(position), Is.Empty);
            }
        }

        [Test]
        public void RejectsNullDefinition()
        {
            Assert.Throws<ArgumentNullException>(() => new SudokuGameSession(null, TimeSpan.Zero));
        }

        [TestCase(-1L)]
        [TestCase(long.MinValue)]
        public void RejectsNegativeBonusDuration(long ticks)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SudokuGameSession(session.Definition, TimeSpan.FromTicks(ticks)));
        }

        [Test]
        public void FullyGivenDefinitionStartsWon()
        {
            var definition = SudokuPuzzleDefinition.Create("already-complete",
                SudokuPuzzle.Create(SudokuFixtures.Parse(SudokuFixtures.ClassicSolution)));
            var complete = new SudokuGameSession(definition, TimeSpan.FromSeconds(10));
            Assert.That(complete.Board.IsComplete, Is.True);
            Assert.That(complete.State, Is.EqualTo(SudokuSessionState.Won));
            Assert.That(complete.RemainingLives, Is.EqualTo(3));
            Assert.That(complete.ClearValue(Given).Status, Is.EqualTo(SudokuMoveStatus.SessionEnded));
        }

        [Test]
        public void CorrectMoveKeepsLivesAndLiveBoardViewReflectsIt()
        {
            var view = session.Board;
            var result = session.SetValue(Editable, new SudokuValue(4));
            AssertMove(result, SudokuMoveStatus.Applied, true, false, 3, SudokuSessionState.Active, false);
            Assert.That(view.GetValue(Editable)?.Value, Is.EqualTo(4));
            Assert.That(session.Board, Is.SameAs(view));
            Assert.That(view.GetConflicts(Editable), Is.EqualTo(SudokuConflict.None));
            Assert.That(session.MistakeCount, Is.Zero);
            Assert.That(session.State, Is.EqualTo(SudokuSessionState.Active));
        }

        [Test]
        public void LocallyLegalButNonCanonicalValueCostsOneLifeAndRemainsOnBoard()
        {
            var wrong = new SudokuValue(1);
            Assert.That(session.Board.IsPlacementValid(Editable, wrong), Is.True);
            Assert.That(session.Board.GetCandidates(Editable), Does.Contain(wrong));
            var result = session.SetValue(Editable, wrong);
            AssertMove(result, SudokuMoveStatus.Applied, false, true, 2, SudokuSessionState.Active, false);
            Assert.That(session.Board.GetValue(Editable), Is.EqualTo(wrong));
            Assert.That(session.Board.GetConflicts(Editable), Is.EqualTo(SudokuConflict.None));
            Assert.That(session.MistakeCount, Is.EqualTo(1));
        }

        [TestCase(5)]
        [TestCase(6)]
        [TestCase(8)]
        public void ConflictingWrongValueAlsoCostsExactlyOneLife(int number)
        {
            var wrong = new SudokuValue(number);
            Assert.That(session.Board.GetConflicts(Editable, wrong), Is.Not.EqualTo(SudokuConflict.None));
            var result = session.SetValue(Editable, wrong);
            AssertMove(result, SudokuMoveStatus.Applied, false, true, 2, SudokuSessionState.Active, false);
            Assert.That(session.Board.GetValue(Editable), Is.EqualTo(wrong));
            Assert.That(session.Board.IsComplete, Is.False);
        }

        [Test]
        public void RepeatingSameWrongValueDoesNotSpendMoreLives()
        {
            session.SetValue(Editable, new SudokuValue(1));
            for (var attempt = 0; attempt < 10; attempt++)
            {
                var result = session.SetValue(Editable, new SudokuValue(1));
                AssertMove(result, SudokuMoveStatus.Unchanged, false, false, 2, SudokuSessionState.Active, false);
            }

            Assert.That(session.MistakeCount, Is.EqualTo(1));
            Assert.That(session.Board.GetValue(Editable)?.Value, Is.EqualTo(1));
        }

        [Test]
        public void RepeatingSameCorrectValueIsUnchanged()
        {
            session.SetValue(Editable, new SudokuValue(4));
            AssertMove(session.SetValue(Editable, new SudokuValue(4)), SudokuMoveStatus.Unchanged,
                true, false, 3, SudokuSessionState.Active, false);
        }

        [Test]
        public void ReplacingWrongValueWithDifferentWrongValueIsAnotherMistake()
        {
            session.SetValue(Editable, new SudokuValue(1));
            AssertMove(session.SetValue(Editable, new SudokuValue(2)), SudokuMoveStatus.Applied,
                false, true, 1, SudokuSessionState.Active, false);
            Assert.That(session.MistakeCount, Is.EqualTo(2));
            Assert.That(session.Board.GetValue(Editable)?.Value, Is.EqualTo(2));
        }

        [Test]
        public void CorrectionDoesNotSpendOrRestoreLife()
        {
            session.SetValue(Editable, new SudokuValue(1));
            AssertMove(session.SetValue(Editable, new SudokuValue(4)), SudokuMoveStatus.Applied,
                true, false, 2, SudokuSessionState.Active, false);
            Assert.That(session.Board.GetValue(Editable)?.Value, Is.EqualTo(4));
            Assert.That(session.MistakeCount, Is.EqualTo(1));
        }

        [Test]
        public void CorrectCanonicalValueDoesNotCostLifeEvenWithWrongPeerConflict()
        {
            session.SetValue(Peer, new SudokuValue(4));
            Assert.That(session.Board.IsPlacementValid(Editable, new SudokuValue(4)), Is.False);
            AssertMove(session.SetValue(Editable, new SudokuValue(4)), SudokuMoveStatus.Applied,
                true, false, 2, SudokuSessionState.Active, false);
            Assert.That(session.Board.GetConflicts(Editable), Is.Not.EqualTo(SudokuConflict.None));
        }

        [TestCase(1)]
        [TestCase(4)]
        public void ClearValueDoesNotSpendOrRestoreLives(int number)
        {
            session.SetValue(Editable, new SudokuValue(number));
            var lives = session.RemainingLives;
            AssertMove(session.ClearValue(Editable), SudokuMoveStatus.Applied,
                null, false, lives, SudokuSessionState.Active, false);
            Assert.That(session.Board.GetValue(Editable), Is.Null);
            AssertMove(session.ClearValue(Editable), SudokuMoveStatus.Unchanged,
                null, false, lives, SudokuSessionState.Active, false);
        }

        [Test]
        public void ClearingThenReenteringWrongValueIsAnotherAttempt()
        {
            session.SetValue(Editable, new SudokuValue(1));
            session.ClearValue(Editable);
            AssertMove(session.SetValue(Editable, new SudokuValue(1)), SudokuMoveStatus.Applied,
                false, true, 1, SudokuSessionState.Active, false);
        }

        [Test]
        public void EveryGivenRejectsAllPlayerMutationsIncludingItsOwnValue()
        {
            var before = Snapshot(session);
            for (var index = 0; index < 81; index++)
            {
                var position = new CellPosition(index / 9, index % 9);
                if (!session.Board.IsGiven(position))
                    continue;
                var results = MutateEveryWay(session, position).Concat(new[]
                {
                    session.SetValue(position, session.Definition.Solution.GetValue(position))
                });
                foreach (var result in results)
                    AssertMove(result, SudokuMoveStatus.GivenCell, null, false, 3, SudokuSessionState.Active, false);
            }

            Assert.That(Snapshot(session), Is.EqualTo(before));
        }

        [Test]
        public void ThirdMistakeLosesAndStoresFinalWrongValue()
        {
            session.SetValue(Editable, new SudokuValue(1));
            session.SetValue(Editable, new SudokuValue(2));
            var result = session.SetValue(Editable, new SudokuValue(5));
            AssertMove(result, SudokuMoveStatus.Applied, false, true, 0, SudokuSessionState.Lost, true);
            Assert.That(result.PreviousState, Is.EqualTo(SudokuSessionState.Active));
            Assert.That(session.State, Is.EqualTo(SudokuSessionState.Lost));
            Assert.That(session.RemainingLives, Is.Zero);
            Assert.That(session.MistakeCount, Is.EqualTo(3));
            Assert.That(session.Board.GetValue(Editable)?.Value, Is.EqualTo(5));
        }

        [TestCase(SudokuSessionState.Won)]
        [TestCase(SudokuSessionState.Lost)]
        public void TerminalSessionRejectsEveryMutationForEveryCell(SudokuSessionState terminal)
        {
            session.AddNote(Peer, new SudokuValue(9));
            session.AdvanceTime(TimeSpan.FromSeconds(2));
            if (terminal == SudokuSessionState.Won)
                FillCanonical(session);
            else
            {
                session.SetValue(Editable, new SudokuValue(1));
                session.SetValue(Editable, new SudokuValue(2));
                session.SetValue(Editable, new SudokuValue(5));
            }

            Assert.That(session.State, Is.EqualTo(terminal));
            var before = Snapshot(session);
            var lives = session.RemainingLives;
            for (var index = 0; index < 81; index++)
            {
                var position = new CellPosition(index / 9, index % 9);
                foreach (var result in MutateEveryWay(session, position))
                    AssertMove(result, SudokuMoveStatus.SessionEnded, null, false, lives, terminal, false);
            }

            session.AdvanceTime(TimeSpan.MaxValue);
            Assert.That(Snapshot(session), Is.EqualTo(before));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void EveryBuiltInPuzzleCanBeWon(int index)
        {
            var definition = new BuiltInSudokuPuzzleProvider().GetPuzzle(index);
            var game = new SudokuGameSession(definition, TimeSpan.FromSeconds(10));
            var result = FillCanonical(game);
            AssertMove(result, SudokuMoveStatus.Applied, true, false, 3, SudokuSessionState.Won, true);
            Assert.That(game.Board.IsComplete, Is.True);
            Assert.That(game.State, Is.EqualTo(SudokuSessionState.Won));
        }

        [TestCase(SudokuFixtures.ClassicPuzzle)]
        [TestCase(SudokuFixtures.SecondPuzzle)]
        public void IncompleteCanonicalBoardStaysActiveUntilLastCell(string fixture)
        {
            var definition = SudokuPuzzleDefinition.Create("completion-fixture",
                SudokuPuzzle.Create(SudokuFixtures.Parse(fixture)));
            var game = new SudokuGameSession(definition, TimeSpan.Zero);
            var last = Enumerable.Range(0, 81).Select(index => new CellPosition(index / 9, index % 9))
                .Last(position => !game.Board.IsGiven(position));
            FillCanonical(game, last);
            Assert.That(game.State, Is.EqualTo(SudokuSessionState.Active));
            Assert.That(game.Board.IsComplete, Is.False);
            AssertMove(game.SetValue(last, definition.Solution.GetValue(last)), SudokuMoveStatus.Applied,
                true, false, 3, SudokuSessionState.Won, true);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void PreviousMistakesThenCorrectionDoNotPreventWinning(int mistakes)
        {
            session.SetValue(Editable, new SudokuValue(1));
            if (mistakes == 2)
                session.SetValue(Editable, new SudokuValue(2));
            session.SetValue(Editable, new SudokuValue(4));
            var result = FillCanonical(session);
            AssertMove(result, SudokuMoveStatus.Applied, true, false, 3 - mistakes, SudokuSessionState.Won, true);
            Assert.That(session.MistakeCount, Is.EqualTo(mistakes));
            Assert.That(session.Board.IsComplete, Is.True);
        }

        [Test]
        public void FullWrongBoardStaysActiveAndFinalCorrectionWins()
        {
            session.SetValue(Editable, new SudokuValue(1));
            FillCanonical(session, Editable);
            Assert.That(Enumerable.Range(0, 81).All(index =>
                session.Board.GetValue(new CellPosition(index / 9, index % 9)).HasValue), Is.True);
            Assert.That(session.State, Is.EqualTo(SudokuSessionState.Active));
            Assert.That(session.Board.IsComplete, Is.False);
            AssertMove(session.SetValue(Editable, new SudokuValue(4)), SudokuMoveStatus.Applied,
                true, false, 2, SudokuSessionState.Won, true);
        }

        [Test]
        public void MoveResultIsSnapshotOfThatAction()
        {
            var result = session.SetValue(Editable, new SudokuValue(1));
            session.SetValue(Editable, new SudokuValue(2));
            session.SetValue(Editable, new SudokuValue(5));
            AssertMove(result, SudokuMoveStatus.Applied, false, true, 2, SudokuSessionState.Active, false);
        }

        [Test]
        public void SessionsAreIndependentAndNeverMutateDefinition()
        {
            var definition = session.Definition;
            var other = new SudokuGameSession(definition, TimeSpan.FromSeconds(10));
            var before = Snapshot(other);
            session.AddNote(Peer, new SudokuValue(9));
            session.AdvanceTime(TimeSpan.FromSeconds(3));
            session.SetValue(Editable, new SudokuValue(1));
            session.ClearValue(Editable);
            FillCanonical(session);
            Assert.That(Snapshot(other), Is.EqualTo(before));
            Assert.That(session.Definition, Is.SameAs(definition));
            Assert.That(definition.PuzzleId, Is.EqualTo("session-fixture"));
            for (var index = 0; index < 81; index++)
            {
                var position = new CellPosition(index / 9, index % 9);
                Assert.That(definition.Puzzle.GetGiven(position)?.Value ?? 0,
                    Is.EqualTo(SudokuFixtures.ClassicPuzzle[index] - '0'));
                Assert.That(definition.Solution.GetValue(position).Value,
                    Is.EqualTo(SudokuFixtures.ClassicSolution[index] - '0'));
            }
        }

        [Test]
        public void NotesAcceptAllNineValuesInSortedOrderWithoutCheckingLegality()
        {
            for (var number = 9; number >= 1; number--)
                AssertMove(session.AddNote(Editable, new SudokuValue(number)), SudokuMoveStatus.Applied,
                    null, false, 3, SudokuSessionState.Active, false);
            Assert.That(session.GetNotes(Editable).Select(value => value.Value), Is.EqualTo(Enumerable.Range(1, 9)));
            Assert.That(session.Board.GetValue(Editable), Is.Null);
            Assert.That(session.MistakeCount, Is.Zero);
        }

        [Test]
        public void NotesAddRemoveAndToggleAreSetOperations()
        {
            var value = new SudokuValue(4);
            Assert.That(session.RemoveNote(Editable, value).Status, Is.EqualTo(SudokuMoveStatus.Unchanged));
            Assert.That(session.AddNote(Editable, value).Status, Is.EqualTo(SudokuMoveStatus.Applied));
            Assert.That(session.AddNote(Editable, value).Status, Is.EqualTo(SudokuMoveStatus.Unchanged));
            Assert.That(session.GetNotes(Editable), Is.EqualTo(new[] { value }));
            Assert.That(session.RemoveNote(Editable, value).Status, Is.EqualTo(SudokuMoveStatus.Applied));
            Assert.That(session.RemoveNote(Editable, value).Status, Is.EqualTo(SudokuMoveStatus.Unchanged));
            Assert.That(session.GetNotes(Editable), Is.Empty);
            Assert.That(session.ToggleNote(Editable, value).Status, Is.EqualTo(SudokuMoveStatus.Applied));
            Assert.That(session.GetNotes(Editable), Is.EqualTo(new[] { value }));
            Assert.That(session.ToggleNote(Editable, value).Status, Is.EqualTo(SudokuMoveStatus.Applied));
            Assert.That(session.GetNotes(Editable), Is.Empty);
            Assert.That(session.RemainingLives, Is.EqualTo(3));
        }

        [TestCase(1)]
        [TestCase(4)]
        public void SettingNormalValueClearsOnlyItsNotesAndClearingDoesNotRestoreThem(int number)
        {
            session.AddNote(Editable, new SudokuValue(1));
            session.AddNote(Editable, new SudokuValue(4));
            session.AddNote(Peer, new SudokuValue(number));
            session.SetValue(Editable, new SudokuValue(number));
            Assert.That(session.GetNotes(Editable), Is.Empty);
            Assert.That(session.GetNotes(Peer), Is.EqualTo(new[] { new SudokuValue(number) }));
            session.ClearValue(Editable);
            Assert.That(session.GetNotes(Editable), Is.Empty);
            Assert.That(session.GetNotes(Peer), Is.EqualTo(new[] { new SudokuValue(number) }));
            Assert.That(session.AddNote(Editable, new SudokuValue(2)).Status, Is.EqualTo(SudokuMoveStatus.Applied));
        }

        [Test]
        public void ClearingAlreadyEmptyCellPreservesItsNotes()
        {
            session.AddNote(Editable, new SudokuValue(4));
            Assert.That(session.ClearValue(Editable).Status, Is.EqualTo(SudokuMoveStatus.Unchanged));
            Assert.That(session.GetNotes(Editable), Is.EqualTo(new[] { new SudokuValue(4) }));
        }

        [TestCase(1)]
        [TestCase(4)]
        public void FilledCellRejectsAllNoteMutations(int number)
        {
            session.SetValue(Editable, new SudokuValue(number));
            var before = Snapshot(session);
            var value = new SudokuValue(9);
            var results = new[]
            {
                session.AddNote(Editable, value), session.RemoveNote(Editable, value), session.ToggleNote(Editable, value)
            };
            foreach (var result in results)
                AssertMove(result, SudokuMoveStatus.CellNotEmpty, null, false,
                    session.RemainingLives, SudokuSessionState.Active, false);
            Assert.That(Snapshot(session), Is.EqualTo(before));
        }

        [Test]
        public void NotesDoNotLeakMutableCollectionsOrChangeEarlierSnapshots()
        {
            var empty = session.GetNotes(Editable);
            Assert.Throws<NotSupportedException>(() => ((IList<SudokuValue>)empty).Add(new SudokuValue(1)));
            session.AddNote(Editable, new SudokuValue(4));
            var snapshot = session.GetNotes(Editable);
            var list = (IList<SudokuValue>)snapshot;
            Assert.That(list.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => list[0] = new SudokuValue(9));
            Assert.Throws<NotSupportedException>(() => list.Add(new SudokuValue(1)));
            Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => list.Clear());
            Assert.That(session.GetNotes(Editable), Is.EqualTo(new[] { new SudokuValue(4) }));
            session.ToggleNote(Editable, new SudokuValue(4));
            session.AddNote(Editable, new SudokuValue(9));
            session.SetValue(Editable, new SudokuValue(4));
            Assert.That(empty, Is.Empty);
            Assert.That(snapshot, Is.EqualTo(new[] { new SudokuValue(4) }));
            Assert.That(session.GetNotes(Editable), Is.Empty);
        }

        private static SudokuMoveResult[] MutateEveryWay(SudokuGameSession game, CellPosition position) => new[]
        {
            game.SetValue(position, new SudokuValue(1)), game.ClearValue(position),
            game.AddNote(position, new SudokuValue(9)), game.RemoveNote(position, new SudokuValue(9)),
            game.ToggleNote(position, new SudokuValue(9))
        };

        private static SudokuMoveResult FillCanonical(SudokuGameSession game, CellPosition? skip = null)
        {
            SudokuMoveResult last = null;
            for (var index = 0; index < 81; index++)
            {
                var position = new CellPosition(index / 9, index % 9);
                if (!game.Board.IsGiven(position) && position != skip)
                    last = game.SetValue(position, game.Definition.Solution.GetValue(position));
            }

            return last;
        }

        private static string Snapshot(SudokuGameSession game) =>
            string.Join("|", Enumerable.Range(0, 81).Select(index =>
            {
                var position = new CellPosition(index / 9, index % 9);
                return (game.Board.GetValue(position)?.Value ?? 0) + ":" +
                    string.Join(",", game.GetNotes(position).Select(value => value.Value));
            })) + $"/{game.State}/{game.RemainingLives}/{game.ElapsedTime.Ticks}/{game.RemainingBonusTime.Ticks}";

        private static void AssertMove(SudokuMoveResult result, SudokuMoveStatus status, bool? correct,
            bool lifeLost, int lives, SudokuSessionState state, bool stateChanged)
        {
            Assert.That(result.Status, Is.EqualTo(status));
            Assert.That(result.IsCorrect, Is.EqualTo(correct));
            Assert.That(result.LifeLost, Is.EqualTo(lifeLost));
            Assert.That(result.RemainingLives, Is.EqualTo(lives));
            Assert.That(result.State, Is.EqualTo(state));
            Assert.That(result.StateChanged, Is.EqualTo(stateChanged));
            Assert.That(result.PreviousState, Is.EqualTo(stateChanged ? SudokuSessionState.Active : state));
        }
    }
}
