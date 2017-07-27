rem Sample usage: d:\temp\RazzleRemover\ProjectTransformer>transform d:\dd\vs\src\Platform c:\users\olegtk\Source\Repos\VS-Platform\src

@echo Transforming original csproj files...
bin\Debug\ProjectTransformer.exe %1

@echo Copying over to target repo...
robocopy %1 %2 /S /IF *.newcsproj

@echo replacing original csproj files in target repo...
for /r "%2" %%x in (*.csproj) do del "%%x"
for /r "%2" %%x in (*.newcsproj) do ren "%%x" *.csproj