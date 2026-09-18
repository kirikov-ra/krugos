using System;
using System.Globalization;
using Krugos.Application;
using Krugos.Domain;

namespace Krugos.Presentation
{
    public sealed class SudokuGameplayPresenter
    {
        private readonly SudokuPuzzleDefinition definition;
        private readonly TimeSpan bonusDuration;
        private TimeSpan errorRemaining;

        public SudokuGameSession Session { get; private set; }
        public SudokuAnimalSet Animals { get; }
        public CellPosition? Selection { get; private set; }
        public CellPosition? ErrorCell { get; private set; }
        public bool NotesMode { get; private set; }
        public string Feedback { get; private set; } = "Select an empty cell, then choose an animal.";
        public event Action Changed;

        public string Countdown
        {
            get
            {
                var seconds = (long)Math.Ceiling(Session.RemainingBonusTime.TotalSeconds);
                return (seconds / 60).ToString("00", CultureInfo.InvariantCulture) + ":"
                    + (seconds % 60).ToString("00", CultureInfo.InvariantCulture);
            }
        }

        public SudokuGameplayPresenter(SudokuPuzzleDefinition definition, SudokuAnimalSet animals,
            TimeSpan bonusDuration)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Animals = animals ?? throw new ArgumentNullException(nameof(animals));
            this.bonusDuration = bonusDuration;
            Session = new SudokuGameSession(definition, bonusDuration);
        }

        public void Select(CellPosition position)
        {
            if (Session.State != SudokuSessionState.Active || Session.Board.IsGiven(position))
                return;
            Selection = position;
            Feedback = NotesMode ? "Notes mode · choose animals to mark possibilities." : "Choose an animal for the selected cell.";
            Changed?.Invoke();
        }

        public void ToggleNotesMode()
        {
            if (Session.State != SudokuSessionState.Active)
                return;
            NotesMode = !NotesMode;
            Feedback = NotesMode ? "Notes mode · choose animals to mark possibilities." : "Animal mode · choose an animal to place.";
            Changed?.Invoke();
        }

        public SudokuMoveResult InputAnimal(AnimalId animal)
        {
            if (!Selection.HasValue)
                return null;
            var position = Selection.Value;
            var value = Animals.GetValue(animal);
            var result = NotesMode ? Session.ToggleNote(position, value) : Session.SetValue(position, value);
            if (result.IsCorrect == false)
            {
                ErrorCell = position;
                errorRemaining = TimeSpan.FromSeconds(1.5);
                Feedback = result.LifeLost ? "That animal does not fit · one life lost." : "Same animal · no additional life lost.";
            }
            else if (result.Status == SudokuMoveStatus.CellNotEmpty)
                Feedback = "Clear this cell before adding notes.";
            else if (result.Status == SudokuMoveStatus.Applied)
            {
                if (ErrorCell == position)
                    ErrorCell = null;
                Feedback = NotesMode ? "Notes updated." : "Animal placed.";
            }
            Changed?.Invoke();
            return result;
        }

        public SudokuMoveResult Clear()
        {
            if (!Selection.HasValue)
                return null;
            var result = Session.ClearValue(Selection.Value);
            if (result.Status == SudokuMoveStatus.Applied)
            {
                if (ErrorCell == Selection)
                    ErrorCell = null;
                Feedback = "Cell cleared.";
            }
            Changed?.Invoke();
            return result;
        }

        public void AdvanceTime(TimeSpan delta)
        {
            var previousCountdown = Countdown;
            Session.AdvanceTime(delta);
            var errorExpired = false;
            if (ErrorCell.HasValue)
            {
                errorRemaining = delta >= errorRemaining ? TimeSpan.Zero : errorRemaining - delta;
                if (errorRemaining == TimeSpan.Zero)
                {
                    ErrorCell = null;
                    errorExpired = true;
                }
            }
            if (errorExpired || Countdown != previousCountdown)
                Changed?.Invoke();
        }

        public void Restart()
        {
            Session = new SudokuGameSession(definition, bonusDuration);
            Selection = null;
            ErrorCell = null;
            errorRemaining = TimeSpan.Zero;
            NotesMode = false;
            Feedback = "Select an empty cell, then choose an animal.";
            Changed?.Invoke();
        }
    }
}
