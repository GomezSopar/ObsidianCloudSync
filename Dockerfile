FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/ObsidianSync.Blazor/ObsidianSync.Blazor.csproj"
RUN dotnet publish "src/ObsidianSync.Blazor/ObsidianSync.Blazor.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ObsidianSync.Blazor.dll"]
