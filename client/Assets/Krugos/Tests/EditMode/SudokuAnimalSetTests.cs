using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Krugos.Application;
using Krugos.Domain;
using Krugos.Infrastructure;
using NUnit.Framework;

namespace Krugos.Tests.EditMode
{
    public sealed class SudokuAnimalSetTests
    {
        private static AnimalId[] CreateAnimals() => new[]
        {
            "fox", "panda", "elephant", "giraffe", "lion", "zebra", "monkey", "hippo", "tiger"
        }.Select(id => new AnimalId(id)).ToArray();

        [Test]
        public void CreatesExactlyNineOrderedAnimals()
        {
            var input = CreateAnimals();
            var set = new SudokuAnimalSet(input);
            Assert.That(set.Animals.Count, Is.EqualTo(9));
            Assert.That(set.Animals, Is.EqualTo(input));
        }

        [Test]
        public void RejectsNullList()
        {
            Assert.Throws<ArgumentNullException>(() => new SudokuAnimalSet(null));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(8)]
        [TestCase(10)]
        [TestCase(18)]
        public void RejectsIncorrectCount(int count)
        {
            var animals = Enumerable.Range(1, count).Select(index => new AnimalId("animal-" + index)).ToArray();
            Assert.Throws<ArgumentException>(() => new SudokuAnimalSet(animals));
        }

        [TestCase(0)]
        [TestCase(4)]
        [TestCase(8)]
        public void RejectsNullElement(int index)
        {
            var animals = CreateAnimals();
            animals[index] = null;
            Assert.Throws<ArgumentException>(() => new SudokuAnimalSet(animals));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void RejectsDuplicateByIdentifier(bool sameReference)
        {
            var animals = CreateAnimals();
            animals[8] = sameReference ? animals[0] : new AnimalId("fox");
            Assert.Throws<ArgumentException>(() => new SudokuAnimalSet(animals));
        }

        [Test]
        public void UniquenessAndReverseLookupUseCaseSensitiveOrdinalEquality()
        {
            var animals = CreateAnimals();
            animals[8] = new AnimalId("FOX");
            var set = new SudokuAnimalSet(animals);
            Assert.That(set.GetValue(new AnimalId("fox")), Is.EqualTo(new SudokuValue(1)));
            Assert.That(set.GetValue(new AnimalId("FOX")), Is.EqualTo(new SudokuValue(9)));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        public void MapsBothDirectionsUsingInputOrderAndIdentifierEquality(int number)
        {
            var animals = CreateAnimals();
            var set = new SudokuAnimalSet(animals);
            var value = new SudokuValue(number);
            Assert.That(set.GetAnimal(value), Is.EqualTo(animals[number - 1]));
            Assert.That(set.GetValue(new AnimalId(animals[number - 1].Value)), Is.EqualTo(value));
            Assert.That(set.GetValue(set.GetAnimal(value)), Is.EqualTo(value));
        }

        [Test]
        public void DefaultSudokuValueMapsToFirstAnimal()
        {
            Assert.That(new SudokuAnimalSet(CreateAnimals()).GetAnimal(default).Value, Is.EqualTo("fox"));
        }

        [Test]
        public void RejectsNullReverseLookup()
        {
            var set = new SudokuAnimalSet(CreateAnimals());
            Assert.Throws<ArgumentNullException>(() => set.GetValue(null));
        }

        [TestCase("otter")]
        [TestCase("FOX")]
        public void RejectsAnimalOutsideSet(string id)
        {
            var set = new SudokuAnimalSet(CreateAnimals());
            Assert.Throws<ArgumentException>(() => set.GetValue(new AnimalId(id)));
        }

        [Test]
        public void MutatingInputListCannotChangeEitherMappingOrSnapshot()
        {
            var input = CreateAnimals().ToList();
            var expected = input.ToArray();
            var set = new SudokuAnimalSet(input);
            var snapshot = set.Animals;
            input[0] = new AnimalId("otter");
            input.Reverse();
            input.Clear();
            AssertMapping(set, expected);
            Assert.That(snapshot, Is.EqualTo(expected));
        }

        [Test]
        public void MutatingInputArrayCannotChangeSet()
        {
            var input = CreateAnimals();
            var expected = input.ToArray();
            var set = new SudokuAnimalSet(input);
            Array.Clear(input, 0, input.Length);
            AssertMapping(set, expected);
        }

        [Test]
        public void SnapshotRejectsGenericCollectionMutations()
        {
            var expected = CreateAnimals();
            var set = new SudokuAnimalSet(expected);
            var list = (IList<AnimalId>)set.Animals;
            var replacement = new AnimalId("otter");
            Assert.That(list.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => list[0] = replacement);
            Assert.Throws<NotSupportedException>(() => list.Add(replacement));
            Assert.Throws<NotSupportedException>(() => list.Insert(0, replacement));
            Assert.Throws<NotSupportedException>(() => list.Remove(expected[0]));
            Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => list.Clear());
            AssertMapping(set, expected);
        }

        [Test]
        public void SnapshotRejectsNonGenericCollectionMutations()
        {
            var expected = CreateAnimals();
            var set = new SudokuAnimalSet(expected);
            var list = (IList)set.Animals;
            var replacement = new AnimalId("otter");
            Assert.That(list.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => list[0] = replacement);
            Assert.Throws<NotSupportedException>(() => list.Add(replacement));
            Assert.Throws<NotSupportedException>(() => list.Insert(0, replacement));
            Assert.Throws<NotSupportedException>(() => list.Remove(expected[0]));
            Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => list.Clear());
            AssertMapping(set, expected);
        }

        [Test]
        public void NewOrderAndReplacementLeaveOriginalSetAndSnapshotUnchanged()
        {
            var expected = CreateAnimals();
            var original = new SudokuAnimalSet(expected);
            var snapshot = original.Animals;
            var reordered = new SudokuAnimalSet(snapshot.Reverse().ToArray());
            var replacement = snapshot.ToArray();
            replacement[0] = new AnimalId("otter");
            var replaced = new SudokuAnimalSet(replacement);
            AssertMapping(reordered, expected.Reverse().ToArray());
            AssertMapping(replaced, replacement);
            AssertMapping(original, expected);
            Assert.That(snapshot, Is.EqualTo(expected));
        }

        [Test]
        public void BuiltInDevelopmentSetHasExactStableOrderAndNineUniqueIds()
        {
            var set = BuiltInSudokuAnimalSet.Default;
            AssertMapping(set, CreateAnimals());
            Assert.That(set.Animals.Distinct().Count(), Is.EqualTo(9));
            AssertMapping(BuiltInSudokuAnimalSet.Default, CreateAnimals());
        }

        private static void AssertMapping(SudokuAnimalSet set, AnimalId[] expected)
        {
            Assert.That(set.Animals, Is.EqualTo(expected));
            for (var index = 0; index < 9; index++)
            {
                var value = new SudokuValue(index + 1);
                Assert.That(set.GetAnimal(value), Is.EqualTo(expected[index]));
                Assert.That(set.GetValue(new AnimalId(expected[index].Value)), Is.EqualTo(value));
            }
        }
    }
}
