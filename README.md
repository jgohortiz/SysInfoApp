## Para compilar
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

## Ruta archivo a publicar
bin\Release\net9.0-windows\win-x64\publish
