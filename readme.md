PLog - a tool for viewing Android log and more
===

## Setup
* Visual Studio (VS) 2017/2019 with F# support (Community / Professional / Enterprise).
* If you have to use VS 2013, please stop and jump to the section `VS 2013` below.
* Open `PLog.sln` with VS and wait for all `nuget` packages being downloaded.
* Run `copy_libs.sh` to overwrite some packages.

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

---

## VS 2013
Using VS 2013 is possible but for Windows version only.

#### Setup
* Your VS 2013 edition must support F# (Community / Professional / Enterprise).
* You need to update some components of your VS 2013:
    * Open VS 2013.
    * Tools / Extensions and Updates / Updates tab / Visual Studio Gallery
    * Update the two components related to `nuget` and `F#`.

#### Build
* Go to the `VS2013` folder.
* Open solution `PLog.sln`.
* Build (not run) the solution for the first time to download all the required `nuget` packages.
* Verify that a folder named `packages` is created at the same level with `PLog.sln`.
* Now close, re-open, and re-build the solution.
* Ready!!!

#### Release
* Make sure the solution above is built in `Release` mode.
* Go to the `VS2013` folder.
* Run `release.bat` to copy all output files/folders to the `build` folder.
* See the artifacts in the `build` folder.

---

## Contact
* Email: nghia.buivan@hotmail.com
* Skype: live:katatunix
