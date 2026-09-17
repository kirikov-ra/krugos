using System;
using System.Collections.Generic;
using Krugos.Domain;
using NUnit.Framework;

namespace Krugos.Tests.EditMode
{
    public sealed class SudokuTypeTests
    {
        [Test]
        public void PositionsAcceptEveryCellAndRemainDistinct()
        {
            var positions = new HashSet<CellPosition>();
            for (var row = 0; row < 9; row++)
            for (var column = 0; column < 9; column++)
            {
                var position = new CellPosition(row, column);
                Assert.That(position.Row, Is.EqualTo(row));
                Assert.That(position.Column, Is.EqualTo(column));
                positions.Add(position);
            }

            Assert.That(positions.Count, Is.EqualTo(81));
        }

        [TestCase(-1, 0)]
        [TestCase(9, 0)]
        [TestCase(0, -1)]
        [TestCase(0, 9)]
        [TestCase(int.MinValue, 0)]
        [TestCase(0, int.MaxValue)]
        public void PositionRejectsInvalidCoordinates(int row, int column)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CellPosition(row, column));
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
        public void ValueAcceptsLogicalDigit(int number)
        {
            Assert.That(new SudokuValue(number).Value, Is.EqualTo(number));
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(10)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void ValueRejectsInvalidDigit(int number)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SudokuValue(number));
        }

        [Test]
        public void DefaultStructsAreValidAndEmptyIsNullable()
        {
            Assert.That(default(CellPosition), Is.EqualTo(new CellPosition(0, 0)));
            Assert.That(default(SudokuValue).Value, Is.EqualTo(1));
            Assert.That(default(SudokuValue?), Is.Null);
        }

        [Test]
        public void PositionsAndValuesHaveValueEquality()
        {
            var position = new CellPosition(2, 7);
            Assert.That(position == new CellPosition(2, 7), Is.True);
            Assert.That(position != new CellPosition(7, 2), Is.True);
            Assert.That(position.Equals((object)new CellPosition(2, 7)), Is.True);
            Assert.That(position.Equals(null), Is.False);
            Assert.That(position.GetHashCode(), Is.EqualTo(new CellPosition(2, 7).GetHashCode()));

            var value = new SudokuValue(9);
            Assert.That(value == new SudokuValue(9), Is.True);
            Assert.That(value != new SudokuValue(1), Is.True);
            Assert.That(value.Equals((object)new SudokuValue(9)), Is.True);
            Assert.That(value.Equals(null), Is.False);
            Assert.That(value.GetHashCode(), Is.EqualTo(new SudokuValue(9).GetHashCode()));
        }
    }
}
