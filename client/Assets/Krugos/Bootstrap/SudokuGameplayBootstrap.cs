using System;
using Krugos.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Krugos.Bootstrap
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class SudokuGameplayBootstrap : MonoBehaviour
    {
        [SerializeField, Min(0)] private float developmentCountdownSeconds = 600;
        private SudokuGameplayView view;
        public SudokuGameplayPresenter Presenter { get; private set; }

        private void Start()
        {
            var composition = new SudokuGameplayComposition();
            Presenter = composition.CreatePresenter(TimeSpan.FromSeconds(developmentCountdownSeconds));
            BindView();
        }

        private void OnEnable()
        {
            if (Presenter != null) BindView();
        }

        private void BindView() => view = new SudokuGameplayView(GetComponent<UIDocument>().rootVisualElement, Presenter);

        private void Update() => Presenter?.AdvanceTime(TimeSpan.FromSeconds(Time.unscaledDeltaTime));

        private void OnDisable()
        {
            view?.Dispose();
            view = null;
        }
    }
}
