FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia solo los .csproj para cachear el restore.
COPY Inventario/Inventario.API/Inventario.API.csproj Inventario/Inventario.API/
COPY Inventario/Inventario.Application/Inventario.Application.csproj Inventario/Inventario.Application/
COPY Inventario/Inventario.Core/Inventario.Core.csproj Inventario/Inventario.Core/
COPY Inventario/Inventario.Infrastructure/Inventario.Infrastructure.csproj Inventario/Inventario.Infrastructure/
COPY DailyTasks/DailyTasks.API/DailyTasks.API.csproj DailyTasks/DailyTasks.API/
COPY DailyTasks/DailyTasks.Core/DailyTasks.Core.csproj DailyTasks/DailyTasks.Core/
COPY DailyTasks/DailyTasks.Application/DailyTasks.Application.csproj DailyTasks/DailyTasks.Application/
COPY DailyTasks/DailyTasks.Infrastructure/DailyTasks.Infrastructure.csproj DailyTasks/DailyTasks.Infrastructure/

RUN dotnet restore Inventario/Inventario.API/Inventario.API.csproj

COPY . .
RUN dotnet publish Inventario/Inventario.API/Inventario.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# .NET 8 incluye un usuario sin privilegios para ejecutar la app.
USER app

EXPOSE 8080
CMD ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet Inventario.API.dll"]
