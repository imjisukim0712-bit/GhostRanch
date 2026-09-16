# Delivery rule

After every implementation build for this project, publish the current Windows build as a self-contained, single `.exe` and provide its absolute path to the user. Use:

`dotnet publish GhostWidget/GhostWidget.csproj -c Release -o GhostWidget/dist`
