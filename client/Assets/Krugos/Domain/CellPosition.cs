using System;

namespace Krugos.Domain
{
    public readonly struct CellPosition : IEquatable<CellPosition>
    {
        public int Row { get; }
        public int Column { get; }

        public CellPosition(int row, int column)
        {
            if (row < 0 || row >= 9)
                throw new ArgumentOutOfRangeException(nameof(row), row, "Row must be between 0 and 8.");
            if (column < 0 || column >= 9)
                throw new ArgumentOutOfRangeException(nameof(column), column, "Column must be between 0 and 8.");

            Row = row;
            Column = column;
        }

        internal int Index => Row * 9 + Column;

        public bool Equals(CellPosition other) => Row == other.Row && Column == other.Column;
        public override bool Equals(object obj) => obj is CellPosition other && Equals(other);
        public override int GetHashCode() => Index;
        public static bool operator ==(CellPosition left, CellPosition right) => left.Equals(right);
        public static bool operator !=(CellPosition left, CellPosition right) => !left.Equals(right);
    }
}
