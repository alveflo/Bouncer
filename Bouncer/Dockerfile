FROM mcr.microsoft.com/dotnet/core/runtime:3.1 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY ["Bouncer/Bouncer.fsproj", "Bouncer/"]
RUN dotnet restore "Bouncer/Bouncer.fsproj"
COPY . .
WORKDIR "/src/Bouncer"
RUN dotnet build "Bouncer.fsproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Bouncer.fsproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Bouncer.dll"]
