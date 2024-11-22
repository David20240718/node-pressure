FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["test/TestCryptoBox/TestCryptoBox.csproj", "test/TestCryptoBox/"]
RUN dotnet restore "test/TestCryptoBox/TestCryptoBox.csproj"
COPY . .
WORKDIR "/src/test/TestCryptoBox"
RUN dotnet build "TestCryptoBox.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TestCryptoBox.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TestCryptoBox.dll"]
