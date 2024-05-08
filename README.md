# OneDrive tool

[![CI](https://github.com/JanneMattila/onedrive-tool/actions/workflows/ci.yml/badge.svg)](https://github.com/JanneMattila/onedrive-tool/actions/workflows/ci.yml) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)


## Background

You might end up into situation that you have filled your
personal OneDrive with many duplicate files. This can
quite easily happen in you take a lot of pictures with
your different cameras and then upload then over time
to different folders. This can eat up your quota quite
easily. This tool tries to make it easier to find duplicates
and then provide means to clean them up.

You can also have backup drives or USB sticks and you might
not be aware if all those files have been successfully uploaded to OneDrive.

OneDrive tool tries to solve the above problems.

## Usage

Usage instructions:

```
Description:

    ___             ____       _              _              _
   / _ \ _ __   ___|  _ \ _ __(_)_   _____   | |_ ___   ___ | |
  | | | | '_ \ / _ \ | | | '__| \ \ / / _ \  | __/ _ \ / _ \| |
  | |_| | | | |  __/ |_| | |  | |\ V /  __/  | || (_) | (_) | |
  \___ /|_| |_|\___|____/|_|  |_| \_/ \___|   \__\___/ \___/|_|

  More information can be found here:
  https://github.com/JanneMattila/onedrive-tool

Usage:
  OneDriveTool [options]

Options:
  -f, --file <file> (REQUIRED)  CSV file to use
  -e, --export                  Export OneDrive metadata
  -a, --analyze                 Analyze OneDrive export file
  -s, --scan <scan>             Scan local folder recursively
  -sf, --scan-file <scan-file>  Scan result output file
  --logging <debug|info|trace>  Logging verbosity [default: info]
  --version                     Show version information
  -?, -h, --help                Show help and usage information
```

To export your OneDrive content to CSV:

```powershell
# OneDriveTool export
OneDriveTool --export --file onedrive-export.csv
```

Analyze exported CSV file:

```powershell
# OneDriveTool analyze
OneDriveTool --analyze --file onedrive-export.csv
```

Scan local folder to see if those files are already in OneDrive:

```powershell
# OneDriveTool scan
OneDriveTool --scan D:\\OneDrive --scan-file backup-harddrive1.csv --file onedrive-export.csv
```
