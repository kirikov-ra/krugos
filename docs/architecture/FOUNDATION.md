# Основание Krugos — KRG-001

## Baseline и расположение

Production-проект расположен в `client/`. Baseline — **Unity 6000.6.1f1**,
revision `7efac9f6c10e`, C#, режим 2D и Built-in Render Pipeline.
Версия явно согласована пользователем вместо первоначально запрошенной линейки;
фактические версия и revision записаны в `ProjectSettings/ProjectVersion.txt`.
Целевые платформы — Android и iOS.

Прежний React/Phaser-прототип остаётся в корневых `src/`, `public/` и npm/Vite-файлах.
Backend не создан. Игровой логики, UI, сцен и интеграций в Unity-проекте нет.
Для импорта и EditMode-тестов bootstrap-сцена не понадобилась.

## Слои и зависимости

| Слой в `client/Assets/Krugos/` | Ответственность | Разрешённые ссылки на слои |
| --- | --- | --- |
| Domain | Чистые игровые и бизнес-правила; пока логики нет | Нет |
| Application | Сценарии использования и оркестрация; пока логики нет | Domain |
| Infrastructure | Место внешних адаптеров; пока интеграций нет | Application, Domain |
| Presentation | Unity-представление; пока компонентов нет | Application, Domain |
| Editor | Только инструменты редактора; пока инструментов нет | Пока нет |
| Tests/EditMode | Архитектурные и функциональные тесты, включая built-in provider KRG-004 | Domain, Application, Infrastructure |

```text
Krugos.Application    -> Krugos.Domain
Krugos.Infrastructure -> Krugos.Application, Krugos.Domain
Krugos.Presentation   -> Krugos.Application, Krugos.Domain
Krugos.Editor         -> [нет ссылок на сборки Krugos]
Krugos.Tests.EditMode -> Krugos.Domain, Krugos.Application, Krugos.Infrastructure
```

Presentation может использовать UnityEngine. Infrastructure может использовать
Unity API при необходимости конкретного адаптера. Editor и Tests.EditMode
ограничены `includePlatforms: ["Editor"]`; runtime-сборки на них не ссылаются.
Тестовая сборка использует Unity Test Framework, NUnit и UnityEditor для проверки
фактического графа компиляции.

В KRG-004 добавлена явная ссылка Tests.EditMode на Infrastructure для проверки
встроенного provider. Направления зависимостей runtime-сборок не изменены.

В Domain и Application установлено `noEngineReferences: true`.
Domain должен компилироваться и тестироваться как обычный C#, без Unity:
игровые правила не должны зависеть от сцены или платформенного SDK.
У всех сборок явные ссылки, `autoReferenced: false` и `overrideReferences: true`;
автоматические ссылки на сторонние DLL отключены. Исключение в списке DLL —
`nunit.framework.dll` только для Tests.EditMode.

Файлы `AssemblyInfo.cs` содержат лишь описание сборки: они позволяют компилятору
создавать пока пустые сборки для проверки границ. Игровых типов, сервисов,
интерфейсов и runtime-инициализации нет.

Будущие платформенные SDK и адаптеры хранения, HTTP, аналитики и монетизации
должны располагаться в Infrastructure. Инструменты их настройки в редакторе —
в Editor. В этой задаче таких интеграций и абстракций нет.

## Настройки и пакеты

ProjectSettings сформированы установленным редактором. Имя продукта — Krugos;
редактор работает в режиме 2D, сериализация — Force Text, `.meta` видимы и сохраняются
в Git. Список сцен пуст; дополнительный rendering pipeline не установлен.
Настройки платформ, подписи и идентификаторы публикации не подбирались искусственно.

Прямая зависимость одна: `com.unity.test-framework: 1.8.0`, встроенная версия
Test Framework установленного редактора. Переход с подготовленной ранее версии
пакета необходим для согласования с новой baseline редактора, а не для новой функциональности.
Unity разрешил и записал в `packages-lock.json` следующие транзитивные зависимости:

- `com.unity.ext.nunit: 2.1.0` — NUnit для тестов;
- `com.unity.modules.imgui: 1.0.0` — зависимость Test Framework;
- `com.unity.modules.jsonserialize: 1.0.0` — зависимость Test Framework.

Все четыре записи lock-файла имеют источник `builtin`. Сторонние SDK не добавлены.
При первом импорте каркаса без версии Unity автоматически добавил стандартные пакеты;
этот импорт остановлен, manifest очищен, повторное разрешение выполнено после фиксации
baseline. Итоговый manifest содержит только Test Framework; lock-файл и PackageCache —
четыре указанных пакета.

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
