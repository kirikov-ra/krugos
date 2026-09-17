using System;
using Krugos.Domain;
using NUnit.Framework;

namespace Krugos.Tests.EditMode
{
    public sealed class SudokuGameSessionTimeTests
    {
        private static readonly CellPosition Editable = new CellPosition(0, 2);
        private SudokuPuzzleDefinition definition;

        [OneTimeSetUp]
        public void SetUpDefinition()
        {
            definition = SudokuPuzzleDefinition.Create("time-fixture",
                SudokuPuzzle.Create(SudokuFixtures.Parse(SudokuFixtures.ClassicPuzzle)));
        }

        [Test]
        public void ZeroDurationDisablesOnlyBonusAndGameCanBeWon()
        {
            var session = new SudokuGameSession(definition, TimeSpan.Zero);
            AssertTime(session, 0, 0, false);
            Assert.That(session.State, Is.EqualTo(SudokuSessionState.Active));
            Assert.That(session.RemainingLives, Is.EqualTo(3));
            ReachTerminal(session, SudokuSessionState.Won);
            Assert.That(session.State, Is.EqualTo(SudokuSessionState.Won));
        }

        [Test]
        public void CountdownAndElapsedTimePreserveIndividualTicks()
        {
            var session = new SudokuGameSession(definition, TimeSpan.FromTicks(10));
            session.AdvanceTime(TimeSpan.FromTicks(3));
            AssertTime(session, 3, 7, true);
            session.AdvanceTime(TimeSpan.FromTicks(6));
            AssertTime(session, 9, 1, true);
            session.AdvanceTime(TimeSpan.FromTicks(1));
            AssertTime(session, 10, 0, false);
            Assert.That(session.State, Is.EqualTo(SudokuSessionState.Active));
            Assert.That(session.RemainingLives, Is.EqualTo(3));
        }

        [Test]
        public void OvershootClampsBonusWhileElapsedTimeIncludesWholeDelta()
        {
            var session = new SudokuGameSession(definition, TimeSpan.FromTicks(10));
            session.AdvanceTime(TimeSpan.FromTicks(11));
            AssertTime(session, 11, 0, false);
            session.AdvanceTime(TimeSpan.FromTicks(7));
            AssertTime(session, 18, 0, false);
            Assert.That(session.State, Is.EqualTo(SudokuSessionState.Active));
            Assert.That(session.RemainingLives, Is.EqualTo(3));
        }

        [Test]
        public void ZeroDeltaChangesNothingBeforeAndAfterExpiry()
        {
            var session = new SudokuGameSession(definition, TimeSpan.FromTicks(10));
            session.AdvanceTime(TimeSpan.Zero);
            AssertTime(session, 0, 10, true);
            session.AdvanceTime(TimeSpan.FromTicks(11));
            session.AdvanceTime(TimeSpan.Zero);
            AssertTime(session, 11, 0, false);
        }

        [Test]
        public void ExpiryPreservesBoardNotesAndLivesAndAllowsContinuedPlay()
        {
            var session = new SudokuGameSession(definition, TimeSpan.FromTicks(10));
            var peer = new CellPosition(0, 3);
            session.SetValue(Editable, new SudokuValue(1));
            session.AddNote(peer, new SudokuValue(6));
            session.AdvanceTime(TimeSpan.FromTicks(20));
            AssertTime(session, 20, 0, false);
            Assert.That(session.Board.GetValue(Editable)?.Value, Is.EqualTo(1));
            Assert.That(session.GetNotes(peer), Is.EqualTo(new[] { new SudokuValue(6) }));
            Assert.That(session.RemainingLives, Is.EqualTo(2));
            Assert.That(session.MistakeCount, Is.EqualTo(1));
            Assert.That(session.State, Is.EqualTo(SudokuSessionState.Active));
            Assert.That(session.RemoveNote(peer, new SudokuValue(6)).Status, Is.EqualTo(SudokuMoveStatus.Applied));
            Assert.That(session.ToggleNote(peer, new SudokuValue(2)).Status, Is.EqualTo(SudokuMoveStatus.Applied));
            Assert.That(session.ClearValue(Editable).Status, Is.EqualTo(SudokuMoveStatus.Applied));
            ReachTerminal(session, SudokuSessionState.Won);
            Assert.That(session.State, Is.EqualTo(SudokuSessionState.Won));
            Assert.That(session.RemainingLives, Is.EqualTo(2));
            AssertTime(session, 20, 0, false);
        }

        [Test]
        public void WrongMoveAfterExpiryStillCostsOneLife()
        {
            var session = new SudokuGameSession(definition, TimeSpan.FromTicks(10));
            session.AdvanceTime(TimeSpan.FromTicks(10));
            var result = session.SetValue(Editable, new SudokuValue(1));
            Assert.That(result.LifeLost, Is.True);
            Assert.That(result.RemainingLives, Is.EqualTo(2));
            Assert.That(result.State, Is.EqualTo(SudokuSessionState.Active));
            AssertTime(session, 10, 0, false);
        }

        [TestCase(SudokuSessionState.Active, -1L)]
        [TestCase(SudokuSessionState.Active, long.MinValue)]
        [TestCase(SudokuSessionState.Won, -1L)]
        [TestCase(SudokuSessionState.Won, long.MinValue)]
        [TestCase(SudokuSessionState.Lost, -1L)]
        [TestCase(SudokuSessionState.Lost, long.MinValue)]
        public void NegativeDeltaIsRejectedInEveryStateWithoutTimeMutation(SudokuSessionState state, long ticks)
        {
            var session = new SudokuGameSession(definition, TimeSpan.FromTicks(10));
            session.AdvanceTime(TimeSpan.FromTicks(2));
            ReachTerminal(session, state);
            var lives = session.RemainingLives;
            Assert.Throws<ArgumentOutOfRangeException>(() => session.AdvanceTime(TimeSpan.FromTicks(ticks)));
            AssertTime(session, 2, 8, true);
            Assert.That(session.State, Is.EqualTo(state));
            Assert.That(session.RemainingLives, Is.EqualTo(lives));
        }

        [TestCase(SudokuSessionState.Won, 2L)]
        [TestCase(SudokuSessionState.Won, 12L)]
        [TestCase(SudokuSessionState.Lost, 2L)]
        [TestCase(SudokuSessionState.Lost, 12L)]
        public void TerminalStateFreezesBothTimesAndBonusFlag(SudokuSessionState state, long elapsed)
        {
            var session = new SudokuGameSession(definition, TimeSpan.FromTicks(10));
            session.AdvanceTime(TimeSpan.FromTicks(elapsed));
            ReachTerminal(session, state);
            var lives = session.RemainingLives;
            session.AdvanceTime(TimeSpan.Zero);
            session.AdvanceTime(TimeSpan.FromTicks(1));
            session.AdvanceTime(TimeSpan.MaxValue);
            AssertTime(session, elapsed, Math.Max(0, 10 - elapsed), elapsed < 10);
            Assert.That(session.State, Is.EqualTo(state));
            Assert.That(session.RemainingLives, Is.EqualTo(lives));
        }

        [Test]
        public void MaximumDurationAndDeltaDoNotWrapOrUnderflow()
        {
            var session = new SudokuGameSession(definition, TimeSpan.MaxValue);
            session.AdvanceTime(TimeSpan.FromTicks(long.MaxValue - 1));
            AssertTime(session, long.MaxValue - 1, 1, true);
            session.AdvanceTime(TimeSpan.FromTicks(1));
            AssertTime(session, long.MaxValue, 0, false);
            session.AdvanceTime(TimeSpan.Zero);
            AssertTime(session, long.MaxValue, 0, false);
        }

        [Test]
        public void MaximumDeltaClampsSmallCountdown()
        {
            var session = new SudokuGameSession(definition, TimeSpan.FromTicks(1));
            session.AdvanceTime(TimeSpan.MaxValue);
            AssertTime(session, long.MaxValue, 0, false);
        }

        [Test]
        public void ElapsedOverflowIsRejectedBeforeEitherTimeChanges()
        {
            var session = new SudokuGameSession(definition, TimeSpan.MaxValue);
            session.AdvanceTime(TimeSpan.FromTicks(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => session.AdvanceTime(TimeSpan.MaxValue));
            AssertTime(session, 1, long.MaxValue - 1, true);
            Assert.That(session.State, Is.EqualTo(SudokuSessionState.Active));
            Assert.That(session.RemainingLives, Is.EqualTo(3));
            session.AdvanceTime(TimeSpan.FromTicks(long.MaxValue - 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => session.AdvanceTime(TimeSpan.FromTicks(1)));
            AssertTime(session, long.MaxValue, 0, false);
        }

        [Test]
        public void TimeAdvancementIsIndependentOfDeltaPartitioning()
        {
            var first = new SudokuGameSession(definition, TimeSpan.FromTicks(10));
            var second = new SudokuGameSession(definition, TimeSpan.FromTicks(10));
            first.AdvanceTime(TimeSpan.FromTicks(21));
            foreach (var ticks in new long[] { 0, 1, 2, 3, 4, 5, 6 })
                second.AdvanceTime(TimeSpan.FromTicks(ticks));
            Assert.That(first.ElapsedTime, Is.EqualTo(second.ElapsedTime));
            Assert.That(first.RemainingBonusTime, Is.EqualTo(second.RemainingBonusTime));
            Assert.That(first.IsSpeedBonusAvailable, Is.EqualTo(second.IsSpeedBonusAvailable));
        }

        private static void ReachTerminal(SudokuGameSession session, SudokuSessionState state)
        {
            if (state == SudokuSessionState.Lost)
            {
                session.SetValue(Editable, new SudokuValue(1));
                session.SetValue(Editable, new SudokuValue(2));
                session.SetValue(Editable, new SudokuValue(5));
            }
            else if (state == SudokuSessionState.Won)
            {
                for (var index = 0; index < 81; index++)
                {
                    var position = new CellPosition(index / 9, index % 9);
                    if (!session.Board.IsGiven(position))
                        session.SetValue(position, session.Definition.Solution.GetValue(position));
                }
            }
        }

        private static void AssertTime(SudokuGameSession session, long elapsed, long remaining, bool bonus)
        {
            Assert.That(session.ElapsedTime, Is.EqualTo(TimeSpan.FromTicks(elapsed)));
            Assert.That(session.RemainingBonusTime, Is.EqualTo(TimeSpan.FromTicks(remaining)));
            Assert.That(session.IsSpeedBonusAvailable, Is.EqualTo(bonus));
        }
    }
}
