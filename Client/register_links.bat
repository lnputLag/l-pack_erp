@echo off
rem регистрирует протокол доступа к программе по ссылкам

set regfile=links.reg
set path=%cd%

echo Register path: %path%\l-pack_erp.exe

echo Windows Registry Editor Version 5.00 > %regfile%
echo [HKEY_CLASSES_ROOT\l-pack] >> %regfile%
echo @="URL:l-pack" >> %regfile%
echo "URL Protocol"="" >> %regfile%
echo [HKEY_CLASSES_ROOT\l-pack\DefaultIcon] >> %regfile%
echo @="%path%\\l-pack_erp.exe,0" >> %regfile%
echo [HKEY_CLASSES_ROOT\l-pack\shell] >> %regfile%
echo [HKEY_CLASSES_ROOT\l-pack\shell\open] >> %regfile%
echo [HKEY_CLASSES_ROOT\l-pack\shell\open\command] >> %regfile%
echo @="%path%\\l-pack_erp.exe -url %%1" >> %regfile%

start %regfile%

echo Complete
pause