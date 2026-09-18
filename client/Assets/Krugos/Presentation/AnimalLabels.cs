using System;
using Krugos.Application;

namespace Krugos.Presentation
{
    public static class AnimalLabels
    {
        public static string Full(AnimalId animal) =>
            (animal ?? throw new ArgumentNullException(nameof(animal))).Value.ToUpperInvariant();

        public static string Compact(AnimalId animal) => Shorten(animal, 3);
        public static string Note(AnimalId animal) => Shorten(animal, 2);

        private static string Shorten(AnimalId animal, int length)
        {
            var label = Full(animal);
            return label.Substring(0, Math.Min(length, label.Length));
        }
    }
}
