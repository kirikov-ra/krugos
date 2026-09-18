using System;
using Krugos.Application;
using Krugos.Domain;
using Krugos.Infrastructure;
using Krugos.Presentation;

namespace Krugos.Bootstrap
{
    public sealed class SudokuGameplayComposition
    {
        public static readonly TimeSpan DevelopmentBonusDuration = TimeSpan.FromMinutes(10);
        public SudokuPuzzleDefinition Definition { get; }
        public SudokuAnimalSet Animals { get; }

        public SudokuGameplayComposition()
        {
            Definition = new BuiltInSudokuPuzzleProvider().GetPuzzle(0);
            Animals = BuiltInSudokuAnimalSet.Default;
        }

        public SudokuGameplayPresenter CreatePresenter(TimeSpan bonusDuration) =>
            new SudokuGameplayPresenter(Definition, Animals, bonusDuration);
    }
}
