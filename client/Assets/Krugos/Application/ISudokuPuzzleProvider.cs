using Krugos.Domain;

namespace Krugos.Application
{
    public interface ISudokuPuzzleProvider
    {
        int Count { get; }
        SudokuPuzzleDefinition GetPuzzle(int index);
    }
}
