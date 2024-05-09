Param (
    [Parameter(HelpMessage = "OneDrive tool generated local file in CSV format.")]
    [string] $LocalCSVFile,

    [Parameter(HelpMessage = "Local folder to use.")]
    [string] $LocalFolder
)

# Convert .mts to .mp4
if ([string]::IsNullOrEmpty($LocalCSVFile) -eq $false) {
    $csv = Import-Csv -Path $LocalCSVFile -Delimiter ";"
    $mtsFiles = $csv | Where-Object -Property Name -Value "*.mts" -Like | Select-Object @{
        Name       = 'Output';
        Expression = { $_.Path + "/" + $_.Name }
    } | Select-Object -ExpandProperty Output
}
elseif ([string]::IsNullOrEmpty($LocalFolder) -eq $false) {
    $mtsFiles = Get-ChildItem -Path $LocalFolder -Filter "*.mts" | Select-Object -Property FullName -ExpandProperty FullName
}
else {
    throw "Either -LocalCSVFile or -LocalFolder must be provided."
}

$mtsFiles | Format-Table
"$($mtsFiles.Count) .mts files found."

foreach ($mtsFile in $mtsFiles) {
    $source = $mtsFile
    $destination = $source -replace ".mts", ".mp4"

    if (Test-Path $destination) {
        "Destination file already exists: $destination. Skip conversion."
    }
    else {
        ffmpeg -i $source -c copy $destination
    }

    $sourceSize = (Get-Item $source).Length / 1MB
    $destinationSize = (Get-Item $destination).Length / 1MB

    "Source size: $sourceSize MB, Destination size: $destinationSize MB -> $([math]::Round($destinationSize / $sourceSize, 2))x"
}
