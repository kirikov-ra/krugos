# Первый playable — KRG-007

## Запуск

Открыть `client/` в **Unity 6000.6.1f1**, дождаться импорта и компиляции.
Открыть `Assets/Krugos/Scenes/SudokuGameplay.unity` либо выполнить
`Krugos > Open Sudoku Gameplay`. В Game View выбрать portrait 720×1280 или
1080×1920 и нажать Play. Сцена уже включена в Editor Build Settings.

Автоматически создаётся партия `built-in-001` из
`BuiltInSudokuPuzzleProvider.GetPuzzle(0)` с `BuiltInSudokuAnimalSet.Default`,
3 жизнями и development countdown **600 секунд**. Длительность задана явно
в компоненте `SudokuGameplayBootstrap`, не связана со сложностью.
Для короткой проверки expiry перед Play можно временно поставить в Inspector
`Development Countdown Seconds = 2`; не сохранять это изменение в сцену.

Мышкой выбрать светлую editable-клетку, затем животное на панели. Серые givens
заблокированы. Зелёный фон выделяет выбранную клетку. Неверный ввод остаётся
на доске и подсвечивается красным 1,5 секунды. Повтор того же неверного значения
не снимает ещё одну жизнь; замена другим неверным — снимает.

`Notes` переключает ввод на заметки: повторный клик по животному снимает note.
Для заполненной клетки notes отвергаются с пояснением; сначала использовать
`Clear`. `Clear` вызывает именно `ClearValue`: пустую клетку с notes оставляет
без изменений, как предписывает существующая сессия. Для удаления заметки
повторно нажать соответствующее животное в Notes mode.

Таймер показывает оставшееся бонусное окно; при `00:00` можно продолжать играть.
После `Puzzle Complete` или `No Lives Left` игровой ввод отключается.
`Restart` / `Play again` создаёт новую сессию той же definition; предыдущая
terminal session не возвращается в Active.

## Композиция и ответственность

`Krugos.Bootstrap` — верхний runtime composition root, с явными ссылками на
Presentation, Infrastructure, Application и Domain. Нижние слои не ссылаются
на Bootstrap. Presentation не имеет ссылки на Infrastructure.
DI container отсутствует.

- `SudokuGameplayComposition` получает первую definition и default animal set.
- `SudokuGameplayBootstrap` — небольшой MonoBehaviour: создаёт presenter/view,
  управляет их жизненным циклом, передаёт `Time.unscaledDeltaTime` в presenter.
- `SudokuGameplayPresenter` хранит сессию и только UI-состояние: selection,
  Notes mode, краткую обратную связь и временную подсветку последнего error result.
  Все mutations идут через `SudokuGameSession`; время — через `AdvanceTime`.
- `SudokuGameplayView` связывает элементы UXML с presenter и обновляет HUD,
  controls и terminal overlay.
- `Krugos.Presentation.SudokuBoardView` системно создаёт 81 UI Toolkit Button
  и сетки notes, читает значения и notes из сессии, поддерживает квадрат доски.
  Это UI-класс; `Krugos.Domain.SudokuBoardView` остаётся read-only доступом к модели.
- `AnimalLabels` форматирует переданный `AnimalId`, не знает соответствия чисел.
- `SudokuGameplaySceneSetup` — Editor-only команда открытия и явного пересоздания
  минимальной сцены. При обычном импорте ничего не пересоздаёт.

Gameplay truth — **только `SudokuGameSession`**. UI не считает canonical
correctness, Sudoku constraints, lives, completion или правила notes.
Подсветка ошибки основана на `SudokuMoveResult.IsCorrect`; повтор ввода и
terminal rejection остаются ответственностью Domain. Отдельной копии доски нет.

## UI и временные животные

Runtime UI Toolkit: `SudokuGameplay.uxml`, `SudokuGameplay.uss`,
`KrugosRuntimeTheme.tss` и сериализованный `SudokuPanelSettings.asset`.
PanelSettings масштабирует относительно 720×1280 по ширине. Board занимает
основную площадь, блоки 3×3 разделены более толстыми линиями.

Названия берутся через `SudokuAnimalSet.GetAnimal`; клик преобразуется обратно
через `GetValue`. Панель показывает полные ID в верхнем регистре, клетки —
первые три буквы (`FOX`, `PAN`, `ELE`, `GIR`, `LIO`, `ZEB`, `MON`, `HIP`, `TIG`),
notes — первые две в mini-grid 3×3. Tooltip содержит полное имя. Цифры не
используются как игровые символы. Эти сокращения уникальны для default set;
произвольные будущие наборы могут потребовать другой visual representation.
Sprite assets не нужны. Позже labels можно заменить icons, сохранив Domain/Application.

Добавлена прямая зависимость только от встроенного
`com.unity.modules.uielements: 1.0.0` — runtime API UI Toolkit. Его штатные
транзитивные модули: ui, imgui, jsonserialize, hierarchycore, physics, animation.
Первые imgui/jsonserialize уже использовались Test Framework. Сторонних пакетов нет.

## Проверки

`pwsh -File tools/Validate-Foundation.ps1` проверяет обновлённый граф восьми
сборок, metadata, package allowlist и Git hygiene. Архитектурные EditMode tests
также проверяют фактические compiler references и изоляцию Editor/Tests от player.

Новые EditMode tests проверяют composition, переставленный animal mapping в
реальном UXML view, неизменность definition при rendering, given selection,
canonical error/repeat, notes/clear, expiry, обе terminal states/restart,
сериализованные ссылки сцены и отсутствие Missing Script.

Минимальный PlayMode suite загружает production scene, отправляет UI Toolkit
NavigationSubmit events настоящим кнопкам и проверяет полный ввод до Won/Lost,
notes, clear, timer, restart и отсутствие неожиданных логов. Отдельно проверяет
геометрию квадратной доски и доступность controls на panel targets 720×1280 и
1080×1920. Это автоматическая проверка; она не заменяет клики мышкой в Editor.

Команда запуска аналогична FOUNDATION.md, с `-testPlatform PlayMode` для
PlayMode suite. Для графической проверки PlayMode не использовать `-nographics`.
Результаты и логи складывать в игнорируемый `client/TestResults/`.

Для воспроизводимого smoke в обычном Editor есть два пункта меню
`Krugos > Validation` (код в **Tests.EditMode**, исключён из player):
`Expire Bonus Countdown` передаёт оставшееся время в `AdvanceTime`;
`Prepare Final Move` создаёт свежую сессию и через presenter заполняет все
editable-клетки кроме `(0, 2)`. Последний ход — нажать `GIRAFFE` мышкой.
Это тестовая подготовка состояния для проверки Won, не игровой hints/solve UI.

### Результат 17–18.09.2026

- Импорт и компиляция Unity 6000.6.1f1: успешно, exit code 0.
- EditMode: **288/288 Passed** (279 прежних + 9 новых), без пропусков.
- PlayMode: **3/3 Passed**, запуск с графическим устройством.
- Panel targets **720×1280 и 1080×1920**: квадратная доска и controls внутри
  экрана подтверждены автоматическим PlayMode test.
- Реальный **не-batch Editor Play Mode**: сцена открыта и Play нажат через UI;
  кликами мыши проверены selection, correct input, Clear, несколько notes,
  normal input вместо notes, неправильный ввод с видимым сохранённым animal,
  уменьшение lives, повтор без штрафа, Lost, блокировка доски и Restart.
- В живом Editor также проверены `00:00` и последующий правильный ход;
  время ускорено тестовым меню. Won получен последним кликом мыши после
  `Prepare Final Move`; Restart после Won проверен. Полная последовательность
  заполнения с нуля дополнительно проверена автоматическими UI events.
- Визуально просмотрен portrait Game View около 9:16; точные два разрешения
  выше проверены отдельно через panel targets, а не ручной сменой presets.
- После live smoke Console: **0 errors / 0 warnings**, Missing Script/Reference
  не обнаружены. При первом открытии Editor было штатное предупреждение о
  deprecated Input Manager в исходной конфигурации проекта; Input System
  package ради его скрытия не добавлялся.
- Логи: `krg-007-import.log`, `krg-007-editmode.log/xml`,
  `krg-007-playmode.log/xml`, `krg-007-editor-smoke.log` в `client/TestResults/`.

## Намеренно отложено

Landscape и mobile player builds, safe-area на конкретных устройствах,
финальные sprites/icons и локализация UI. Нет menu, difficulty, hints, undo,
generator, save/resume, коллекции/zoo, progression, backend, ads/IAP/analytics,
звука или polished animations. KRG-008 и следующие задачи не начаты.
