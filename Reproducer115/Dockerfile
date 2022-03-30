FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Reproducer114/Reproducer114.csproj", "Reproducer114/"]
RUN dotnet restore "Reproducer114/Reproducer114.csproj"
COPY . .
WORKDIR "/src/Reproducer114"
RUN dotnet build "Reproducer114.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Reproducer114.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Reproducer114.dll"]
