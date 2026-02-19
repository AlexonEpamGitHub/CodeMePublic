FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5200

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["Web.Api.Publisher.csproj", "."]
COPY ["../Shared.Messages/Shared.Messages.csproj", "../Shared.Messages/"]

# Restore dependencies
RUN dotnet restore "Web.Api.Publisher.csproj"

# Copy all source files
COPY . .

# Build
RUN dotnet build "Web.Api.Publisher.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Web.Api.Publisher.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Web.Api.Publisher.dll"]
