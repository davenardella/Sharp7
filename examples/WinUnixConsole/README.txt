=======================
Microsoft Visual Studio
=======================

Compilation via batch file:

dotnet

or from command line:

csc Client.cs ..\..\src\Sharp7.cs

in both cases you must use the command prompt that you find in 
Microsoft Visual Studio XX.0\Common7\Tools\Shortcuts\


================================================================================
Mono (Windows/Linux and other platform supported and compliant with BDS Sockets)
================================================================================

Compilation via script file:

./mono.sh

or from command line:

mcs Client.cs ../../src/Sharp7.cs  /out:Client.exe

example of use :
  
mono Client.exe 192.168.0.65 0 2

