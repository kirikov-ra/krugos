$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repository = Split-Path $PSScriptRoot -Parent
$assetRoot = Join-Path $repository 'client/Assets'
$expected = @{
    'Krugos.Domain' = @()
    'Krugos.Application' = @('Krugos.Domain')
    'Krugos.Infrastructure' = @('Krugos.Application', 'Krugos.Domain')
    'Krugos.Presentation' = @('Krugos.Application', 'Krugos.Domain')
    'Krugos.Editor' = @()
    'Krugos.Tests.EditMode' = @(
        'Krugos.Domain', 'Krugos.Application', 'UnityEngine.TestRunner', 'UnityEditor.TestRunner'
    )
}

function Assert-Condition($condition, [string]$message) {
    if (!$condition) { throw $message }
}

function Assert-SameSet($actual, $wanted, [string]$message) {
    Assert-Condition ((@($actual | Sort-Object) -join '|') -ceq
        (@($wanted | Sort-Object) -join '|')) $message
}

$definitions = @(Get-ChildItem $assetRoot -Recurse -Filter '*.asmdef')
Assert-Condition ($definitions.Count -eq $expected.Count) 'Expected exactly six assembly definitions.'
$names = @()
foreach ($file in $definitions) {
    $definition = Get-Content $file.FullName -Raw | ConvertFrom-Json
    $name = $definition.name
    Assert-Condition ($expected.ContainsKey($name)) "Unexpected assembly: $name"
    $names += $name
    Assert-SameSet $definition.references $expected[$name] "Incorrect dependencies: $name"
    $editorOnly = $name -in @('Krugos.Editor', 'Krugos.Tests.EditMode')
    Assert-SameSet $definition.includePlatforms @(if ($editorOnly) { 'Editor' }) "Incorrect platform: $name"
    Assert-SameSet $definition.excludePlatforms @() "Unexpected platform exclusions: $name"
    Assert-Condition (!$definition.autoReferenced -and $definition.overrideReferences) "Implicit references enabled: $name"
    $pure = $name -in @('Krugos.Domain', 'Krugos.Application')
    Assert-Condition ($definition.noEngineReferences -eq $pure) "Incorrect engine reference setting: $name"
    $isTest = $name -eq 'Krugos.Tests.EditMode'
    Assert-SameSet $definition.precompiledReferences @(if ($isTest) { 'nunit.framework.dll' }) "Unexpected DLL: $name"
    Assert-SameSet $definition.defineConstraints @(if ($isTest) { 'UNITY_INCLUDE_TESTS' }) "Unexpected constraints: $name"
}
Assert-SameSet $names @($expected.Keys) 'Duplicate or missing assembly names.'

$guids = @()
foreach ($asset in Get-ChildItem $assetRoot -Recurse | Where-Object Extension -ne '.meta') {
    Assert-Condition (Test-Path ($asset.FullName + '.meta')) "Missing metadata: $($asset.FullName)"
}
foreach ($meta in Get-ChildItem $assetRoot -Recurse -Filter '*.meta') {
    Assert-Condition (Test-Path $meta.FullName.Substring(0, $meta.FullName.Length - 5)) "Orphan metadata: $($meta.FullName)"
    $match = [regex]::Match((Get-Content $meta.FullName -Raw), '(?m)^guid: ([0-9a-f]{32})\r?$')
    Assert-Condition $match.Success "Invalid GUID: $($meta.FullName)"
    $guids += $match.Groups[1].Value
}
Assert-Condition (@($guids | Sort-Object -Unique).Count -eq $guids.Count) 'Duplicate asset GUIDs.'

$manifest = Get-Content (Join-Path $repository 'client/Packages/manifest.json') -Raw | ConvertFrom-Json
Assert-SameSet @($manifest.dependencies.PSObject.Properties.Name) @('com.unity.test-framework') 'Unexpected direct package dependencies.'
Assert-Condition ($manifest.dependencies.'com.unity.test-framework' -eq '1.8.0') 'Unexpected Test Framework version.'

$version = Get-Content (Join-Path $repository 'client/ProjectSettings/ProjectVersion.txt')
Assert-Condition ($version -contains 'm_EditorVersion: 6000.6.1f1') 'Unexpected Unity baseline.'
Assert-Condition ($version -contains 'm_EditorVersionWithRevision: 6000.6.1f1 (7efac9f6c10e)') 'Unexpected Unity revision.'
$lock = Get-Content (Join-Path $repository 'client/Packages/packages-lock.json') -Raw | ConvertFrom-Json
$lockedVersions = @{
    'com.unity.test-framework' = '1.8.0'
    'com.unity.ext.nunit' = '2.1.0'
    'com.unity.modules.imgui' = '1.0.0'
    'com.unity.modules.jsonserialize' = '1.0.0'
}
Assert-SameSet @($lock.dependencies.PSObject.Properties.Name) @($lockedVersions.Keys) 'Unexpected resolved dependencies.'
foreach ($package in $lockedVersions.Keys) {
    Assert-Condition ($lock.dependencies.$package.version -eq $lockedVersions[$package]) "Unexpected resolved version: $package"
    Assert-Condition ($lock.dependencies.$package.source -eq 'builtin') "Unexpected package source: $package"
}

$gitArguments = @('-c', "safe.directory=$($repository.Replace('\', '/'))", '-C', $repository)
$tracked = @(& git @gitArguments ls-files)
Assert-Condition ($LASTEXITCODE -eq 0) 'Unable to read Git index.'
$generated = '^client/(Library|Temp|Obj|Logs|UserSettings|Builds?|TestResults|MemoryCaptures|Recordings|\.vs|\.idea|\.vscode)/'
Assert-Condition (@($tracked | Where-Object { $_ -match $generated }).Count -eq 0) 'Generated Unity files are tracked.'
foreach ($folder in @('Library', 'Temp', 'Obj', 'Logs', 'UserSettings', 'Build', 'Builds', 'TestResults', 'MemoryCaptures', 'Recordings', '.vs', '.idea', '.vscode')) {
    & git @gitArguments check-ignore --quiet "client/$folder/probe.txt"
    Assert-Condition ($LASTEXITCODE -eq 0) "Generated folder is not ignored: $folder"
}
foreach ($path in @('client/Assets/Krugos.meta', 'client/Packages/manifest.json', 'client/Packages/packages-lock.json', 'client/ProjectSettings/ProjectVersion.txt')) {
    & git @gitArguments check-ignore --quiet $path
    Assert-Condition ($LASTEXITCODE -eq 1) "Required project file is ignored or Git failed: $path"
}
Write-Output 'PASS: assembly configuration, editor isolation, asset metadata, package manifest and Git hygiene.'
Write-Output 'Unity import, compilation and EditMode tests require Unity 6000.6.1f1; this script does not run them.'
