using Krugos.Application;

namespace Krugos.Infrastructure
{
    public static class BuiltInSudokuAnimalSet
    {
        public static SudokuAnimalSet Default { get; } = new SudokuAnimalSet(new[]
        {
            new AnimalId("fox"),
            new AnimalId("panda"),
            new AnimalId("elephant"),
            new AnimalId("giraffe"),
            new AnimalId("lion"),
            new AnimalId("zebra"),
            new AnimalId("monkey"),
            new AnimalId("hippo"),
            new AnimalId("tiger")
        });
    }
}
