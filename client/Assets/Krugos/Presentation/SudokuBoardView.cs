using System;
using Krugos.Domain;
using UnityEngine.UIElements;

namespace Krugos.Presentation
{
    public sealed class SudokuBoardView : IDisposable
    {
        private readonly VisualElement board;
        private readonly Button[] cells = new Button[81];
        private readonly Label[] values = new Label[81];
        private readonly Label[,] notes = new Label[81, 9];
        private readonly VisualElement[] noteGrids = new VisualElement[81];

        public SudokuBoardView(VisualElement board, Action<CellPosition> onSelect)
        {
            this.board = board;
            board.Clear();
            board.RegisterCallback<GeometryChangedEvent>(Resize);
            for (var row = 0; row < 9; row++)
            {
                var rowElement = new VisualElement();
                rowElement.AddToClassList("board-row");
                board.Add(rowElement);
                for (var column = 0; column < 9; column++)
                {
                    var index = row * 9 + column;
                    var position = new CellPosition(row, column);
                    var cell = new Button(() => onSelect(position)) { name = $"cell-{row}-{column}" };
                    cell.AddToClassList("cell");
                    if (column == 2 || column == 5) cell.AddToClassList("box-right");
                    if (row == 2 || row == 5) cell.AddToClassList("box-bottom");
                    values[index] = new Label { pickingMode = PickingMode.Ignore };
                    values[index].AddToClassList("cell-value");
                    cell.Add(values[index]);
                    var grid = new VisualElement { pickingMode = PickingMode.Ignore };
                    grid.AddToClassList("note-grid");
                    for (var note = 0; note < 9; note++)
                    {
                        notes[index, note] = new Label { pickingMode = PickingMode.Ignore };
                        notes[index, note].AddToClassList("note");
                        grid.Add(notes[index, note]);
                    }
                    cell.Add(grid);
                    cells[index] = cell;
                    noteGrids[index] = grid;
                    rowElement.Add(cell);
                }
            }
        }

        public void Render(SudokuGameplayPresenter presenter)
        {
            var session = presenter.Session;
            for (var index = 0; index < 81; index++)
            {
                var position = new CellPosition(index / 9, index % 9);
                var value = session.Board.GetValue(position);
                var given = session.Board.IsGiven(position);
                var cell = cells[index];
                cell.EnableInClassList("given", given);
                cell.EnableInClassList("selected", presenter.Selection == position);
                cell.EnableInClassList("error", presenter.ErrorCell == position);
                cell.SetEnabled(!given && session.State == SudokuSessionState.Active);
                values[index].text = value.HasValue ? AnimalLabels.Compact(presenter.Animals.GetAnimal(value.Value)) : "";
                cell.tooltip = value.HasValue ? AnimalLabels.Full(presenter.Animals.GetAnimal(value.Value)) : "Empty cell";
                noteGrids[index].style.display = value.HasValue ? DisplayStyle.None : DisplayStyle.Flex;
                for (var note = 0; note < 9; note++) notes[index, note].text = "";
                foreach (var note in session.GetNotes(position))
                    notes[index, note.Value - 1].text = AnimalLabels.Note(presenter.Animals.GetAnimal(note));
            }
        }

        private void Resize(GeometryChangedEvent evt)
        {
            if (Math.Abs(board.resolvedStyle.height - evt.newRect.width) > 0.1f)
                board.style.height = evt.newRect.width;
        }

        public void Dispose() => board.UnregisterCallback<GeometryChangedEvent>(Resize);
    }
}
