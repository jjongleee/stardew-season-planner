$Host.UI.RawUI.WindowTitle = "SeasonPlanner Debug Console"
$pipe = New-Object System.IO.Pipes.NamedPipeClientStream('.', 'SeasonPlannerDebug', [System.IO.Pipes.PipeDirection]::In)
$pipe.Connect(5000)
$reader = New-Object System.IO.StreamReader($pipe)
while ($true) {
    $line = $reader.ReadLine()
    if ($line -eq $null) { break }
    Write-Host $line
}
