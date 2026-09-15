$ErrorActionPreference = 'Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    $compiler = 'C:/Program Files/dotnet/sdk/10.0.401/Roslyn/bincore/csc.dll'
    if (-not (Test-Path $compiler)) { throw 'Update the compiler path for your installed .NET SDK.' }
    New-Item -ItemType Directory -Path Temp/DeskChecks -Force | Out-Null
    foreach ($assembly in @('Assembly-CSharp', 'Assembly-CSharp-Editor')) {
        $project = [xml](Get-Content "$assembly.csproj" -Raw)
        $refs = @($project.SelectNodes("//*[local-name()='HintPath']") |
            Where-Object { $_.InnerText -notmatch '[\\/]Assembly-CSharp\.dll$' } |
            ForEach-Object { '-r:"' + $_.InnerText + '"' })
        $sources = @($project.SelectNodes("//*[local-name()='Compile']") |
            ForEach-Object { $_.GetAttribute('Include') } |
            Where-Object { $_ -and (Test-Path -LiteralPath $_) })
        if ($assembly -eq 'Assembly-CSharp') {
            $sources += @(Get-ChildItem Assets/Scripts -Filter '*.cs' | ForEach-Object FullName)
        } else {
            $refs += '-r:"Temp/DeskChecks/Assembly-CSharp.dll"'
            $sources += @(Get-ChildItem Assets/Editor -Filter '*.cs' | ForEach-Object FullName)
        }
        $sources = @($sources | ForEach-Object { (Resolve-Path -LiteralPath $_).Path } |
            Sort-Object -Unique | ForEach-Object { '"' + $_ + '"' })
        $defines = $project.SelectSingleNode("//*[local-name()='DefineConstants']").InnerText
        $arguments = @('-nologo', '-nostdlib+', '-target:library', '-langversion:9.0',
            "-out:Temp/DeskChecks/$assembly.dll", "-define:$defines") + $refs + $sources
        $response = "Temp/DeskChecks/$assembly.rsp"
        Set-Content -LiteralPath $response -Value $arguments
        & dotnet $compiler "@$response"
        if ($LASTEXITCODE -ne 0) { throw "$assembly compilation failed." }
        Write-Output "$assembly compiled successfully."
    }
} finally { Pop-Location }
