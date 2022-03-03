FROM ghcr.io/symend-deployments/dotnetasp:5.0 AS base
WORKDIR /app

FROM ghcr.io/symend-deployments/dotnetsdk:5.0 AS build
WORKDIR /src
COPY ["EfStress.csproj", "."]
RUN dotnet restore "EfStress.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "EfStress.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EfStress.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EfStress.dll"]
