# Символы животных — KRG-006

Sudoku внутри использует только `SudokuValue` (`1..9`); пустота — `null`.
Числа задают ограничения строк, столбцов, блоков и canonical correctness.
Выбор животных меняет представление этих значений, а не математическую puzzle.

## Слои

- **Application:** `AnimalId` и `SudokuAnimalSet` — чистый C# без Unity.
  Сейчас ID нужен только контракту представления, поэтому общий animal primitive
  в Domain не вводится. Mapping использует существующий `SudokuValue` через
  уже разрешённую ссылку Application → Domain.
- **Infrastructure:** `BuiltInSudokuAnimalSet.Default` — фиксированный набор
  для разработки, по аналогии со встроенным puzzle content. Это данные, не каталог
  животных и не сервис управления выбором.
- **Domain:** не знает animal-типов. `SudokuPuzzle`, `SudokuSolver`,
  `SudokuSolution`, `SudokuBoard`, `SudokuPuzzleDefinition` и `SudokuGameSession`
  не принимают и не хранят animal mapping.

Новые сборки и ссылки не нужны. Presentation уже имеет доступ к контракту
Application; готовую конфигурацию можно передать извне. Runtime-связь Presentation
с Infrastructure в этой задаче не добавляется.

## AnimalId

`new AnimalId(string value)` создаёт неизменяемый `sealed` класс со строковым
свойством `Value`; `ToString()` возвращает ту же строку.
`null` отвергается через `ArgumentNullException`, пустая строка и строка только
из whitespace — через `ArgumentException`. Reference type выбран, чтобы
`default` не создавал экземпляр с невалидным ID; `null` внутри набора запрещён.

Сравнение регистрозависимое, `StringComparer.Ordinal`, без зависимости от культуры.
Строка сохраняется точно: нет trim, смены регистра или Unicode-нормализации.
`fox`, `FOX` и ` fox ` — разные ID. `Equals`, `==`, `!=` и `GetHashCode`
согласованы; два экземпляра с `fox` равны. Постоянным идентификатором является
строка, а не hash code. ID назначает вызывающий код; runtime GUID не генерируются.

## Набор и mapping

`new SudokuAnimalSet(IReadOnlyList<AnimalId> orderedAnimals)` требует ровно
**9 непустых, уникальных AnimalId**. Уникальность определяется равенством ID,
а не ссылок. `null` вместо списка вызывает `ArgumentNullException`, неверное
количество, `null`-элемент или повтор ID — `ArgumentException`.

| API | Контракт |
| --- | --- |
| `GetAnimal(SudokuValue value)` | Элемент с индексом `value.Value - 1`: первое животное соответствует 1, девятое — 9. |
| `GetValue(AnimalId animal)` | Ищет ID в фиксированном порядке и возвращает индекс + 1 как `SudokuValue`. |
| `Animals` | Неизменяемый ordered snapshot, `IReadOnlyList<AnimalId>`, ровно 9 элементов. |

Обратный поиск использует то же ordinal equality и не более девяти сравнений.
Неизвестный ID вызывает `ArgumentException`, `null` — `ArgumentNullException`.
Пустая Sudoku-клетка не имеет животного: потребитель проверяет nullable value
перед обращением к mapping.

Конструктор копирует элементы в собственный массив и закрывает его
`ReadOnlyCollection`. Массив не отдаётся наружу; даже приведение `Animals`
к generic/non-generic `IList` не разрешает mutation. Элементы также неизменяемы.
Снимок можно безопасно хранить и повторно читать без новых копий.

Замена или reorder — создание нового `SudokuAnimalSet` из нового ordered списка.
Исходный набор и ранее полученные снимки сохраняются. Например:

```csharp
var animals = current.Animals.ToArray();
animals[0] = new AnimalId("otter");
var next = new SudokuAnimalSet(animals);
```

Пример использует `System.Linq`; duplicate ID по-прежнему запрещён.
Mutable manager и состояние выбранного игроком набора не реализованы.

## Набор для разработки

`Krugos.Infrastructure.BuiltInSudokuAnimalSet.Default` возвращает общий
неизменяемый набор. Порядок фиксирован:

| SudokuValue | AnimalId |
| --- | --- |
| 1 | fox |
| 2 | panda |
| 3 | elephant |
| 4 | giraffe |
| 5 | lion |
| 6 | zebra |
| 7 | monkey |
| 8 | hippo |
| 9 | tiger |

Это только IDs. Возможность создать произвольный набор из девяти уникальных ID
не означает владение животными или проверку unlocks.

## Инвариант и проверки

Одна `SudokuPuzzleDefinition` может использоваться с разными animal-наборами.
Givens, canonical solution, numeric board, notes, lives, timer и правила победы
или поражения от этого не меняются. Даже если один и тот же ID после reorder
обозначает другое число, это новая конфигурация отображения; переписывать числа
в puzzle или session не требуется.

Регрессия `AnimalRepresentationRegressionTests` использует одну definition,
два разных mapping и две независимые сессии. Проверяются как циклический reorder,
так и замена всех девяти животных, для сценариев победы и поражения. Ввод животных
преобразуется обратно в одинаковые `SudokuValue`; результаты каждого хода и полное
наблюдаемое состояние сессий совпадают. Чтение доски и notes через новый mapping
не меняет уже начатую сессию. Givens и solution сверяются со снимками,
повторный запуск solver возвращает тот же canonical solution.

Остальные тесты проверяют ID, обе стороны mapping для всех девяти значений,
ошибочный ввод, порядок, defensive copy, запрет mutation через `IList`,
независимость новых наборов и фиксированный development set.
Команды Unity EditMode и статической проверки — в
[FOUNDATION.md](../architecture/FOUNDATION.md).

Проверка от 17.09.2026: Unity 6000.6.1f1 скомпилировала проект; весь EditMode
suite — **279/279 Passed** (228 прежних и 51 новый), без ошибок и пропусков,
код завершения — `0`. Новые тесты: 15 для `AnimalId`, 32 для набора и 4 регрессии.
Архитектурные тесты подтвердили фактический граф и отсутствие Unity-ссылок
в Domain/Application. `Validate-Foundation.ps1` прошёл; `.asmdef`, manifest,
lock-файл и существующий код Sudoku не менялись в KRG-006.
Результат и лог находятся в игнорируемых `client/TestResults/krg-006-editmode.xml`
и `client/TestResults/krg-006-editmode.log`. Мобильные сборки не выполнялись.

## Ограничения

Collection/progression, каталог и metadata животных, assets, save/load,
UI выбора и gameplay screen отложены. Сетевых сервисов, пакетов, Unity-компонентов,
новых правил игры и state-management framework нет. KRG-007 не начата.
