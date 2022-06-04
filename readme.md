PLog &ndash; a tool for viewing Android log and more
===

## Setup
* MacOS machine.
* .NET Core SDK 6.0.
* JetBrains Rider is recommended.

## Build
* Run `./fake.sh build`.
* See output: `PLog.Mac/bin/Release/net6.0/PLog.Mac.app`.

## Pack
* Run `./fake.sh pack [version]`. For example: `./fake.sh pack 8.5`.
* See the `.dmg` files in the `build` folder.

## TODO
* Write build script for Windows build. Sorry I don't have any Windows PC at the moment :)
