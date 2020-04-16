#!/bin/bash
ver=$1
if [[ "$ver" == "" ]]; then
    echo -e "\033[1;31mError: version is required.\033[0m"
    exit 1
fi

buildDir=build

rm -dfr $buildDir
mkdir $buildDir

echo -e "\033[1;96m======== Packing macOS version ========\033[0m"
./tools/create-dmg/create-dmg \
    --window-pos 200 120 \
    --window-size 800 400 \
    --app-drop-link 400 200 \
    --add-file readme.txt build.sample/readme.txt 550 32 \
    $buildDir/PLog.Mac.$ver.dmg PLog.Mac/bin/Debug/netcoreapp3.1/PLog.Mac.app

echo -e "\033[1;96m======== Packing Windows version ========\033[0m"
from=PLog.Win/bin/Release/net461
to=$buildDir/PLog.Win.$ver
mkdir $to
cp -pv $from/PLog.Win.exe $to
cp -pv $from/PLog.dll $to
cp -pv $from/FSharp.Core.dll $to
cp -pv $from/Eto.dll $to
cp -pv $from/Eto.WinForms.dll $to
cp -pv $from/FastColoredTextBox.dll $to
cp -pv $from/addr2line.exe $to
cd $buildDir
zip -r PLog.Win.$ver.zip PLog.Win.$ver
rm -dfr PLog.Win.$ver
cd ..
