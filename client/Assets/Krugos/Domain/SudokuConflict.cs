using System;

namespace Krugos.Domain
{
    [Flags]
    public enum SudokuConflict
    {
        None = 0,
        Row = 1,
        Column = 2,
        Box = 4
    }
}
