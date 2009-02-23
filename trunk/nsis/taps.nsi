
outFile "tapssetup.exe"

installDir $PROGRAMFILES32\Taps

section
setShellVarContext all
setOutPath $INSTDIR
file ..\tap\tap.exe
file ..\tap\tap.pdb
file ..\COPYING
file ..\COPYING.EXCEPTION
file ..\TODO
file ..\README
writeUninstaller $INSTDIR\uninstall.exe
createDirectory $SMPROGRAMS\Taps
createShortCut "$SMPROGRAMS\Taps\Uninstall Taps.lnk" $INSTDIR\uninstall.exe

setOutPath $INSTDIR\samples\hello
file ..\samples\hello\*.*
setOutPath $INSTDIR\samples\thread
file ..\samples\thread\*.*
setOutPath $INSTDIR\samples\simple
file /r ..\samples\simple\*.cs
file ..\samples\simple\build.bat
file ..\samples\simple\simple.csproj
setOutPath $INSTDIR\doc
file ..\doc\taps.html
file ..\doc\taps.css
createShortCut "$SMPROGRAMS\Taps\Documentation.lnk" $INSTDIR\doc\taps.html

setOutPath $SYSDIR
file ..\tap\tap.exe
file ..\tap\tap.pdb

sectionEnd

section "uninstall"
setShellVarContext all
setOutPath $INSTDIR
delete tap.exe
delete tap.pdb
delete uninstall.exe
rmdir /r $INSTDIR\samples
rmdir /r $INSTDIR\doc
delete "$SMPROGRAMS\Taps\Uninstall Taps.lnk"
delete "$SMPROGRAMS\Taps\Documentation.lnk"
rmdir $SMPROGRAMS\Taps

setOutPath $SYSDIR
delete tap.exe
delete tap.pdb
rmdir /rebootok $INSTDIR
sectionEnd