param(
    [string]$path,
    [string]$out
)

$asm = [Reflection.Assembly]::LoadFile($path)
$types = @()

try {
    $types = $asm.GetTypes()
} catch [Reflection.ReflectionTypeLoadException] {
    $types = @($_.Exception.Types | Where-Object { $_ -ne $null })
}

$types |
    ForEach-Object { $_.FullName } |
    Sort-Object |
    Out-File -FilePath $out -Encoding UTF8
