FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/SistemaLegalPagares/SistemaLegalPagares.csproj src/SistemaLegalPagares/
RUN dotnet restore src/SistemaLegalPagares/SistemaLegalPagares.csproj

COPY src/SistemaLegalPagares/ src/SistemaLegalPagares/
RUN dotnet publish src/SistemaLegalPagares/SistemaLegalPagares.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SistemaLegalPagares.dll"]
