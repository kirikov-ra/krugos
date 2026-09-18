# Основание Krugos — KRG-001

## Baseline и расположение

Production-проект расположен в `client/`. Baseline — **Unity 6000.6.1f1**,
revision `7efac9f6c10e`, C#, режим 2D и Built-in Render Pipeline.
Версия явно согласована пользователем вместо первоначально запрошенной линейки;
фактические версия и revision записаны в `ProjectSettings/ProjectVersion.txt`.
Целевые платформы — Android и iOS.

Прежний React/Phaser-прототип остаётся в корневых `src/`, `public/` и npm/Vite-файлах.
Backend не создан. KRG-002–KRG-006 добавили Sudoku Domain, встроенный контент,
игровую сессию и animal mapping. KRG-007 добавляет первый playable на runtime
UI Toolkit: `Assets/Krugos/Scenes/SudokuGameplay.unity`.
См. [GAMEPLAY_PRESENTATION.md](../systems/GAMEPLAY_PRESENTATION.md).

## Слои и зависимости

| Слой в `client/Assets/Krugos/` | Ответственность | Разрешённые ссылки на слои |
| --- | --- | --- |
| Domain | Sudoku, solver, definition, игровая сессия | Нет |
| Application | Контракт контента и animal mapping | Domain |
| Infrastructure | Встроенные puzzles и development animal set | Application, Domain |
| Presentation | UI Toolkit view/presenter, ввод и отображение сессии | Application, Domain |
| Bootstrap | Верхний runtime composition root | Presentation, Infrastructure, Application, Domain |
| Editor | Открытие и явное создание gameplay scene | Bootstrap |
| Tests/EditMode | Архитектурные и функциональные тесты | Domain, Application, Infrastructure, Presentation, Bootstrap |
| Tests/PlayMode | Smoke tests production scene и UI | Domain, Application, Presentation, Bootstrap |

```text
Krugos.Application    -> Krugos.Domain
Krugos.Infrastructure -> Krugos.Application, Krugos.Domain
Krugos.Presentation   -> Krugos.Application, Krugos.Domain
Krugos.Bootstrap      -> Krugos.Presentation, Krugos.Infrastructure, Krugos.Application, Krugos.Domain
Krugos.Editor         -> Krugos.Bootstrap
Krugos.Tests.EditMode -> Krugos.Domain, Krugos.Application, Krugos.Infrastructure, Krugos.Presentation, Krugos.Bootstrap
Krugos.Tests.PlayMode -> Krugos.Domain, Krugos.Application, Krugos.Presentation, Krugos.Bootstrap
```

Presentation может использовать UnityEngine. Infrastructure может использовать
Unity API при необходимости конкретного адаптера. Editor и Tests.EditMode
ограничены `includePlatforms: ["Editor"]`; runtime-сборки на них не ссылаются.
Тестовая сборка использует Unity Test Framework, NUnit и UnityEditor для проверки
фактического графа компиляции.

Bootstrap связывает готовый provider/default animal set с Presentation без DI
framework. Domain/Application/Infrastructure/Presentation **не ссылаются на
Bootstrap**. Presentation **не ссылается на Infrastructure**. Тестовые сборки
ограничены `UNITY_INCLUDE_TESTS`; Tests.PlayMode использует только runtime API
Test Framework, а Tests.EditMode также UnityEditor.TestRunner.

В Domain и Application установлено `noEngineReferences: true`.
Domain должен компилироваться и тестироваться как обычный C#, без Unity:
игровые правила не должны зависеть от сцены или платформенного SDK.
У всех сборок явные ссылки, `autoReferenced: false` и `overrideReferences: true`;
автоматические ссылки на сторонние DLL отключены. Исключение в списке DLL —
`nunit.framework.dll` только для Tests.EditMode и Tests.PlayMode.

Файлы `AssemblyInfo.cs` содержат описание сборки и сохранены из KRG-001.

Будущие платформенные SDK и адаптеры хранения, HTTP, аналитики и монетизации
должны располагаться в Infrastructure. Инструменты их настройки в редакторе —
в Editor. В этой задаче таких интеграций и абстракций нет.

## Настройки и пакеты

ProjectSettings сформированы установленным редактором. Имя продукта — Krugos;
редактор работает в режиме 2D, сериализация — Force Text, `.meta` видимы и сохраняются
в Git. Gameplay scene включена в Editor Build Settings; дополнительный rendering pipeline не установлен.
Настройки платформ, подписи и идентификаторы публикации не подбирались искусственно.

Прямые зависимости: `com.unity.test-framework: 1.8.0` и добавленный в KRG-007
`com.unity.modules.uielements: 1.0.0`. Последний необходим для runtime UI Toolkit;
это встроенный модуль установленного редактора. Переход с подготовленной ранее версии
пакета необходим для согласования с новой baseline редактора, а не для новой функциональности.
Unity разрешил и записал в `packages-lock.json` следующие транзитивные зависимости:

- `com.unity.ext.nunit: 2.1.0` — NUnit для тестов;
- `com.unity.modules.imgui: 1.0.0` — зависимость Test Framework;
- `com.unity.modules.jsonserialize: 1.0.0` — зависимость Test Framework.
- `com.unity.modules.ui`, `com.unity.modules.hierarchycore`,
  `com.unity.modules.physics`, `com.unity.modules.animation`: `1.0.0` —
  штатные зависимости UI Toolkit; imgui/jsonserialize используются совместно.

Все девять записей lock-файла имеют источник `builtin`. Сторонние SDK не добавлены.
При первом импорте каркаса без версии Unity автоматически добавил стандартные пакеты;
этот импорт остановлен, manifest очищен, повторное разрешение выполнено после фиксации
baseline. В KRG-001 manifest содержал только Test Framework и четыре записи в lock;
расширение KRG-007 ограничено необходимым встроенным UI-модулем и его зависимостями.

## Воспроизводимые проверки

Из корня репозитория:

```powershell
pwsh -File tools/Validate-Foundation.ps1
```

Скрипт проверяет объявленный граф, ограничения Editor/Tests, настройки Domain/Application,
пары asset/.meta, версию редактора, manifest/lock-файл и правила Git.
Он не подменяет Unity-компиляцию и Test Runner.

В редакторе открыть `client/` через Unity 6000.6.1f1, дождаться импорта и выполнить
`Window > General > Test Runner > EditMode > Run All`.
Для запуска без интерфейса из корня (при необходимости изменить путь к редактору):

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.6.1f1\Editor\Unity.exe'
$projectPath = (Resolve-Path client).Path
$resultsPath = Join-Path $projectPath 'TestResults'
New-Item -ItemType Directory -Force $resultsPath | Out-Null
$unityArguments = @(
    '-batchmode', '-nographics',
    '-projectPath', ('"{0}"' -f $projectPath),
    '-runTests', '-testPlatform', 'EditMode',
    '-testResults', ('"{0}"' -f (Join-Path $resultsPath 'editmode.xml')),
    '-logFile', ('"{0}"' -f (Join-Path $resultsPath 'editmode.log'))
)
$process = Start-Process -FilePath $unityEditor -ArgumentList $unityArguments `
    -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Unity failed: $($process.ExitCode)" }
[xml]$results = Get-Content (Join-Path $resultsPath 'editmode.xml') -Raw
if ($results.'test-run'.result -ne 'Passed' -or [int]$results.'test-run'.passed -lt 4) {
    throw 'EditMode tests did not pass or were not discovered.'
}
```

Не добавлять `-quit` к запуску тестов: завершением управляет Test Runner.
Редактору нужен обычный доступ к пользовательской лицензии и кешу пакетов;
первый запуск в ограниченном sandbox не смог их использовать.

## Результаты KRG-001 и ограничения окружения

- Импорт через Unity 6000.6.1f1 завершён с кодом 0; все шесть сборок скомпилированы.
- EditMode: **4/4 Passed**, ошибок компиляции C# в итоговом запуске нет.
- Два случая проверяют ссылки в DLL Domain/Application; ещё два теста проверяют
  реальные ссылки компилятора и отсутствие Editor/Tests в графе player-компиляции.
- Domain/Application не имеют Unity-ссылок ни в DLL, ни в списке входных ссылок
  Unity-компилятора. Ранее также прошла отдельная компиляция обычным .NET Framework
  `csc.exe` без Unity DLL. Игровая логика пока отсутствует.
- Минимальная Android player build **не выполнялась**: Android Build Support не установлен.
- Генерация iOS/Xcode project **не выполнялась**: iOS Build Support не установлен.
  Это отсутствие модуля окружения, а не доказанная невозможность экспорта на Windows.
- Локальная сборка/подпись iOS-приложения через Xcode требует macOS:
  это **platform limitation**, а не дефект KRG-001.
  См. [документацию Unity о процессе сборки iOS](https://docs.unity3d.com/6000.0/Documentation/Manual/iphone-BuildProcess.html).
- В успешных запусках есть диагностические сообщения редактора о раскладке клавиатуры
  и сетевых запросах Unity (включая timeout UnityConnect при завершении). Они не
  являются ошибками C# и не помешали импорту или прохождению тестов.

Логи `import.log`, `editmode.log` и результат `editmode.xml` находятся в игнорируемом
`client/TestResults/`. Library, кеши, результаты тестов и сборок не входят в Git.
Полная mobile player build не подтверждена; проверка графа player-компиляции
не выдаётся за сборку приложения. KRG-001 завершена в рамках согласованной baseline
и условных проверок доступных платформенных модулей.
