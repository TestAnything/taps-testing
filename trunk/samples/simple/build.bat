@echo off
call "%VS90COMNTOOLS%/vsvars32.bat" > NUL
msbuild /p:Configuration=Debug
