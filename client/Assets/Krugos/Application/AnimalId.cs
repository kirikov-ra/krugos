using System;

namespace Krugos.Application
{
    public sealed class AnimalId : IEquatable<AnimalId>
    {
        public string Value { get; }

        public AnimalId(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Animal ID cannot be empty or whitespace.", nameof(value));

            Value = value;
        }

        public bool Equals(AnimalId other) =>
            !ReferenceEquals(other, null) && StringComparer.Ordinal.Equals(Value, other.Value);

        public override bool Equals(object obj) => obj is AnimalId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;

        public static bool operator ==(AnimalId left, AnimalId right) =>
            ReferenceEquals(left, right) || (!ReferenceEquals(left, null) && left.Equals(right));

        public static bool operator !=(AnimalId left, AnimalId right) => !(left == right);
    }
}
