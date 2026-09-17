using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Krugos.Domain;

namespace Krugos.Application
{
    public sealed class SudokuAnimalSet
    {
        private readonly ReadOnlyCollection<AnimalId> animals;

        public IReadOnlyList<AnimalId> Animals => animals;

        public SudokuAnimalSet(IReadOnlyList<AnimalId> orderedAnimals)
        {
            if (orderedAnimals == null)
                throw new ArgumentNullException(nameof(orderedAnimals));
            if (orderedAnimals.Count != 9)
                throw new ArgumentException("An animal set must contain exactly nine animals.", nameof(orderedAnimals));

            var copy = new AnimalId[9];
            var unique = new HashSet<AnimalId>();
            for (var index = 0; index < copy.Length; index++)
            {
                var animal = orderedAnimals[index];
                if (animal == null)
                    throw new ArgumentException("An animal set cannot contain null IDs.", nameof(orderedAnimals));
                if (!unique.Add(animal))
                    throw new ArgumentException("An animal set must contain unique IDs.", nameof(orderedAnimals));

                copy[index] = animal;
            }

            animals = Array.AsReadOnly(copy);
        }

        public AnimalId GetAnimal(SudokuValue value) => animals[value.Value - 1];

        public SudokuValue GetValue(AnimalId animal)
        {
            if (animal == null)
                throw new ArgumentNullException(nameof(animal));

            var index = animals.IndexOf(animal);
            if (index < 0)
                throw new ArgumentException("Animal ID does not belong to this set.", nameof(animal));

            return new SudokuValue(index + 1);
        }
    }
}
