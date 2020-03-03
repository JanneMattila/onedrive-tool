# OneDrive Cleaner

![CI](https://github.com/JanneMattila/onedrive-cleaner/workflows/CI/badge.svg?branch=master)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Edit in [Visual Studio Online](https://online.visualstudio.com/environments/new?name=quizmaker&repo=JanneMattila/onedrive-cleaner).

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
# onedrive-cleaner
  ___             ____       _
 / _ \ _ __   ___|  _ \ _ __(_)_   _____
| | | | '_ \ / _ \ | | | '__| \ \ / / _ \
| |_| | | | |  __/ |_| | |  | |\ V /  __/
\___ /|_| |_|\___|____/|_|  |_| \_/ \___|
  ____ _
 / ___| | ___  __ _ _ __   ___ _ __
| |   | |/ _ \/ _` | '_ \ / _ \ '__|
| |___| |  __/ (_| | | | |  __/ |
 \____|_|\___|\__,_|_| |_|\___|_|


Usage:
        export          Exports to CSV file
        analyze         Analyzes the CSV file
```

To export your OneDrive content to CSV:

```cmd
# onedrive-cleaner export
```

Analyze exported CSV file:

```cmd
# onedrive-cleaner analyze
```
