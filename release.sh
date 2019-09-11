rm -R build/*

cp -pvR PLog.Mac/bin/Release/PLog.Mac.app build

from=PLog.Win/bin/Release
to=build/PLog.Win
mkdir $to
cp -pv $from/*.dll $to
cp -pv $from/*.exe $to
cp -pvR $from/tools $to
