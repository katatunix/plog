rm -R build/*

ver=$1

./tools/create-dmg/create-dmg \
    --window-pos 200 120 \
    --window-size 800 400 \
    --app-drop-link 550 185 \
    build/PLog.Mac.$ver.dmg PLog.Mac/bin/Release/PLog.Mac.app

from=PLog.Win/bin/Release
to=build/PLog.Win.$ver
mkdir $to
cp -pv $from/*.dll $to
cp -pv $from/*.exe $to
cp -pvR $from/tools $to
