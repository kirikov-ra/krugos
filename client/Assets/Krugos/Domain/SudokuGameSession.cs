using System;
using System.Collections.Generic;

namespace Krugos.Domain
{
    public sealed class SudokuGameSession
    {
        public const int InitialLives = 3;

        private readonly SudokuBoard board;
        private readonly int[] notes = new int[81];

        public SudokuPuzzleDefinition Definition { get; }
        public SudokuBoardView Board { get; }
        public SudokuSessionState State { get; private set; }
        public int RemainingLives { get; private set; } = InitialLives;
        public int MistakeCount => InitialLives - RemainingLives;
        public TimeSpan ElapsedTime { get; private set; }
        public TimeSpan RemainingBonusTime { get; private set; }
        public bool IsSpeedBonusAvailable => RemainingBonusTime > TimeSpan.Zero;

        public SudokuGameSession(SudokuPuzzleDefinition definition, TimeSpan bonusDuration)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (bonusDuration < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(bonusDuration), "Bonus duration cannot be negative.");

            board = new SudokuBoard(definition.Puzzle);
            Board = new SudokuBoardView(board);
            RemainingBonusTime = bonusDuration;
            State = IsCanonicalComplete() ? SudokuSessionState.Won : SudokuSessionState.Active;
        }

        public SudokuMoveResult SetValue(CellPosition position, SudokuValue value)
        {
            var rejection = GetRejection(position);
            if (rejection.HasValue)
                return Result(rejection.Value);

            var isCorrect = value == Definition.Solution.GetValue(position);
            if (board.GetValue(position) == value)
                return Result(SudokuMoveStatus.Unchanged, isCorrect);

            var previousState = State;
            board.SetValue(position, value);
            notes[position.Index] = 0;
            if (!isCorrect)
                RemainingLives--;

            if (RemainingLives == 0)
                State = SudokuSessionState.Lost;
            else if (IsCanonicalComplete())
                State = SudokuSessionState.Won;

            return new SudokuMoveResult(SudokuMoveStatus.Applied, isCorrect, !isCorrect,
                RemainingLives, previousState, State);
        }

        public SudokuMoveResult ClearValue(CellPosition position)
        {
            var rejection = GetRejection(position);
            if (rejection.HasValue)
                return Result(rejection.Value);
            if (!board.GetValue(position).HasValue)
                return Result(SudokuMoveStatus.Unchanged);

            board.ClearValue(position);
            return Result(SudokuMoveStatus.Applied);
        }

        public SudokuMoveResult AddNote(CellPosition position, SudokuValue value) =>
            ChangeNote(position, value, true);

        public SudokuMoveResult RemoveNote(CellPosition position, SudokuValue value) =>
            ChangeNote(position, value, false);

        public SudokuMoveResult ToggleNote(CellPosition position, SudokuValue value) =>
            ChangeNote(position, value, null);

        public IReadOnlyList<SudokuValue> GetNotes(CellPosition position)
        {
            var values = new List<SudokuValue>();
            for (var number = 1; number <= 9; number++)
            {
                if ((notes[position.Index] & (1 << (number - 1))) != 0)
                    values.Add(new SudokuValue(number));
            }

            return values.AsReadOnly();
        }

        public void AdvanceTime(TimeSpan delta)
        {
            if (delta < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(delta), "Time advancement cannot be negative.");
            if (State != SudokuSessionState.Active)
                return;
            if (delta > TimeSpan.MaxValue - ElapsedTime)
                throw new ArgumentOutOfRangeException(nameof(delta), "Elapsed time must fit in TimeSpan.");

            ElapsedTime += delta;
            RemainingBonusTime = delta >= RemainingBonusTime ? TimeSpan.Zero : RemainingBonusTime - delta;
        }

        private SudokuMoveResult ChangeNote(CellPosition position, SudokuValue value, bool? present)
        {
            var rejection = GetRejection(position);
            if (rejection.HasValue)
                return Result(rejection.Value);
            if (board.GetValue(position).HasValue)
                return Result(SudokuMoveStatus.CellNotEmpty);

            var mask = 1 << (value.Value - 1);
            var previous = notes[position.Index];
            notes[position.Index] = !present.HasValue ? previous ^ mask
                : present.Value ? previous | mask : previous & ~mask;
            return Result(previous == notes[position.Index] ? SudokuMoveStatus.Unchanged : SudokuMoveStatus.Applied);
        }

        private SudokuMoveStatus? GetRejection(CellPosition position)
        {
            if (State != SudokuSessionState.Active)
                return SudokuMoveStatus.SessionEnded;
            return board.IsGiven(position) ? SudokuMoveStatus.GivenCell : (SudokuMoveStatus?)null;
        }

        private SudokuMoveResult Result(SudokuMoveStatus status, bool? isCorrect = null) =>
            new SudokuMoveResult(status, isCorrect, false, RemainingLives, State, State);

        private bool IsCanonicalComplete()
        {
            if (!board.IsComplete)
                return false;
            for (var index = 0; index < 81; index++)
            {
                var position = new CellPosition(index / 9, index % 9);
                if (board.GetValue(position) != Definition.Solution.GetValue(position))
                    return false;
            }

            return true;
        }
    }
}
