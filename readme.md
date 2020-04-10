PLog - a tool for viewing Android log and more
===

## Setup
* Visual Studio (VS) 2017/2019 with F# support (Community / Professional / Enterprise).

## Build
* Windows version:
    * Project `PLog.Win`
    * Should be built on Windows to have correct app icons.
* macOS version:
    * Project `PLog.Mac`
    * Must be built on macOS.

## Release
* Make sure the two projects above are built in `Release` mode.
* Run `release.sh` to copy all output files/folders to the `build` folder.
* See the artifacts in the `build` folder.
