buildDir=build
ver=$1

rm -dfr $buildDir
mkdir $buildDir

./tools/create-dmg/create-dmg \
    --window-pos 200 120 \
    --window-size 800 400 \
    --app-drop-link 550 185 \
    $buildDir/PLog.Mac.$ver.dmg PLog.Mac/bin/Release/net461/PLog.Mac.app

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
