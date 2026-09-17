namespace Krugos.Domain
{
    public sealed class SudokuMoveResult
    {
        public SudokuMoveStatus Status { get; }
        public bool? IsCorrect { get; }
        public bool LifeLost { get; }
        public int RemainingLives { get; }
        public SudokuSessionState PreviousState { get; }
        public SudokuSessionState State { get; }
        public bool StateChanged => PreviousState != State;

        internal SudokuMoveResult(SudokuMoveStatus status, bool? isCorrect, bool lifeLost,
            int remainingLives, SudokuSessionState previousState, SudokuSessionState state)
        {
            Status = status;
            IsCorrect = isCorrect;
            LifeLost = lifeLost;
            RemainingLives = remainingLives;
            PreviousState = previousState;
            State = state;
        }
    }
}
