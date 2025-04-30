dotnet publish -c Release -r win-x64 --self-contained true -o ./out/Minecraft-Easy-Servers-win-x64
dotnet publish -c Release -r linux-x64 --self-contained true -o ./out/Minecraft-Easy-Servers-linux-x64
dotnet publish -c Release -r linux-arm64 --self-contained true -o ./out/Minecraft-Easy-Servers-linux-arm64