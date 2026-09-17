using System;

namespace Krugos.Domain
{
    public readonly struct SudokuValue : IEquatable<SudokuValue>
    {
        // Zero-based storage keeps default(SudokuValue) valid: it represents 1.
        private readonly byte offset;

        public int Value => offset + 1;

        public SudokuValue(int value)
        {
            if (value < 1 || value > 9)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Sudoku value must be between 1 and 9.");

            offset = (byte)(value - 1);
        }

        public bool Equals(SudokuValue other) => offset == other.offset;
        public override bool Equals(object obj) => obj is SudokuValue other && Equals(other);
        public override int GetHashCode() => Value;
        public static bool operator ==(SudokuValue left, SudokuValue right) => left.Equals(right);
        public static bool operator !=(SudokuValue left, SudokuValue right) => !left.Equals(right);
    }
}
