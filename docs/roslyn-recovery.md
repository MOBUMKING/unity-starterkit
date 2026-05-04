# Roslyn 백엔드 복구 가이드

Unity MCP `execute_code` 자가 검증의 컴파일 백엔드(Roslyn)를 잃어버렸을 때 복구하는 절차.

## 손실 신호

다음 중 하나가 감지되면 Roslyn DLL이 빠진 상태다.

- `execute_code` 응답에 `"compiler":"codedom"`
- `"파일 이름이나 확장명이 너무 깁니다"` 에러로 mono.exe 호출 실패
- `Assets/Plugins/Roslyn/` 폴더 부재 또는 DLL 4개 중 누락

## 왜 CodeDom 폴백은 안 되는가

Unity 6 + URP + 다수 패키지(UniTask, DOTween, Newtonsoft 등) 환경에서 CodeDom의 `CSharpCodeProvider`는 mono.exe를 외부 프로세스로 호출한다. 이때 어셈블리 reference 인자(`-r:<path>` 100여 개)가 Windows CreateProcess CommandLine 한계(32KB)에 걸려 컴파일이 실패한다. PATH 길이 정리로는 해소되지 않는 구조적 문제이며, in-process 컴파일러인 Roslyn으로 우회하는 것이 유일한 해결책이다.

## Roslyn DLL 기준값

- 경로: `Assets/Plugins/Roslyn/`
- 4개 DLL (총 약 12.4MB):

| DLL | 버전 | NuGet 패키지 |
|-----|------|------------|
| `Microsoft.CodeAnalysis.dll` | 4.12.0 | `microsoft.codeanalysis.common` |
| `Microsoft.CodeAnalysis.CSharp.dll` | 4.12.0 | `microsoft.codeanalysis.csharp` |
| `System.Collections.Immutable.dll` | 8.0.0 | `system.collections.immutable` |
| `System.Reflection.Metadata.dll` | 8.0.0 | `system.reflection.metadata` |

DLL 배치 자체는 자가 검증 우선 원칙 3조건(기존 코드 미변경 + 저비용 + 자가 검증 가능)을 충족하므로 손실 감지 시 **사용자 컨펌 없이 즉시 복구**한다.

## 복구 절차 (우선순위 순)

### 1순위: PowerShell 자동 설치

Roslyn 미설치 상태에서는 `execute_code`로 인스톨러 메소드(`MCPForUnity.Editor.Setup.RoslynInstaller.Install`) 호출 자체가 불가하므로 이 우회가 필수다.

```powershell
$ErrorActionPreference = 'Stop'
$pluginsDir = Join-Path (Get-Location) 'Assets\Plugins\Roslyn'
New-Item -ItemType Directory -Path $pluginsDir -Force | Out-Null

$entries = @(
  @{ pkg = 'microsoft.codeanalysis.common'; ver = '4.12.0'; src = 'lib/netstandard2.0/Microsoft.CodeAnalysis.dll'; dst = 'Microsoft.CodeAnalysis.dll' },
  @{ pkg = 'microsoft.codeanalysis.csharp'; ver = '4.12.0'; src = 'lib/netstandard2.0/Microsoft.CodeAnalysis.CSharp.dll'; dst = 'Microsoft.CodeAnalysis.CSharp.dll' },
  @{ pkg = 'system.collections.immutable'; ver = '8.0.0'; src = 'lib/netstandard2.0/System.Collections.Immutable.dll'; dst = 'System.Collections.Immutable.dll' },
  @{ pkg = 'system.reflection.metadata'; ver = '8.0.0'; src = 'lib/netstandard2.0/System.Reflection.Metadata.dll'; dst = 'System.Reflection.Metadata.dll' }
)

$tmp = Join-Path $env:TEMP ("roslyn-install-" + [guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory -Path $tmp -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem

foreach ($e in $entries) {
  $url = "https://api.nuget.org/v3-flatcontainer/$($e.pkg)/$($e.ver)/$($e.pkg).$($e.ver).nupkg"
  $nupkg = Join-Path $tmp "$($e.pkg).$($e.ver).nupkg"
  Invoke-WebRequest -Uri $url -OutFile $nupkg -UseBasicParsing -TimeoutSec 60
  $zip = [System.IO.Compression.ZipFile]::OpenRead($nupkg)
  $entry = $zip.Entries | Where-Object { $_.FullName.Replace('\','/') -ieq $e.src } | Select-Object -First 1
  if ($entry) {
    [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Join-Path $pluginsDir $e.dst), $true)
  }
  $zip.Dispose()
}
Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
```

이후 Unity에 import 트리거:

```
mcp__UnityMCP__refresh_unity(mode=force, scope=all, compile=request, wait_for_ready=true)
```

### 2순위: 공식 UI (사용자 클릭 필요 — 자동화 불가)

Unity Editor에서:

1. `Window > MCP For Unity > Toggle MCP Window` (단축키 `Ctrl+Shift+M`)
2. Scripts/Validation 탭의 "Install Roslyn DLLs" 버튼 클릭

> 메뉴 `Window/MCP For Unity/Install Roslyn DLLs`는 v9.6.8에서 메뉴바에 등록되어 있지 않다 — `execute_menu_item`으로 직접 호출 불가. EditorWindow 내부 버튼 콜백으로만 노출.

## 검증

복구 후 `execute_code` 한 줄로 PASS 확정:

```csharp
// compiler='auto'로 호출
var t = System.Type.GetType("Microsoft.CodeAnalysis.CSharp.CSharpCompilation, Microsoft.CodeAnalysis.CSharp");
return $"Roslyn loaded={t != null} | location={t?.Assembly.Location}";
```

응답의 메타데이터에서 다음을 확인:

- `"compiler":"roslyn"` (auto가 Roslyn으로 디스패치)
- `Roslyn loaded=True`
- 어셈블리 위치가 `Assets\Plugins\Roslyn\Microsoft.CodeAnalysis.CSharp.dll`

## README와의 차이 (주의)

MCP for Unity 공식 README는 다음 절차를 안내하지만, **v9.6.8에서는 모두 불필요**하다 (`Editor/Setup/RoslynInstaller.cs` 코드로 검증 완료):

- ❌ NuGetForUnity 설치
- ❌ `SQLitePCLRaw.core` / `SQLitePCLRaw.bundle_e_sqlite3` v3.0.2 설치
- ❌ Scripting Define Symbols에 `USE_ROSLYN` 추가
- ❌ Microsoft.CodeAnalysis v5.0 (실제 인스톨러는 4.12.0 사용)

README보다 패키지 인스톨러 코드가 진실이며, 이 가이드는 코드 기준으로 작성됨.

## 이력

- 2026-05-04: 초기 손실 → PowerShell 자동 설치로 복구 → 본 가이드 작성
