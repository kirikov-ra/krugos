namespace Krugos.Domain
{
    public enum SudokuMoveStatus
    {
        Applied,
        Unchanged,
        GivenCell,
        CellNotEmpty,
        SessionEnded
    }
}
