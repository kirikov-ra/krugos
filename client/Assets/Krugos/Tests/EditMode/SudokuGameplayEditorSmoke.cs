using Krugos.Bootstrap;
using Krugos.Domain;
using Krugos.Presentation;
using UnityEditor;
using UnityEngine;

namespace Krugos.Tests.EditMode
{
    // Test-only shortcuts for repeatable live Editor smoke checks; excluded from players.
    public static class SudokuGameplayEditorSmoke
    {
        [MenuItem("Krugos/Validation/Expire Bonus Countdown")]
        public static void ExpireCountdown()
        {
            var presenter = GetPresenter();
            presenter?.AdvanceTime(presenter.Session.RemainingBonusTime);
        }

        [MenuItem("Krugos/Validation/Prepare Final Move")]
        public static void PrepareFinalMove()
        {
            var presenter = GetPresenter();
            if (presenter == null) return;
            presenter.Restart();
            var last = new CellPosition(0, 2);
            for (var row = 0; row < 9; row++)
            for (var column = 0; column < 9; column++)
            {
                var position = new CellPosition(row, column);
                if (presenter.Session.Board.IsGiven(position) || position == last) continue;
                presenter.Select(position);
                presenter.InputAnimal(presenter.Animals.GetAnimal(presenter.Session.Definition.Solution.GetValue(position)));
            }
            presenter.Select(last);
        }

        [MenuItem("Krugos/Validation/Expire Bonus Countdown", true)]
        [MenuItem("Krugos/Validation/Prepare Final Move", true)]
        public static bool CanValidate() => EditorApplication.isPlaying && GetPresenter() != null;

        private static SudokuGameplayPresenter GetPresenter() =>
            Object.FindAnyObjectByType<SudokuGameplayBootstrap>()?.Presenter;
    }
}
