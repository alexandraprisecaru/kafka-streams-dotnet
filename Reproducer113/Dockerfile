FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Reproducer113/Reproducer113.csproj", "Reproducer113/"]
RUN dotnet restore "Reproducer113/Reproducer113.csproj"
COPY . .
WORKDIR "/src/Reproducer113"
RUN dotnet build "Reproducer113.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Reproducer113.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Reproducer113.dll"]
