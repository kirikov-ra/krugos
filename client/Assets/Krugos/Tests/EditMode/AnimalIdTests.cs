using System;
using System.Collections.Generic;
using Krugos.Application;
using NUnit.Framework;

namespace Krugos.Tests.EditMode
{
    public sealed class AnimalIdTests
    {
        [TestCase("fox")]
        [TestCase("panda")]
        [TestCase("elephant")]
        [TestCase("Fox")]
        [TestCase(" fox ")]
        public void PreservesNonBlankIdentifierExactly(string value)
        {
            var id = new AnimalId(value);
            Assert.That(id.Value, Is.EqualTo(value));
            Assert.That(id.ToString(), Is.EqualTo(value));
        }

        [Test]
        public void RejectsNullIdentifier()
        {
            Assert.Throws<ArgumentNullException>(() => new AnimalId(null));
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase("\t\r\n")]
        [TestCase("\u00a0")]
        public void RejectsEmptyOrWhitespaceIdentifier(string value)
        {
            Assert.Throws<ArgumentException>(() => new AnimalId(value));
        }

        [Test]
        public void EqualityAndHashingUseIdentifierInsteadOfReference()
        {
            var first = new AnimalId("fox");
            var second = new AnimalId("fox");
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Equals(second), Is.True);
            Assert.That(first.Equals((object)second), Is.True);
            Assert.That(first == second, Is.True);
            Assert.That(first != second, Is.False);
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
            Assert.That(new HashSet<AnimalId> { first, second }.Count, Is.EqualTo(1));
        }

        [TestCase("panda")]
        [TestCase("FOX")]
        [TestCase(" fox ")]
        public void DistinctOrdinalIdentifiersAreNotEqual(string value)
        {
            var first = new AnimalId("fox");
            var second = new AnimalId(value);
            Assert.That(first.Equals(second), Is.False);
            Assert.That(first == second, Is.False);
            Assert.That(first != second, Is.True);
        }

        [Test]
        public void EqualityHandlesNullAndUnrelatedObjects()
        {
            var id = new AnimalId("fox");
            AnimalId absent = null;
            Assert.That(id.Equals(absent), Is.False);
            Assert.That(id.Equals((object)null), Is.False);
            Assert.That(id.Equals("fox"), Is.False);
            Assert.That(id == absent, Is.False);
            Assert.That(absent == id, Is.False);
            Assert.That(absent == (AnimalId)null, Is.True);
            Assert.That(id != absent, Is.True);
        }
    }
}
