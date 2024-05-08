# OneDrive tool
<!-- 
![CI](https://github.com/JanneMattila/onedrive-cleaner/workflows/CI/badge.svg?branch=master)
-->

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)


## Introduction

You might end up into situation that you have filled your
personal OneDrive with many duplicate files. This can
quite easily happen in you take a lot of pictures with
your different cameras and then upload then over time
to different folders. This can eat up your quota quite
easily. This tool tries to make it easier to find duplicates
and then provide means to clean them up.

## Usage

![Work-in-progress](https://img.shields.io/badge/warning-work%20in%20progress-red)

Usage instructions:

```cmd
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

```cmd
# OneDriveTool export
```

Analyze exported CSV file:

```cmd
# OneDriveTool analyze
```
