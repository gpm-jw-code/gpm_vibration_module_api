cd /d "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin"
msbuild -t:Restore "%~dp0\gpm_moudle_api_solution.sln"
msbuild "%~dp0\gpm_moudle_api_solution.sln"
pause