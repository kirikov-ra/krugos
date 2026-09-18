using System;
using System.Linq;
using Krugos.Application;
using Krugos.Bootstrap;
using Krugos.Domain;
using Krugos.Infrastructure;
using Krugos.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Krugos.Tests.EditMode
{
    public sealed class SudokuGameplayPresentationTests
    {
        private static readonly CellPosition Editable = new CellPosition(0, 2);
        private SudokuGameplayComposition composition;
        private SudokuGameplayPresenter presenter;

        [SetUp]
        public void SetUp()
        {
            composition = new SudokuGameplayComposition();
            presenter = composition.CreatePresenter(SudokuGameplayComposition.DevelopmentBonusDuration);
        }

        [Test]
        public void CompositionUsesFirstBuiltInPuzzleDefaultAnimalsAndExplicitDuration()
        {
            Assert.That(presenter.Session.Definition.PuzzleId, Is.EqualTo(new BuiltInSudokuPuzzleProvider().GetPuzzle(0).PuzzleId));
            Assert.That(presenter.Animals, Is.SameAs(BuiltInSudokuAnimalSet.Default));
            Assert.That(presenter.Session.RemainingLives, Is.EqualTo(3));
            Assert.That(presenter.Session.RemainingBonusTime, Is.EqualTo(TimeSpan.FromMinutes(10)));
        }

        [Test]
        public void ReorderedMappingDrivesBothAnimalInputAndRenderingWithoutChangingDefinition()
        {
            var animals = new SudokuAnimalSet(composition.Animals.Animals.Reverse().ToArray());
            presenter = new SudokuGameplayPresenter(composition.Definition, animals, TimeSpan.FromMinutes(10));
            presenter.Select(Editable);
            var correct = composition.Definition.Solution.GetValue(Editable);
            Assert.That(presenter.InputAnimal(animals.GetAnimal(correct)).IsCorrect, Is.True);
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Krugos/Presentation/UI/SudokuGameplay.uxml");
            var root = template.CloneTree();
            using (new SudokuGameplayView(root, presenter))
            {
                Assert.That(root.Q<Button>("cell-0-2").Q<Label>().text, Is.EqualTo(AnimalLabels.Compact(animals.GetAnimal(correct))));
                Assert.That(root.Q<Button>("animal-" + animals.GetAnimal(correct).Value).text,
                    Is.EqualTo(AnimalLabels.Full(animals.GetAnimal(correct))));
                Assert.That(presenter.Session.Board.GetValue(Editable), Is.EqualTo(correct));
                Assert.That(composition.Definition.Puzzle.GetGiven(Editable), Is.Null);
                Assert.That(composition.Definition.Solution.GetValue(Editable), Is.EqualTo(correct));
                Assert.That(presenter.Session.RemainingLives, Is.EqualTo(3));
                Assert.That(presenter.Session.ElapsedTime, Is.EqualTo(TimeSpan.Zero));
            }
        }

        [Test]
        public void IncorrectInputAndRepeatFollowDomainResultAndErrorExpires()
        {
            presenter.Select(Editable);
            var wrong = composition.Animals.GetAnimal(new SudokuValue(1));
            Assert.That(presenter.InputAnimal(wrong).LifeLost, Is.True);
            Assert.That(presenter.InputAnimal(wrong).Status, Is.EqualTo(SudokuMoveStatus.Unchanged));
            Assert.That(presenter.Session.RemainingLives, Is.EqualTo(2));
            Assert.That(presenter.ErrorCell, Is.EqualTo(Editable));
            presenter.AdvanceTime(TimeSpan.FromSeconds(2));
            Assert.That(presenter.ErrorCell, Is.Null);
            Assert.That(presenter.Session.Board.GetValue(Editable), Is.EqualTo(new SudokuValue(1)));
        }

        [Test]
        public void NotesFilledCellRejectionAndClearFollowSessionApi()
        {
            presenter.Select(Editable);
            var animal = composition.Animals.GetAnimal(new SudokuValue(4));
            presenter.ToggleNotesMode();
            presenter.InputAnimal(animal);
            Assert.That(presenter.Session.GetNotes(Editable), Is.EqualTo(new[] { new SudokuValue(4) }));
            presenter.ToggleNotesMode();
            presenter.InputAnimal(animal);
            Assert.That(presenter.Session.GetNotes(Editable), Is.Empty);
            presenter.ToggleNotesMode();
            Assert.That(presenter.InputAnimal(animal).Status, Is.EqualTo(SudokuMoveStatus.CellNotEmpty));
            presenter.Clear();
            Assert.That(presenter.Session.Board.GetValue(Editable), Is.Null);
        }

        [Test]
        public void GivenSelectionDoesNotAllowEditing()
        {
            presenter.Select(new CellPosition(0, 0));
            Assert.That(presenter.Selection, Is.Null);
            Assert.That(presenter.InputAnimal(composition.Animals.Animals[0]), Is.Null);
            Assert.That(presenter.Session.Board.GetValue(new CellPosition(0, 0)), Is.EqualTo(new SudokuValue(5)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TerminalInputIsRejectedAndRestartCreatesFreshSession(bool win)
        {
            presenter.Select(Editable);
            if (win)
            {
                for (var row = 0; row < 9; row++)
                for (var column = 0; column < 9; column++)
                {
                    var position = new CellPosition(row, column);
                    if (presenter.Session.Board.IsGiven(position)) continue;
                    presenter.Select(position);
                    presenter.InputAnimal(presenter.Animals.GetAnimal(composition.Definition.Solution.GetValue(position)));
                }
            }
            else
                for (var value = 1; value <= 3; value++) presenter.InputAnimal(presenter.Animals.GetAnimal(new SudokuValue(value)));
            var terminal = presenter.Session;
            Assert.That(terminal.State, Is.EqualTo(win ? SudokuSessionState.Won : SudokuSessionState.Lost));
            Assert.That(presenter.InputAnimal(presenter.Animals.Animals[0]).Status, Is.EqualTo(SudokuMoveStatus.SessionEnded));
            Assert.That(presenter.Clear().Status, Is.EqualTo(SudokuMoveStatus.SessionEnded));
            presenter.Restart();
            Assert.That(presenter.Session, Is.Not.SameAs(terminal));
            Assert.That(presenter.Session.Definition, Is.SameAs(terminal.Definition));
            Assert.That(presenter.Session.State, Is.EqualTo(SudokuSessionState.Active));
            Assert.That(presenter.Session.RemainingLives, Is.EqualTo(3));
            Assert.That(presenter.Selection, Is.Null);
        }

        [Test]
        public void CountdownRoundsUpAndExpiryAllowsInput()
        {
            presenter.AdvanceTime(TimeSpan.FromMilliseconds(500));
            Assert.That(presenter.Countdown, Is.EqualTo("10:00"));
            presenter.AdvanceTime(TimeSpan.FromMinutes(10));
            Assert.That(presenter.Countdown, Is.EqualTo("00:00"));
            Assert.That(presenter.Session.State, Is.EqualTo(SudokuSessionState.Active));
            presenter.Select(Editable);
            Assert.That(presenter.InputAnimal(presenter.Animals.GetAnimal(new SudokuValue(4))).IsCorrect, Is.True);
        }

        [Test]
        public void ProductionSceneHasNoMissingScriptsOrObjectReferences()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Krugos/Scenes/SudokuGameplay.unity");
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), Is.Zero);
                    foreach (var component in transform.GetComponents<Component>())
                    {
                        using (var serialized = new SerializedObject(component))
                        {
                            var property = serialized.GetIterator();
                            while (property.NextVisible(true))
                                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                                    Assert.That(property.objectReferenceEntityIdValue, Is.EqualTo(EntityId.None), property.propertyPath);
                        }
                    }
                }
                var document = scene.GetRootGameObjects().SelectMany(root => root.GetComponents<UIDocument>()).Single();
                Assert.That(document.panelSettings, Is.Not.Null);
                Assert.That(document.panelSettings.themeStyleSheet, Is.Not.Null);
                Assert.That(document.visualTreeAsset, Is.Not.Null);
                Assert.That(document.GetComponent<SudokuGameplayBootstrap>(), Is.Not.Null);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
