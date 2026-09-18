using System;
using Krugos.Domain;
using UnityEngine.UIElements;

namespace Krugos.Presentation
{
    public sealed class SudokuGameplayView : IDisposable
    {
        private readonly SudokuGameplayPresenter presenter;
        private readonly SudokuBoardView board;
        private readonly Label lives;
        private readonly Label countdown;
        private readonly Label feedback;
        private readonly Label bonusCaption;
        private readonly Label terminalTitle;
        private readonly Label terminalDetail;
        private readonly VisualElement overlay;
        private readonly VisualElement palette;
        private readonly Button notes;
        private readonly Button clear;
        private readonly Button restart;
        private readonly Button terminalRestart;

        public SudokuGameplayView(VisualElement root, SudokuGameplayPresenter presenter)
        {
            this.presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            lives = root.Q<Label>("lives");
            countdown = root.Q<Label>("countdown");
            bonusCaption = root.Q<Label>("bonus-caption");
            feedback = root.Q<Label>("feedback");
            overlay = root.Q("terminal-overlay");
            terminalTitle = root.Q<Label>("terminal-title");
            terminalDetail = root.Q<Label>("terminal-detail");
            palette = root.Q("animal-palette");
            notes = root.Q<Button>("notes-toggle");
            clear = root.Q<Button>("clear");
            restart = root.Q<Button>("restart");
            terminalRestart = root.Q<Button>("terminal-restart");
            board = new SudokuBoardView(root.Q("board"), presenter.Select);
            palette.Clear();
            foreach (var animal in presenter.Animals.Animals)
            {
                var tile = new Button(() => presenter.InputAnimal(animal))
                {
                    name = "animal-" + animal.Value,
                    text = AnimalLabels.Full(animal),
                    tooltip = AnimalLabels.Compact(animal) + " · " + AnimalLabels.Full(animal)
                };
                tile.AddToClassList("animal-tile");
                palette.Add(tile);
            }
            notes.clicked += presenter.ToggleNotesMode;
            clear.clicked += Clear;
            restart.clicked += presenter.Restart;
            terminalRestart.clicked += presenter.Restart;
            presenter.Changed += Render;
            Render();
        }

        private void Clear() => presenter.Clear();

        private void Render()
        {
            var session = presenter.Session;
            var active = session.State == SudokuSessionState.Active;
            board.Render(presenter);
            lives.text = session.RemainingLives + " / " + SudokuGameSession.InitialLives;
            countdown.text = presenter.Countdown;
            bonusCaption.text = session.IsSpeedBonusAvailable ? "BONUS COUNTDOWN" : "TIME UP · KEEP PLAYING";
            feedback.text = presenter.Feedback;
            notes.text = presenter.NotesMode ? "Notes · ON" : "Notes · OFF";
            notes.EnableInClassList("active", presenter.NotesMode);
            notes.SetEnabled(active);
            clear.SetEnabled(active && presenter.Selection.HasValue);
            palette.SetEnabled(active && presenter.Selection.HasValue);
            overlay.style.display = active ? DisplayStyle.None : DisplayStyle.Flex;
            terminalTitle.text = session.State == SudokuSessionState.Won ? "Puzzle Complete" : "No Lives Left";
            terminalDetail.text = session.State == SudokuSessionState.Won
                ? "Every animal has found its place." : "A fresh board is one tap away.";
        }

        public void Dispose()
        {
            presenter.Changed -= Render;
            notes.clicked -= presenter.ToggleNotesMode;
            clear.clicked -= Clear;
            restart.clicked -= presenter.Restart;
            terminalRestart.clicked -= presenter.Restart;
            palette.Clear();
            board.Dispose();
        }
    }
}
