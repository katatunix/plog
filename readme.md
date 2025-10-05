PLog &ndash; a tool for viewing Android log and more
===

## Setup
* MacOS or Windows machine.
* .NET Core SDK 9.0.
* JetBrains Rider is recommended.

## Build
### Mac OS
* Run `./fake.sh build`.
* See output: `PLog.Mac/bin/Release/net9.0/osx-x64` and `PLog.Mac/bin/Release/net9.0/osx-arm64`.

### Windows
* Run `dotnet publish .\PLog.Win\PLog.Win.fsproj -c Release -r win-x64 -p:SatelliteResourceLanguages=en`.
* See output `PLog.Win\bin\Release\net9.0-windows7.0\win-x64\publish`.

## Pack
* Run `./fake.sh pack [version]`. For example: `./fake.sh pack 8.5`.
* See the `.dmg` files in the `build` folder.
