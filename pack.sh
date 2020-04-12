buildDir=build
ver=$1

rm -dfr $buildDir
mkdir $buildDir

./tools/create-dmg/create-dmg \
    --window-pos 200 120 \
    --window-size 800 400 \
    --app-drop-link 550 185 \
    $buildDir/PLog.Mac.$ver.dmg PLog.Mac/bin/Release/PLog.Mac.app

from=PLog.Win/bin/Release
to=$buildDir/PLog.Win.$ver
mkdir $to
cp -pv $from/*.dll $to
cp -pv $from/*.exe $to
cp -pvR $from/tools $to
cd $buildDir
zip -r PLog.Win.$ver.zip PLog.Win.$ver
rm -dfr PLog.Win.$ver
cd ..
