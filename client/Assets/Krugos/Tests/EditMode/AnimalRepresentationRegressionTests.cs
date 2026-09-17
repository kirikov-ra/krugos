using System;
using System.Collections.Generic;
using System.Linq;
using Krugos.Application;
using Krugos.Domain;
using Krugos.Infrastructure;
using NUnit.Framework;

namespace Krugos.Tests.EditMode
{
    public sealed class AnimalRepresentationRegressionTests
    {
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void DifferentRepresentationsPreserveDefinitionAndAllSessionRules(bool replaceAnimals, bool win)
        {
            var definition = SudokuPuzzleDefinition.Create("animal-regression",
                SudokuPuzzle.Create(SudokuFixtures.Parse(SudokuFixtures.ClassicPuzzle)));
            var givens = ReadGivens(definition.Puzzle);
            var solution = ReadSolution(definition.Solution);
            var firstSet = BuiltInSudokuAnimalSet.Default;
            var secondSet = new SudokuAnimalSet(replaceAnimals
                ? new[] { "otter", "wolf", "bear", "deer", "koala", "rabbit", "lynx", "yak", "moose" }
                    .Select(id => new AnimalId(id)).ToArray()
                : firstSet.Animals.Skip(1).Concat(firstSet.Animals.Take(1)).ToArray());
            var first = new SudokuGameSession(definition, TimeSpan.FromSeconds(10));
            var second = new SudokuGameSession(definition, TimeSpan.FromSeconds(10));
            var editable = new CellPosition(0, 2);
            var given = new CellPosition(0, 0);
            var wrong = new SudokuValue(1);
            var correct = definition.Solution.GetValue(editable);

            Assert.That(first.Definition, Is.SameAs(definition));
            Assert.That(second.Definition, Is.SameAs(definition));
            Assert.That(ReadGivens(first.Definition.Puzzle), Is.EqualTo(givens));
            Assert.That(ReadGivens(second.Definition.Puzzle), Is.EqualTo(givens));
            Assert.That(ReadSolution(first.Definition.Solution), Is.EqualTo(solution));
            Assert.That(ReadSolution(second.Definition.Solution), Is.EqualTo(solution));
            Assert.That(ReadState(first), Is.EqualTo(ReadState(second)));
            for (var number = 1; number <= 9; number++)
            {
                var value = new SudokuValue(number);
                Assert.That(firstSet.GetAnimal(value), Is.Not.EqualTo(secondSet.GetAnimal(value)));
                Assert.That(firstSet.GetValue(firstSet.GetAnimal(value)), Is.EqualTo(value));
                Assert.That(secondSet.GetValue(secondSet.GetAnimal(value)), Is.EqualTo(value));
            }
            AssertReadOnlyRepresentation(first, firstSet, secondSet);

            SudokuMoveResult ApplyBoth(Func<SudokuGameSession, SudokuAnimalSet, SudokuMoveResult> move)
            {
                var left = move(first, firstSet);
                var right = move(second, secondSet);
                Assert.That(right.Status, Is.EqualTo(left.Status));
                Assert.That(right.IsCorrect, Is.EqualTo(left.IsCorrect));
                Assert.That(right.LifeLost, Is.EqualTo(left.LifeLost));
                Assert.That(right.RemainingLives, Is.EqualTo(left.RemainingLives));
                Assert.That(right.PreviousState, Is.EqualTo(left.PreviousState));
                Assert.That(right.State, Is.EqualTo(left.State));
                Assert.That(right.StateChanged, Is.EqualTo(left.StateChanged));
                Assert.That(ReadState(second), Is.EqualTo(ReadState(first)));
                return left;
            }

            SudokuMoveResult SetBoth(CellPosition position, SudokuValue value) =>
                ApplyBoth((session, set) => session.SetValue(position, set.GetValue(set.GetAnimal(value))));

            Assert.That(SetBoth(given, wrong).Status, Is.EqualTo(SudokuMoveStatus.GivenCell));
            Assert.That(ApplyBoth((session, set) => session.AddNote(editable,
                set.GetValue(set.GetAnimal(correct)))).Status, Is.EqualTo(SudokuMoveStatus.Applied));
            Assert.That(ApplyBoth((session, set) => session.ToggleNote(editable,
                set.GetValue(set.GetAnimal(wrong)))).Status, Is.EqualTo(SudokuMoveStatus.Applied));
            AssertReadOnlyRepresentation(first, firstSet, secondSet);
            Assert.That(ApplyBoth((session, set) => session.RemoveNote(editable,
                set.GetValue(set.GetAnimal(wrong)))).Status, Is.EqualTo(SudokuMoveStatus.Applied));

            Assert.That(first.Board.IsPlacementValid(editable, wrong), Is.True);
            Assert.That(second.Board.IsPlacementValid(editable, wrong), Is.True);
            Assert.That(SetBoth(editable, wrong).LifeLost, Is.True);
            Assert.That(first.RemainingLives, Is.EqualTo(2));
            Assert.That(first.GetNotes(editable), Is.Empty);
            Assert.That(SetBoth(editable, wrong).Status, Is.EqualTo(SudokuMoveStatus.Unchanged));
            AssertReadOnlyRepresentation(first, firstSet, secondSet);
            Assert.That(ApplyBoth((session, set) => session.ClearValue(editable)).Status,
                Is.EqualTo(SudokuMoveStatus.Applied));
            Assert.That(SetBoth(editable, correct).IsCorrect, Is.True);
            Assert.That(first.RemainingLives, Is.EqualTo(2));

            first.AdvanceTime(TimeSpan.FromSeconds(11));
            second.AdvanceTime(TimeSpan.FromSeconds(11));
            Assert.That(ReadState(first), Is.EqualTo(ReadState(second)));
            Assert.That(first.ElapsedTime, Is.EqualTo(TimeSpan.FromSeconds(11)));
            Assert.That(first.IsSpeedBonusAvailable, Is.False);
            Assert.That(first.State, Is.EqualTo(SudokuSessionState.Active));

            if (win)
            {
                for (var index = 0; index < 81; index++)
                {
                    var position = new CellPosition(index / 9, index % 9);
                    if (!first.Board.IsGiven(position) && !first.Board.GetValue(position).HasValue)
                        Assert.That(SetBoth(position, definition.Solution.GetValue(position)).IsCorrect, Is.True);
                }
                Assert.That(first.State, Is.EqualTo(SudokuSessionState.Won));
                Assert.That(first.Board.IsComplete, Is.True);
                Assert.That(first.RemainingLives, Is.EqualTo(2));
            }
            else
            {
                Assert.That(SetBoth(editable, new SudokuValue(2)).LifeLost, Is.True);
                Assert.That(SetBoth(editable, new SudokuValue(3)).LifeLost, Is.True);
                Assert.That(first.State, Is.EqualTo(SudokuSessionState.Lost));
                Assert.That(first.RemainingLives, Is.Zero);
            }

            AssertReadOnlyRepresentation(first, firstSet, secondSet);
            var terminalState = ReadState(first);
            Assert.That(SetBoth(editable, correct).Status, Is.EqualTo(SudokuMoveStatus.SessionEnded));
            Assert.That(ApplyBoth((session, set) => session.ClearValue(editable)).Status,
                Is.EqualTo(SudokuMoveStatus.SessionEnded));
            Assert.That(ApplyBoth((session, set) => session.AddNote(editable,
                set.GetValue(set.GetAnimal(wrong)))).Status, Is.EqualTo(SudokuMoveStatus.SessionEnded));
            first.AdvanceTime(TimeSpan.FromSeconds(1));
            second.AdvanceTime(TimeSpan.FromSeconds(1));
            Assert.That(ReadState(first), Is.EqualTo(terminalState));
            Assert.That(ReadState(second), Is.EqualTo(terminalState));

            Assert.That(ReadGivens(definition.Puzzle), Is.EqualTo(givens));
            Assert.That(ReadSolution(definition.Solution), Is.EqualTo(solution));
            Assert.That(SudokuSolver.CountSolutions(definition.Puzzle, 2), Is.EqualTo(1));
            Assert.That(SudokuSolver.TrySolve(definition.Puzzle, out var solvedAgain), Is.True);
            Assert.That(ReadSolution(solvedAgain), Is.EqualTo(solution));
        }

        private static void AssertReadOnlyRepresentation(SudokuGameSession session,
            SudokuAnimalSet first, SudokuAnimalSet second)
        {
            var before = ReadState(session);
            for (var index = 0; index < 81; index++)
            {
                var position = new CellPosition(index / 9, index % 9);
                var value = session.Board.GetValue(position);
                if (value.HasValue)
                    Assert.That(first.GetAnimal(value.Value), Is.Not.EqualTo(second.GetAnimal(value.Value)));
                foreach (var note in session.GetNotes(position))
                    Assert.That(first.GetAnimal(note), Is.Not.EqualTo(second.GetAnimal(note)));
            }
            Assert.That(ReadState(session), Is.EqualTo(before));
        }

        private static object[] ReadState(SudokuGameSession session)
        {
            var state = new List<object>
            {
                session.State, session.RemainingLives, session.MistakeCount,
                session.ElapsedTime, session.RemainingBonusTime, session.IsSpeedBonusAvailable,
                session.Board.IsComplete
            };
            for (var index = 0; index < 81; index++)
            {
                var position = new CellPosition(index / 9, index % 9);
                state.Add(session.Board.GetValue(position));
                state.Add(session.Board.IsGiven(position));
                state.Add(session.Board.GetConflicts(position));
                state.Add(session.Board.GetCandidates(position).ToArray());
                state.Add(session.GetNotes(position).ToArray());
            }
            return state.ToArray();
        }

        private static SudokuValue?[] ReadGivens(SudokuPuzzle puzzle) => Enumerable.Range(0, 81)
            .Select(index => puzzle.GetGiven(new CellPosition(index / 9, index % 9))).ToArray();

        private static SudokuValue[] ReadSolution(SudokuSolution solution) => Enumerable.Range(0, 81)
            .Select(index => solution.GetValue(new CellPosition(index / 9, index % 9))).ToArray();
    }
}
