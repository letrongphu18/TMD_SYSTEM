FROM mcr.microsoft.com/dotnet/sdk:9.0-preview AS build
WORKDIR /src

COPY TMD/TMD/TMD.csproj ./TMD.csproj
RUN dotnet restore "./TMD.csproj"

COPY TMD/. ./TMD/
WORKDIR /src/TMD

RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0-preview AS final
WORKDIR /app

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "TMD.dll"]
