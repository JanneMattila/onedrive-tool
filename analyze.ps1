Param (
    [Parameter(HelpMessage = "OneDrive tool generated local file in CSV format.")]
    [string] $LocalCSVFile
)

$csv = Import-Csv -Path $LocalCSVFile -Delimiter ";"

$filesMissingFromOneDrive = $csv | Where-Object -Property InOneDrive -Value "FALSE" -EQ 

$filesMissingFromOneDrive | Format-Table
"$($filesMissingFromOneDrive.Count) files found which are not available in OneDrive"

$jpgFiles = $filesMissingFromOneDrive | Where-Object -Property Name -Value "*.jpg" -Like | Select-Object @{
    Name       = 'Output';
    Expression = { $_.Path + "/" + $_.Name }
} | Select-Object -ExpandProperty Output

$allFiles = $filesMissingFromOneDrive | Select-Object @{
    Name       = 'Output';
    Expression = { $_.Path + "/" + $_.Name }
} | Select-Object -ExpandProperty Output

$allFiles = $filesMissingFromOneDrive | Where-Object -Property Name -Value "*.info" -NotLike | Where-Object -Property Name -Value "*.db" -NotLike | Where-Object -Property Name -Value "*.ini" -NotLike | Select-Object @{
    Name       = 'Output';
    Expression = { $_.Path + "/" + $_.Name }
} | Select-Object -ExpandProperty Output

$allFiles.Count

Copy-Item $allFiles \temp\copies -Force

$targetFiles = Get-ChildItem -Path \temp\copies | Select-Object -Property FullName -ExpandProperty FullName
$targetFiles.Count

$mtsFiles = Get-ChildItem -Path \temp\copies | Where-Object -Property Name -Value "*.mts" -Like | Select-Object @{
    Name       = 'Output';
    Expression = { $_.Path + "/" + $_.Name }
} | Select-Object -ExpandProperty Output

$mtsFiles

$allFiles | Where-Object { $_ -NotIn $targetFiles } 

foreach ($source in $allFiles) {

    $fileSource = Split-Path -Path $source -Leaf -Resolve
    $fileTarget = $targetFiles | Where-Object -Property Name -Value $fileSource -EQ

    if ($null -eq $fileTarget) {
        "Destination file does not exists: $source. Copy."
        Copy-Item $source \temp\copies -Force
    }
    else {
        # "Destination file already exists: $source. Skip."
    }
}

$index = 10000
foreach ($source in $allFiles) {

    # $destination = Split-Path -Path $source -Leaf -Resolve
    $destinationExtension = Split-Path -Path $source -Resolve -Extension
    # $destination = Join-Path -Path \temp\copies -ChildPath $destination

    Copy-Item $source "\temp\copies\$index$destinationExtension" -Force
    $index++

    # if (Test-Path $destination) {
    #     # "Destination file already exists: $source. Skip."
    # }
    # else {
    #     "Destination file does not exists: $source. Copy."

    #     try {
    #         Copy-Item $source \temp\copies -Force
    #     }
    #     catch {
    #         $source
    #     }
    # }
}