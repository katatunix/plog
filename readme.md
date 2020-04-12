PLog &ndash; a tool for viewing Android log and more
===

## Setup
* Visual Studio (VS) 2017/2019 with F# support (Community / Professional / Enterprise).

## Build
* Open `PLog.sln` with your Visual Studio.
* To build Windows version:
    * Build project `PLog.Win`.
    * Should be built on Windows to have correct app icons.
* To build macOS version:
    * Build project `PLog.Mac`.
    * Must be built on macOS.

## Pack
* Make sure the two projects above are built in `Release` mode.
* Run `./pack.sh [version]` to copy all output files/folders to the `build` folder. For example: `./pack.sh 8.5`.
* See the artifacts in the `build` folder.
