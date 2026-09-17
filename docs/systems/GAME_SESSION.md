# Игровая сессия Sudoku — KRG-005

`Krugos.Domain.SudokuGameSession` — чистая модель одной партии поверх
`SudokuPuzzleDefinition`. Игровые правила относятся к Domain; Application позже
сможет оркестрировать вызовы. Unity, системных часов, событий и новых пакетов нет.
Сессия использует готовое canonical solution и не запускает solver.
Контракты математической доски и контента описаны в
[SUDOKU_DOMAIN.md](SUDOKU_DOMAIN.md) и [PUZZLE_CONTENT.md](PUZZLE_CONTENT.md).

## Создание, состояние и чтение

`new SudokuGameSession(definition, bonusDuration)` создаёт независимую доску
из givens, пустые notes, **3 жизни**, нулевое elapsed time и заданный countdown.
`null` definition и отрицательная duration отвергаются исключениями аргументов.
Нулевая duration разрешена: speed bonus сразу недоступен. Связи со сложностью нет.

`SudokuSessionState` содержит только:

- `Active` — незавершённая партия, разрешены игровые действия;
- `Won` — доска полностью заполнена, валидна по Sudoku constraints и совпадает
  с canonical solution;
- `Lost` — после третьей ошибки осталось 0 жизней.

Обычная puzzle стартует `Active`. Пограничный случай полностью заполненной
definition, разрешённый KRG-004, сразу стартует `Won`: условия победы уже выполнены.
Прежние ошибки не препятствуют победе, если осталась хотя бы одна жизнь.

Свойства: `Definition`, `Board`, `State`, `RemainingLives`, `MistakeCount`,
`ElapsedTime`, `RemainingBonusTime`, `IsSpeedBonusAvailable`.
`MistakeCount` равен `3 - RemainingLives`; отдельной истории ходов нет.

`Board` — живое представление `SudokuBoardView` только для чтения: оно отражает
последующие ходы, но не предоставляет mutable `SudokuBoard`. Запросы значений,
givens, конфликтов, legal placements, candidates и завершённости делегированы
существующей доске. Нельзя обойти правила сессии через её публичную доску.
`SudokuBoard.SetValue` и другие контракты KRG-002 не изменены.

## Ходы, ошибки и жизни

`SetValue(position, value)` сохраняет ввод в editable-клетке, даже если он
неправильный. Ошибка — **новое значение, отличающееся от canonical solution**
в этой клетке. Такая попытка снимает ровно одну жизнь; третья сначала сохраняет
неправильное значение, затем переводит сессию в `Lost`.

Canonical correctness не равна Sudoku legality. Локально допустимый ввод может
стоить жизнь, а canonical-correct ввод с конфликтом из-за неверного соседа — нет.
Проверки constraints остаются доступны для будущей подсветки.

- Повтор того же текущего значения возвращает `Unchanged`, сохраняя lives.
  Для неверного повтора `IsCorrect == false`, но `LifeLost == false`.
- Замена неправильного значения другим неправильным — новая ошибка и `-1 life`.
- Исправление на canonical value не снимает и не восстанавливает жизни.
- `ClearValue(position)` очищает editable-клетку без изменения lives.
  Повторная очистка пустой клетки — `Unchanged`; её notes сохраняются.
- Очистка и последующий повторный неверный ввод считаются новой попыткой.
- Givens определяются существующим `board.IsGiven`; любые их изменения отвергаются.

Все mutations значений и notes возвращают неизменяемый `SudokuMoveResult`:

| Поле | Семантика |
| --- | --- |
| `Status` | `Applied`, `Unchanged`, `GivenCell`, `CellNotEmpty` или `SessionEnded`. `Applied` означает сохранённое действие, в том числе ошибочное. |
| `IsCorrect` | Canonical correctness для `SetValue` со статусом `Applied`/`Unchanged`; `null` для отказов, очистки и notes. |
| `LifeLost` | Снята ли жизнь именно этим действием. |
| `RemainingLives` | Жизни сразу после действия. |
| `PreviousState`, `State`, `StateChanged` | Состояния до/после действия и признак перехода. |

Результат — снимок одного вызова; будущие ходы его не меняют. Нормальные отказы
gameplay представлены статусами, а не exceptions. Диапазоны координат и значений
проверяются существующими `CellPosition` и `SudokuValue`.

## Notes

`AddNote`, `RemoveNote`, `ToggleNote` принимают клетку и `SudokuValue`.
Notes — пользовательский набор `1..9`, доступный только в пустой editable-клетке.
Они не обязаны совпадать с автоматически вычисленными `Board.GetCandidates`.
Повторное добавление и удаление отсутствующего note — `Unchanged`.
Все три операции на given возвращают `GivenCell`, на заполненной editable-клетке —
`CellNotEmpty`. Notes никогда не расходуют жизни.

Внутреннее хранение — закрытая девятибитовая маска на клетку.
`GetNotes(position)` возвращает отсортированный снимок `IReadOnlyList<SudokuValue>`,
обёрнутый в read-only collection: приведение к `IList` не позволяет изменять данные.
Снимок не меняется при следующих действиях. Установка normal value, включая
неверное, очищает notes только своей клетки. Последующая очистка normal value
их не восстанавливает. Автоматического удаления notes у соседей нет.

## Countdown и terminal behavior

Только `AdvanceTime(TimeSpan delta)` двигает время; ввод значений и notes сам по
себе времени не добавляет. Для `Active` полная delta прибавляется к `ElapsedTime`,
а `RemainingBonusTime` уменьшается с ограничением снизу в `0`. Точность — ticks
`TimeSpan`, без округления и чтения реальных часов. При countdown `0`:

- `IsSpeedBonusAvailable == false`;
- lives, доска, notes и `Active` не меняются;
- можно продолжать ввод и выиграть; elapsed time продолжает накапливаться.

Нулевая delta — no-op. Отрицательная delta всегда вызывает
`ArgumentOutOfRangeException`, включая terminal sessions. Если сумма elapsed time
выходит за `TimeSpan.MaxValue`, delta отвергается тем же исключением **до** изменения
обоих времён. Duration и delta до `TimeSpan.MaxValue` поддерживаются без переполнения
при представимой сумме.

После `Won`/`Lost` все `SetValue`, `ClearValue` и notes mutations возвращают
`SessionEnded` без изменений. Эта проверка предшествует проверкам given/filled.
Неотрицательный `AdvanceTime` — no-op: оба времени и bonus flag заморожены.
`IsSpeedBonusAvailable` описывает оставшееся окно (`RemainingBonusTime > 0`),
в том числе на финише; это не право на выдачу награды после поражения.

## Проверки и границы

`SudokuGameSessionTests` проверяет жизни, ошибки, completion после исправления,
все пять built-in puzzles, givens, каждый вид mutations во всех 81 клетках обеих
terminal states, notes, неизменяемость definition и независимость сессий.
`SudokuGameSessionTimeTests` проверяет ticks, ноль, overshoot, продолжение после
expiry, terminal freeze, отрицательные delta и переполнение. Тесты не используют
random или реальные часы. Команды полного EditMode suite и статической проверки —
в [FOUNDATION.md](../architecture/FOUNDATION.md).

Проверка от 17.09.2026: Unity 6000.6.1f1 скомпилировала проект; полный EditMode
suite — **228/228 Passed** (166 прежних и 62 новых), без ошибок и пропусков,
код завершения — `0`. `Validate-Foundation.ps1` прошёл. Архитектурные тесты
подтвердили отсутствие Unity-ссылок в Domain/Application и сохранение границ
сборок. Manifest, lock-файл и `.asmdef` в рамках KRG-005 не менялись.
Результат и лог: `client/TestResults/krg-005-editmode.xml` и
`client/TestResults/krg-005-editmode.log` (игнорируются Git).
Мобильные сборки не выполнялись.

Модель рассчитана на последовательные обращения, классическое Sudoku 9 × 9.
В KRG-005 нет hints, revive/ads, rewards, XP/currency, difficulty, generator,
animal mapping, UI, звука/анимаций, pause, save/resume/persistence, backend,
analytics, achievements и league scoring. KRG-006 и следующие задачи не начаты.
