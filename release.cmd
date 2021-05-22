::Zips the dll into the correct directory structure for release
::Make sure to increment the version

set kk_version=0.1
set kk_name=KK_TestPlugin
set hs2_name=HS2_TestPlugin
set ai_name=AI_TestPlugin

IF EXIST "./bin/%kk_name%/BepinEx/plugins/%kk_name%.dll" "%ProgramFiles%\7-Zip\7z.exe" a -tzip "%HOMEPATH%/downloads/%kk_name% v%kk_version%.zip" "./bin/%kk_name%/BepinEx" -mx0
IF EXIST "./bin/%hs2_name%/BepinEx/plugins/%hs2_name%.dll" "%ProgramFiles%\7-Zip\7z.exe" a -tzip "%HOMEPATH%/downloads/%hs2_name% v%kk_version%.zip" "./bin/%hs2_name%/BepinEx" -mx0
IF EXIST "./bin/%ai_name%/BepinEx/plugins/%ai_name%.dll" "%ProgramFiles%\7-Zip\7z.exe" a -tzip "%HOMEPATH%/downloads/%ai_name% v%kk_version%.zip" "./bin/%ai_name%/BepinEx" -mx0

start %HOMEPATH%/downloads