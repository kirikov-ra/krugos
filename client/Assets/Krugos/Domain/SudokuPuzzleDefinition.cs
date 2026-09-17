using System;
using System.Text.RegularExpressions;

namespace Krugos.Domain
{
    public sealed class SudokuPuzzleDefinition
    {
        public string PuzzleId { get; }
        public SudokuPuzzle Puzzle { get; }
        public SudokuSolution Solution { get; }

        private SudokuPuzzleDefinition(string puzzleId, SudokuPuzzle puzzle, SudokuSolution solution)
        {
            PuzzleId = puzzleId;
            Puzzle = puzzle;
            Solution = solution;
        }

        public static SudokuPuzzleDefinition Create(string puzzleId, SudokuPuzzle puzzle)
        {
            if (puzzleId == null)
                throw new ArgumentNullException(nameof(puzzleId));
            if (!Regex.IsMatch(puzzleId, @"\A[a-z0-9]+(?:-[a-z0-9]+)*\z"))
                throw new ArgumentException("Puzzle ID must contain lowercase ASCII letters or digits, with optional single hyphens between segments.", nameof(puzzleId));
            if (puzzle == null)
                throw new ArgumentNullException(nameof(puzzle));

            if (!SudokuSolver.TrySolve(puzzle, out var solution))
                throw new PuzzleValidationException("A playable puzzle must have a solution.");
            if (!SudokuSolver.HasUniqueSolution(puzzle))
                throw new PuzzleValidationException("A playable puzzle must have exactly one solution.");

            return new SudokuPuzzleDefinition(puzzleId, puzzle, solution);
        }
    }
}
