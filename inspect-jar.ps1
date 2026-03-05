param(
    [string]$jar
)

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($jar)
$zip.Entries | Where-Object { $_.FullName -like '*QuPathBridgeApi*' } | Select-Object FullName
