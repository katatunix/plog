set FROM_DIR=PLog.Win\bin\Release
set TO_DIR=build\PLog.Win
rd /s /q %TO_DIR%
md %TO_DIR%

copy %FROM_DIR%\*.dll %TO_DIR%
copy %FROM_DIR%\*.exe %TO_DIR%

xcopy /E ..\PLog.Win\tools %TO_DIR%\tools\
pause
