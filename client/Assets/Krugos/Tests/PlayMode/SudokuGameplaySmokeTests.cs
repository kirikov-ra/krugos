using System;
using System.Collections;
using Krugos.Bootstrap;
using Krugos.Domain;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Krugos.Tests.PlayMode
{
    public sealed class SudokuGameplaySmokeTests
    {
        private SudokuGameplayBootstrap bootstrap;
        private VisualElement root;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("SudokuGameplay");
            yield return null;
            bootstrap = UnityEngine.Object.FindAnyObjectByType<SudokuGameplayBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            root = bootstrap.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(root.panel, Is.Not.Null);
        }

        private void Click(string name)
        {
            var button = root.Q<Button>(name);
            Assert.That(button, Is.Not.Null, name);
            Assert.That(button.enabledInHierarchy, Is.True, name);
            using (var evt = NavigationSubmitEvent.GetPooled())
            {
                evt.target = button;
                button.SendEvent(evt);
            }
        }

        [UnityTest]
        public IEnumerator ControlsRouteInputNotesClearTimerLossAndRestart()
        {
            var presenter = bootstrap.Presenter;
            var position = new CellPosition(0, 2);
            Assert.That(root.Query<Button>(className: "cell").ToList().Count, Is.EqualTo(81));
            Assert.That(root.Q<Button>("cell-0-0").Q<Label>().text, Is.EqualTo("LIO"));
            Assert.That(root.Q<Button>("cell-0-0").enabledInHierarchy, Is.False);
            Click("cell-0-2");
            Click("animal-giraffe");
            Assert.That(presenter.Session.Board.GetValue(position), Is.EqualTo(new SudokuValue(4)));
            Click("clear");
            Click("notes-toggle");
            Click("animal-fox");
            Assert.That(presenter.Session.GetNotes(position).Count, Is.EqualTo(1));
            Assert.That(root.Q<Button>("cell-0-2").Query<Label>(className: "note").ToList()[0].text, Is.EqualTo("FO"));
            Click("animal-fox");
            Assert.That(presenter.Session.GetNotes(position), Is.Empty);
            Click("notes-toggle");
            Click("animal-fox");
            Assert.That(presenter.Session.RemainingLives, Is.EqualTo(2));
            Assert.That(root.Q<Button>("cell-0-2").ClassListContains("error"), Is.True);
            Assert.That(root.Q<Button>("cell-0-2").Q<Label>().text, Is.EqualTo("FOX"));
            Click("animal-fox");
            Assert.That(presenter.Session.RemainingLives, Is.EqualTo(2));
            var elapsed = presenter.Session.ElapsedTime;
            yield return null;
            yield return null;
            Assert.That(presenter.Session.ElapsedTime, Is.GreaterThan(elapsed));
            presenter.AdvanceTime(TimeSpan.FromMinutes(11));
            Assert.That(root.Q<Label>("countdown").text, Is.EqualTo("00:00"));
            Assert.That(presenter.Session.State, Is.EqualTo(SudokuSessionState.Active));
            Click("animal-panda");
            Click("animal-elephant");
            Assert.That(root.Q<Label>("terminal-title").text, Is.EqualTo("No Lives Left"));
            Assert.That(root.Q<Button>("cell-0-2").enabledInHierarchy, Is.False);
            var terminal = presenter.Session;
            Click("terminal-restart");
            Assert.That(presenter.Session, Is.Not.SameAs(terminal));
            Assert.That(presenter.Session.RemainingLives, Is.EqualTo(3));
            Assert.That(root.Q("terminal-overlay").style.display.value, Is.EqualTo(DisplayStyle.None));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CompletePuzzleThroughUiEventsShowsWon()
        {
            var presenter = bootstrap.Presenter;
            for (var row = 0; row < 9; row++)
            for (var column = 0; column < 9; column++)
            {
                var position = new CellPosition(row, column);
                if (presenter.Session.Board.IsGiven(position)) continue;
                Click($"cell-{row}-{column}");
                Click("animal-" + presenter.Animals.GetAnimal(presenter.Session.Definition.Solution.GetValue(position)).Value);
            }
            yield return null;
            Assert.That(presenter.Session.State, Is.EqualTo(SudokuSessionState.Won));
            Assert.That(root.Q<Label>("terminal-title").text, Is.EqualTo("Puzzle Complete"));
            Assert.That(root.Q("terminal-overlay").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(root.Q("animal-palette").enabledInHierarchy, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PortraitLayoutsKeepSquareBoardAndControlsInsideScreen()
        {
            var document = bootstrap.GetComponent<UIDocument>();
            var original = document.panelSettings;
            var settings = UnityEngine.Object.Instantiate(original);
            document.panelSettings = settings;
            foreach (var size in new[] { new Vector2Int(720, 1280), new Vector2Int(1080, 1920) })
            {
                var texture = new RenderTexture(size.x, size.y, 0);
                settings.targetTexture = texture;
                yield return null;
                yield return null;
                var board = root.Q("board").worldBound;
                var palette = root.Q("animal-palette").worldBound;
                var screen = root.Q("screen").worldBound;
                Assert.That(board.width, Is.EqualTo(board.height).Within(1), size.ToString());
                Assert.That(board.width, Is.GreaterThan(600));
                Assert.That(board.yMin, Is.GreaterThanOrEqualTo(screen.yMin));
                Assert.That(palette.yMax, Is.LessThanOrEqualTo(screen.yMax));
                Assert.That(palette.xMin, Is.GreaterThanOrEqualTo(screen.xMin));
                Assert.That(palette.xMax, Is.LessThanOrEqualTo(screen.xMax));
                settings.targetTexture = null;
                texture.Release();
                UnityEngine.Object.Destroy(texture);
            }
            document.panelSettings = original;
            UnityEngine.Object.Destroy(settings);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
