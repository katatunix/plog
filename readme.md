PLog &ndash; a tool for viewing Android log and more
===

## Setup
* Visual Studio (VS) 2017/2019 with F# support (Community / Professional / Enterprise).
* .NET Core SDK 3.x.
* Targeting pack for `.NET Framework 4.6.1`.

## Build
* Open `PLog.sln` with your Visual Studio.
* To build Windows version:
    * Should be built on Windows to have correct app icons.
    * Build project `PLog.Win`.    
    * Or you can just run `build.win.bat`.
* To build macOS version:
    * Must be built on macOS.
    * Build project `PLog.Mac`.
    * Or you can just run `build.mac.sh`.

## Pack
* Make sure the two projects above are built in `Release` mode.
* Run `./pack.sh [version]` to copy all output files/folders to the `build` folder. For example: `./pack.sh 8.5`.
* See the artifacts in the `build` folder.
