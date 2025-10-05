PLog &ndash; a tool for viewing Android log and more
===

## Setup
* MacOS machine.
* .NET Core SDK 6.0.
* JetBrains Rider is recommended.

## Build
### Mac OS
* Run `./fake.sh build`.
* See output: `PLog.Mac/bin/Release/net6.0/PLog.Mac.app`.

### Windows
* Run `dotnet publish .\PLog.Win\PLog.Win.fsproj -c Release -f net8.0-windows -r win-x64 -p:SatelliteResourceLanguages=en`.
* See output `PLog.Win\bin\Release\net8.0-windows\win-x64\publish`.

## Pack
* Run `./fake.sh pack [version]`. For example: `./fake.sh pack 8.5`.
* See the `.dmg` files in the `build` folder.

