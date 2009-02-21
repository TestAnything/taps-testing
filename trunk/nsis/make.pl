sub ret { exit $?>>8 }

system(qq{"$ENV{ProgramFiles}/NSIS/makensis.exe" taps.nsi}) and ret;

