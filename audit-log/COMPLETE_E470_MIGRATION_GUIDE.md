# Complete E470.AuditLog Migration Guide

## 📋 Table of Contents
- [Overview](#overview)
- [What's Included](#whats-included)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Manual Migration Steps](#manual-migration-steps)
- [Automated Migration](#automated-migration)
- [Docker Updates](#docker-updates)
- [Verification](#verification)
- [Troubleshooting](#troubleshooting)
- [Rollback](#rollback)

---

## Overview

This guide covers the complete migration of the AuditLog solution to **E470.AuditLog**, including:
- ✅ All 9 projects renamed with E470 prefix
- ✅ All namespaces updated to E470.AuditLog.*
- ✅ All project references updated
- ✅ Docker Compose files updated
- ✅ GitHub workflows updated
- ✅ Solution files updated

### Migration Status

| Project | Old Name | New Name | Status |
|---------|----------|----------|--------|
| **SharedKernel** | SharedKernel | E470.AuditLog.SharedKernel | ✅ Complete |
| **Domain** | Domain | E470.AuditLog.Domain | ✅ Complete |
| **Application** | Application | E470.AuditLog.Application | 🔄 Automated |
| **Infrastructure** | Infrastructure | E470.AuditLog.Infrastructure | 🔄 Automated |
| **EventBusClient** | EventBusClient | E470.AuditLog.EventBusClient | 🔄 Automated |
| **Web.Api** | Web.Api | E470.AuditLog.Web.Api | 🔄 Automated |
| **AppHost** | AudiLog.AppHost | E470.AudiLog.AppHost | ✅ Complete |
| **ServiceDefaults** | AuditLog.ServiceDefaults | E470.AuditLog.ServiceDefaults | ✅ Complete |
| **ArchitectureTests** | ArchitectureTests | E470.AuditLog.ArchitectureTests | 🔄 Automated |

**Legend:**
- ✅ Complete - Already migrated
- 🔄 Automated - Will be migrated by script

---

## What's Included

### 1. Automation Scripts

#### PowerShell (Windows)
- **File**: `complete-e470-migration.ps1`
- **Features**:
  - Dry-run mode for safe testing
  - Verbose logging
  - Error handling
  - Progress reporting

#### Bash (Linux/macOS)
- **File**: `complete-e470-migration.sh`
- **Features**:
  - Same features as PowerShell
  - Cross-platform compatibility
  - POSIX compliant

### 2. Updated Docker Files

#### New Docker Compose Files
- `docker/compose.e470-webapi.yml` - Main API service
- `docker/compose.e470-webapi.override.yml` - Development overrides

#### Key Changes:
```yaml
# Service name updated
e470-auditlog-web-api:
  image: e470-auditlog-webapi
  container_name: e470-auditlog-web-api
  
# Database name updated
ConnectionStrings__audit-log-db=Server=mssql;Database=E470AuditLogDb;...

# Dockerfile path updated
dockerfile: src/E470.AuditLog.Web.Api/Dockerfile
```

### 3. Namespace Updates

All namespaces have been standardized to use the E470.AuditLog prefix:

```csharp
// Old namespaces
namespace SharedKernel;
namespace Domain.Todos;
namespace Application.Todos.Create;
namespace Infrastructure.Database;
namespace Web.Api.Endpoints;

// New namespaces
namespace E470.AuditLog.SharedKernel;
namespace E470.AuditLog.Domain.Todos;
namespace E470.AuditLog.Application.Todos.Create;
namespace E470.AuditLog.Infrastructure.Database;
namespace E470.AuditLog.Web.Api.Endpoints;
```

---

## Prerequisites

### Required
- ✅ Git installed
- ✅ .NET 10 SDK (or .NET 8 SDK)
- ✅ PowerShell 5.1+ (Windows) or Bash (Linux/macOS)
- ✅ Access to the repository

### Optional
- Docker Desktop (for container testing)
- Visual Studio 2022 or Rider
- VS Code with C# Dev Kit

---

## Quick Start

### Option 1: Automated Migration (Recommended)

#### Windows (PowerShell)
```powershell
# Navigate to audit-log directory
cd audit-log

# Test run first (no changes)
.\complete-e470-migration.ps1 -DryRun

# Execute migration
.\complete-e470-migration.ps1

# Verify
dotnet build E470.AuditLog.sln
dotnet test E470.AuditLog.sln
```

#### Linux/macOS (Bash)
```bash
# Navigate to audit-log directory
cd audit-log

# Make script executable
chmod +x complete-e470-migration.sh

# Test run first (no changes)
./complete-e470-migration.sh --dry-run

# Execute migration
./complete-e470-migration.sh

# Verify
dotnet build E470.AuditLog.sln
dotnet test E470.AuditLog.sln
```

### Option 2: Manual Migration

See [Manual Migration Steps](#manual-migration-steps) section below.

---

## Manual Migration Steps

If you prefer to migrate manually or need to understand each step:

### Step 1: Create New Project Directories

```bash
# Create new project folders
mkdir -p src/E470.AuditLog.EventBusClient
mkdir -p src/E470.AuditLog.Application
mkdir -p src/E470.AuditLog.Infrastructure
mkdir -p src/E470.AuditLog.Web.Api
mkdir -p tests/E470.AuditLog.ArchitectureTests
```

### Step 2: Copy Files

```bash
# Copy files to new locations
cp -r src/EventBusClient/* src/E470.AuditLog.EventBusClient/
cp -r src/Application/* src/E470.AuditLog.Application/
cp -r src/Infrastructure/* src/E470.AuditLog.Infrastructure/
cp -r src/Web.Api/* src/E470.AuditLog.Web.Api/
cp -r tests/ArchitectureTests/* tests/E470.AuditLog.ArchitectureTests/
```

### Step 3: Update Namespaces

For each `.cs` file in the new projects:

```bash
# Example for Application project
find src/E470.AuditLog.Application -name "*.cs" -exec sed -i 's/namespace Application/namespace E470.AuditLog.Application/g' {} \;
find src/E470.AuditLog.Application -name "*.cs" -exec sed -i 's/using Application/using E470.AuditLog.Application/g' {} \;
```

Repeat for:
- EventBusClient → E470.AuditLog.EventBusClient
- Infrastructure → E470.AuditLog.Infrastructure
- Web.Api → E470.AuditLog.Web.Api
- Domain → E470.AuditLog.Domain
- SharedKernel → E470.AuditLog.SharedKernel

### Step 4: Update Project References

Update `.csproj` files to reference new project names:

```xml
<!-- Old -->
<ProjectReference Include="..\Domain\Domain.csproj" />

<!-- New -->
<ProjectReference Include="..\E470.AuditLog.Domain\E470.AuditLog.Domain.csproj" />
```

### Step 5: Update Solution File

```bash
# Update E470.AuditLog.sln
# Replace old project paths with new paths
sed -i 's|Domain\\Domain.csproj|E470.AuditLog.Domain\\E470.AuditLog.Domain.csproj|g' E470.AuditLog.sln
# ... repeat for all projects
```

### Step 6: Update Docker Files

Update `Dockerfile` in Web.Api:

```dockerfile
# Old
COPY ["src/Web.Api/Web.Api.csproj", "src/Web.Api/"]

# New
COPY ["src/E470.AuditLog.Web.Api/E470.AuditLog.Web.Api.csproj", "src/E470.AuditLog.Web.Api/"]
```

### Step 7: Update Configuration Files

Update `appsettings.json`, `launchSettings.json`, etc.

---

## Automated Migration

### PowerShell Script Details

**Location**: `complete-e470-migration.ps1`

**Parameters**:
- `-DryRun` - Test run without making changes
- `-Verbose` - Show detailed logging

**What it does**:
1. Creates new project directories
2. Copies all files from old to new locations
3. Updates all namespaces in `.cs` files
4. Updates project references in `.csproj` files
5. Updates solution files
6. Updates Docker files
7. Updates GitHub workflows
8. Generates summary report

**Example Usage**:
```powershell
# Dry run first
.\complete-e470-migration.ps1 -DryRun -Verbose

# Execute
.\complete-e470-migration.ps1

# With verbose logging
.\complete-e470-migration.ps1 -Verbose
```

### Bash Script Details

**Location**: `complete-e470-migration.sh`

**Parameters**:
- `--dry-run` - Test run without making changes
- `--verbose` - Show detailed logging

**What it does**:
- Same as PowerShell script
- Cross-platform compatible
- Uses sed for text replacement

**Example Usage**:
```bash
# Make executable
chmod +x complete-e470-migration.sh

# Dry run
./complete-e470-migration.sh --dry-run --verbose

# Execute
./complete-e470-migration.sh
```

---

## Docker Updates

### New Docker Compose Configuration

#### File: `docker/compose.e470-webapi.yml`

```yaml
services:
  e470-auditlog-web-api:
    image: ${DOCKER_REGISTRY-}e470-auditlog-webapi
    container_name: e470-auditlog-web-api
    build:
      context: .
      dockerfile: src/E470.AuditLog.Web.Api/Dockerfile
    ports:
      - 5000:8080
      - 5001:8081
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__audit-log-db=Server=mssql;Database=E470AuditLogDb;User Id=sa;Password=Pass@word1;TrustServerCertificate=True;
    depends_on:
      - mssql
  mssql:
    extends:
      file: compose.mssql.yml
      service: mssql
```

### Running with Docker

```bash
# Navigate to docker directory
cd audit-log/docker

# Build and run
docker-compose -f compose.e470-webapi.yml up --build

# Run in background
docker-compose -f compose.e470-webapi.yml up -d

# Stop services
docker-compose -f compose.e470-webapi.yml down

# View logs
docker-compose -f compose.e470-webapi.yml logs -f
```

### Updated Dockerfile

The Dockerfile has been updated with new project paths:

```dockerfile
# Key changes
COPY ["src/E470.AuditLog.Web.Api/E470.AuditLog.Web.Api.csproj", "src/E470.AuditLog.Web.Api/"]
COPY ["src/E470.AuditLog.ServiceDefaults/E470.AuditLog.ServiceDefaults.csproj", "src/E470.AuditLog.ServiceDefaults/"]
COPY ["src/E470.AuditLog.Infrastructure/E470.AuditLog.Infrastructure.csproj", "src/E470.AuditLog.Infrastructure/"]
# ... etc

WORKDIR "/src/src/E470.AuditLog.Web.Api"
RUN dotnet build "./E470.AuditLog.Web.Api.csproj"

ENTRYPOINT ["dotnet", "E470.AuditLog.Web.Api.dll"]
```

---

## Verification

### 1. Build Solution

```bash
# Clean build
dotnet clean E470.AuditLog.sln
dotnet build E470.AuditLog.sln --configuration Release

# Expected output: Build succeeded. 0 Warning(s). 0 Error(s).
```

### 2. Run Tests

```bash
# Run all tests
dotnet test E470.AuditLog.sln --configuration Release

# Run with coverage
dotnet test E470.AuditLog.sln /p:CollectCoverage=true
```

### 3. Run Application

#### Option A: Using .NET Aspire (Recommended)

```bash
cd audit-log
dotnet run --project src/E470.AudiLog.AppHost/E470.AuditLog.AppHost.csproj
```

Then open: http://localhost:15000 (Aspire Dashboard)

#### Option B: Using Web.Api directly

```bash
cd audit-log/src/E470.AuditLog.Web.Api
dotnet run

# Open Swagger UI
# https://localhost:5001/swagger
```

#### Option C: Using Docker

```bash
cd audit-log/docker
docker-compose -f compose.e470-webapi.yml up
```

### 4. Verify Endpoints

```bash
# Health check
curl http://localhost:5000/health

# API endpoints (requires auth)
curl http://localhost:5000/api/todos
curl http://localhost:5000/api/users
```

### 5. Check Database

```bash
# Connect to SQL Server
docker exec -it mssql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Pass@word1"

# Verify database
SELECT name FROM sys.databases WHERE name = 'E470AuditLogDb';
GO
```

---

## Troubleshooting

### Common Issues

#### Issue 1: Build Errors - "Project not found"

**Symptoms**:
```
error MSB3202: The project file "...\Domain.csproj" was not found.
```

**Solution**:
```bash
# Verify project references
dotnet list E470.AuditLog.sln reference

# Clean and rebuild
dotnet clean E470.AuditLog.sln
dotnet restore E470.AuditLog.sln
dotnet build E470.AuditLog.sln
```

#### Issue 2: Namespace Errors

**Symptoms**:
```
error CS0246: The type or namespace name 'SharedKernel' could not be found
```

**Solution**:
```bash
# Verify namespaces in files
grep -r "namespace SharedKernel" src/

# Should show no results if migration is complete
# All should be "namespace E470.AuditLog.SharedKernel"
```

#### Issue 3: Docker Build Fails

**Symptoms**:
```
ERROR [build 4/6] COPY ["src/Web.Api/Web.Api.csproj"...
COPY failed: file not found
```

**Solution**:
```bash
# Update Dockerfile paths
# Verify the context is set to audit-log directory
docker build -t e470-audit-log -f src/E470.AuditLog.Web.Api/Dockerfile .
```

#### Issue 4: Migration Script Permission Denied (Linux/macOS)

**Symptoms**:
```bash
bash: ./complete-e470-migration.sh: Permission denied
```

**Solution**:
```bash
chmod +x complete-e470-migration.sh
./complete-e470-migration.sh
```

#### Issue 5: Old Projects Still Referenced

**Symptoms**:
Build warnings or errors about old project paths

**Solution**:
```bash
# Search for old references
grep -r "\\Domain\\Domain.csproj" .
grep -r "\\Application\\Application.csproj" .

# Update manually or re-run migration script
```

---

## Rollback

If you need to rollback the migration:

### Option 1: Git Reset

```bash
# If not committed
git reset --hard HEAD

# If committed but not pushed
git reset --hard HEAD~1

# If pushed (create revert commit)
git revert HEAD
```

### Option 2: Manual Rollback

```bash
# Delete new folders
rm -rf src/E470.AuditLog.*
rm -rf tests/E470.AuditLog.*

# Restore old solution file
git checkout HEAD -- AuditLog.sln

# Restore old project files
git checkout HEAD -- src/Application
git checkout HEAD -- src/Infrastructure
# ... etc
```

### Option 3: Use Branch

```bash
# Create backup branch first
git branch backup/before-e470-migration

# If rollback needed
git checkout backup/before-e470-migration
git branch -D feature/rename-to-e470-auditlog
```

---

## Post-Migration Checklist

- [ ] All projects build successfully
- [ ] All tests pass
- [ ] Application runs via Aspire AppHost
- [ ] Application runs via Web.Api directly
- [ ] Docker containers build and run
- [ ] Swagger UI accessible
- [ ] Database migrations work
- [ ] Health checks pass
- [ ] API endpoints respond correctly
- [ ] CI/CD pipeline updated
- [ ] Documentation updated
- [ ] Team notified

---

## Additional Resources

### Documentation Files
- `QUICK_START_E470.md` - Quick start guide
- `MIGRATION_PLAN_E470.md` - Detailed migration plan
- `PROJECT_STRUCTURE_COMPARISON.md` - Before/after comparison
- `PR1_CHANGES_SUMMARY.md` - Complete change log

### Migration Scripts
- `complete-e470-migration.ps1` - PowerShell automation
- `complete-e470-migration.sh` - Bash automation
- `migrate-to-e470.ps1` - Original migration script
- `migrate-to-e470.sh` - Original bash script

### Docker Files
- `docker/compose.e470-webapi.yml` - Main compose file
- `docker/compose.e470-webapi.override.yml` - Dev overrides
- `docker/compose.mssql.yml` - SQL Server (unchanged)

---

## Support

### Need Help?

1. **Check Documentation**: Review all MD files in the repository
2. **Run Dry-Run**: Test migration without making changes
3. **Check Logs**: Review migration script output
4. **Verify Prerequisites**: Ensure all tools are installed
5. **Contact Team**: Reach out to the development team

### Reporting Issues

When reporting issues, include:
- Operating system and version
- .NET SDK version (`dotnet --version`)
- Error messages (full stack trace)
- Steps to reproduce
- Migration script output (if applicable)

---

## Success!

Once migration is complete:

```bash
# Commit changes
git add .
git commit -m "Complete E470.AuditLog migration - all projects renamed and updated"

# Push to remote
git push origin feature/rename-to-e470-auditlog

# Create/update pull request
# Merge when approved
```

**Congratulations!** Your solution has been successfully migrated to E470.AuditLog! 🎉

---

**Last Updated**: 2025
**Version**: 1.0.0
**Status**: Complete Migration Guide
