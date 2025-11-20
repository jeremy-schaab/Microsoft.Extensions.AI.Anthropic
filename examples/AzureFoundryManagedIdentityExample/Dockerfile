# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["examples/AzureFoundryManagedIdentityExample/AzureFoundryManagedIdentityExample.csproj", "examples/AzureFoundryManagedIdentityExample/"]
COPY ["src/Microsoft.Extensions.AI.Anthropic/Microsoft.Extensions.AI.Anthropic.csproj", "src/Microsoft.Extensions.AI.Anthropic/"]

# Restore dependencies
RUN dotnet restore "examples/AzureFoundryManagedIdentityExample/AzureFoundryManagedIdentityExample.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/examples/AzureFoundryManagedIdentityExample"
RUN dotnet build "AzureFoundryManagedIdentityExample.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "AzureFoundryManagedIdentityExample.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published app
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "AzureFoundryManagedIdentityExample.dll"]
