# E470.AuditLog - Comprehensive Rename Migration Plan

## Overview
This document outlines the complete migration plan to rename all projects and namespaces in the solution to include the **E470.AuditLog** prefix.

## Migration Status

### ✅ Phase 1: Aspire Projects (COMPLETED)
- [x] E470.AudiLog.AppHost
- [x] E470.AuditLog.ServiceDefaults

### ✅ Phase 2: SharedKernel Project (COMPLETED)
- [x] Project renamed to **E470.AuditLog.SharedKernel**
- [x] All namespaces updated to `E470.AuditLog.SharedKernel`
- [x] Files migrated:
  - Entity.cs
  - Error.cs
  - ErrorType.cs
  - IDateTimeProvider.cs
  - IDomainEvent.cs
  - IDomainEventHandler.cs
  - Result.cs
  - ValidationError.cs

### 🔄 Phase 3: Domain Project (IN PROGRESS)
**Target**: Rename to **E470.AuditLog.Domain**

**Namespace Changes**:
- `Domain.Todos` → `E470.AuditLog.Domain.Todos`
- `Domain.Users` → `E470.AuditLog.Domain.Users`

**Files to Migrate**:
```
src/E470.AuditLog.Domain/
├── E470.AuditLog.Domain.csproj
├── Todos/
│   ├── Priority.cs
│   ├── TodoItem.cs
│   ├── TodoItemCompletedDomainEvent.cs
│   ├── TodoItemCreatedDomainEvent.cs
│   ├── TodoItemDeletedDomainEvent.cs
│   └── TodoItemErrors.cs
└── Users/
    ├── User.cs
    ├── UserErrors.cs
    └── UserRegisteredDomainEvent.cs
```

**Project References to Update**:
```xml
<ProjectReference Include="..\E470.AuditLog.SharedKernel\E470.AuditLog.SharedKernel.csproj" />
```

### 🔄 Phase 4: Application Project (PENDING)
**Target**: Rename to **E470.AuditLog.Application**

**Namespace Changes**:
- `Application.Abstractions` → `E470.AuditLog.Application.Abstractions`
- `Application.Todos` → `E470.AuditLog.Application.Todos`
- `Application.Users` → `E470.AuditLog.Application.Users`

**Project References to Update**:
```xml
<ProjectReference Include="..\E470.AuditLog.Domain\E470.AuditLog.Domain.csproj" />
<ProjectReference Include="..\E470.AuditLog.EventBusClient\E470.AuditLog.EventBusClient.csproj" />
<ProjectReference Include="..\E470.AuditLog.SharedKernel\E470.AuditLog.SharedKernel.csproj" />
```

**InternalsVisibleTo Update**:
```xml
<InternalsVisibleTo Include="E470.AuditLog.Application.UnitTests" />
```

### 🔄 Phase 5: EventBusClient Project (PENDING)
**Target**: Rename to **E470.AuditLog.EventBusClient**

**Namespace Changes**:
- `EventBusClient` → `E470.AuditLog.EventBusClient`

### 🔄 Phase 6: Infrastructure Project (PENDING)
**Target**: Rename to **E470.AuditLog.Infrastructure**

**Namespace Changes**:
- `Infrastructure.Authentication` → `E470.AuditLog.Infrastructure.Authentication`
- `Infrastructure.Authorization` → `E470.AuditLog.Infrastructure.Authorization`
- `Infrastructure.Database` → `E470.AuditLog.Infrastructure.Database`
- `Infrastructure.DomainEvents` → `E470.AuditLog.Infrastructure.DomainEvents`
- `Infrastructure.Time` → `E470.AuditLog.Infrastructure.Time`
- `Infrastructure.Todos` → `E470.AuditLog.Infrastructure.Todos`
- `Infrastructure.Users` → `E470.AuditLog.Infrastructure.Users`

**Project References to Update**:
```xml
<ProjectReference Include="..\E470.AuditLog.Application\E470.AuditLog.Application.csproj" />
```

**InternalsVisibleTo Update**:
```xml
<InternalsVisibleTo Include="E470.AuditLog.ArchitectureTests" />
```

**EF Core Migrations**:
- Update namespace in migration files
- Update `ApplicationDbContextModelSnapshot.cs`

### 🔄 Phase 7: Web.Api Project (PENDING)
**Target**: Rename to **E470.AuditLog.Web.Api**

**Namespace Changes**:
- `Web.Api` → `E470.AuditLog.Web.Api`
- `Web.Api.Endpoints` → `E470.AuditLog.Web.Api.Endpoints`
- `Web.Api.Extensions` → `E470.AuditLog.Web.Api.Extensions`
- `Web.Api.Infrastructure` → `E470.AuditLog.Web.Api.Infrastructure`
- `Web.Api.Middleware` → `E470.AuditLog.Web.Api.Middleware`

**Project References to Update**:
```xml
<ProjectReference Include="..\E470.AuditLog.ServiceDefaults\E470.AuditLog.ServiceDefaults.csproj" />
<ProjectReference Include="..\E470.AuditLog.Infrastructure\E470.AuditLog.Infrastructure.csproj" />
```

**Program.cs Updates**:
```csharp
namespace E470.AuditLog.Web.Api
{
    public partial class Program;
}
```

### 🔄 Phase 8: ArchitectureTests Project (PENDING)
**Target**: Rename to **E470.AuditLog.ArchitectureTests**

**Namespace Changes**:
- `ArchitectureTests` → `E470.AuditLog.ArchitectureTests`
- `ArchitectureTests.Layers` → `E470.AuditLog.ArchitectureTests.Layers`

**Test Updates**:
- Update assembly references in tests
- Update namespace validation rules

---

## Configuration File Updates

### Docker Compose Files
**Files to Update**:
- `docker/compose.webapi.yml`
- `docker/compose.webapi.override.yml`
- `docker/compose.mssql.yml`

**Changes**:
```yaml
# Update project paths
build:
  context: ..
  dockerfile: src/E470.AuditLog.Web.Api/Dockerfile

# Update image names
image: e470auditlog-webapi:latest
container_name: e470auditlog-webapi
```

### Dockerfile Updates
**File**: `src/E470.AuditLog.Web.Api/Dockerfile`

**Changes**:
```dockerfile
# Update all project references
COPY ["src/E470.AuditLog.Web.Api/E470.AuditLog.Web.Api.csproj", "src/E470.AuditLog.Web.Api/"]
COPY ["src/E470.AuditLog.Infrastructure/E470.AuditLog.Infrastructure.csproj", "src/E470.AuditLog.Infrastructure/"]
COPY ["src/E470.AuditLog.Application/E470.AuditLog.Application.csproj", "src/E470.AuditLog.Application/"]
COPY ["src/E470.AuditLog.Domain/E470.AuditLog.Domain.csproj", "src/E470.AuditLog.Domain/"]
COPY ["src/E470.AuditLog.EventBusClient/E470.AuditLog.EventBusClient.csproj", "src/E470.AuditLog.EventBusClient/"]
COPY ["src/E470.AuditLog.SharedKernel/E470.AuditLog.SharedKernel.csproj", "src/E470.AuditLog.SharedKernel/"]
COPY ["src/E470.AuditLog.ServiceDefaults/E470.AuditLog.ServiceDefaults.csproj", "src/E470.AuditLog.ServiceDefaults/"]

# Update dotnet restore
RUN dotnet restore "src/E470.AuditLog.Web.Api/E470.AuditLog.Web.Api.csproj"

# Update dotnet build
RUN dotnet build "src/E470.AuditLog.Web.Api/E470.AuditLog.Web.Api.csproj" -c Release -o /app/build

# Update dotnet publish
RUN dotnet publish "src/E470.AuditLog.Web.Api/E470.AuditLog.Web.Api.csproj" -c Release -o /app/publish

# Update ENTRYPOINT
ENTRYPOINT ["dotnet", "E470.AuditLog.Web.Api.dll"]
```

### Aspire Configuration
**File**: `.aspire/settings.json`

**Changes**:
```json
{
  "AppHost": {
    "Path": "src/E470.AudiLog.AppHost/E470.AuditLog.AppHost.csproj"
  }
}
```

### GitHub Actions Workflow
**File**: `.github/workflows/build.yml`

**Changes**:
```yaml
# Update solution file reference
- name: Restore dependencies
  run: dotnet restore E470.AuditLog.sln

- name: Build
  run: dotnet build E470.AuditLog.sln --no-restore --configuration Release

- name: Test
  run: dotnet test E470.AuditLog.sln --no-build --configuration Release --verbosity normal
```

### Solution Files
**Files**:
- `E470.AuditLog.sln` (already updated)
- `E470.AuditLog.slnx` (already updated)

**Project Entries to Update**:
```
Project("{9A19103F-BBDC-4300-AFC3-4DDB0B1F04BC}") = "E470.AuditLog.SharedKernel", "src\E470.AuditLog.SharedKernel\E470.AuditLog.SharedKernel.csproj", "{GUID}"
Project("{9A19103F-BBDC-4300-AFC3-4DDB0B1F04BC}") = "E470.AuditLog.Domain", "src\E470.AuditLog.Domain\E470.AuditLog.Domain.csproj", "{GUID}"
Project("{9A19103F-BBDC-4300-AFC3-4DDB0B1F04BC}") = "E470.AuditLog.Application", "src\E470.AuditLog.Application\E470.AuditLog.Application.csproj", "{GUID}"
Project("{9A19103F-BBDC-4300-AFC3-4DDB0B1F04BC}") = "E470.AuditLog.Infrastructure", "src\E470.AuditLog.Infrastructure\E470.AuditLog.Infrastructure.csproj", "{GUID}"
Project("{9A19103F-BBDC-4300-AFC3-4DDB0B1F04BC}") = "E470.AuditLog.Web.Api", "src\E470.AuditLog.Web.Api\E470.AuditLog.Web.Api.csproj", "{GUID}"
Project("{9A19103F-BBDC-4300-AFC3-4DDB0B1F04BC}") = "E470.AuditLog.EventBusClient", "src\E470.AuditLog.EventBusClient\E470.AuditLog.EventBusClient.csproj", "{GUID}"
Project("{9A19103F-BBDC-4300-AFC3-4DDB0B1F04BC}") = "E470.AuditLog.AppHost", "src\E470.AudiLog.AppHost\E470.AuditLog.AppHost.csproj", "{GUID}"
Project("{9A19103F-BBDC-4300-AFC3-4DDB0B1F04BC}") = "E470.AuditLog.ServiceDefaults", "src\E470.AuditLog.ServiceDefaults\E470.AuditLog.ServiceDefaults.csproj", "{GUID}"
Project("{9A19103F-BBDC-4300-AFC3-4DDB0B1F04BC}") = "E470.AuditLog.ArchitectureTests", "tests\E470.AuditLog.ArchitectureTests\E470.AuditLog.ArchitectureTests.csproj", "{GUID}"
```

---

## Implementation Strategy

### Approach
Due to the large number of files, we'll use a **phased approach**:

1. **Create New Project Structure** - Create all new folders and .csproj files
2. **Migrate Core Files** - Copy and update namespaces in batches
3. **Update Solution** - Update solution files to reference new projects
4. **Update Configurations** - Update Docker, CI/CD, and other configs
5. **Delete Old Projects** - Remove old project folders
6. **Verification** - Build and test the solution

### Automated Tools Recommendation
For a production migration, consider using:
- **ReSharper** - Automated namespace and project renaming
- **Rider** - Refactoring tools
- **PowerShell Scripts** - Batch file updates
- **dotnet-rename** - CLI tool for project renaming

### Manual Steps Required
Some steps require careful manual review:
- EF Core migration history
- Database connection strings
- Environment-specific configurations
- User secrets
- Team member IDE configurations

---

## Namespace Mapping Reference

| Old Namespace | New Namespace |
|--------------|---------------|
| `SharedKernel` | `E470.AuditLog.SharedKernel` |
| `Domain` | `E470.AuditLog.Domain` |
| `Domain.Todos` | `E470.AuditLog.Domain.Todos` |
| `Domain.Users` | `E470.AuditLog.Domain.Users` |
| `Application` | `E470.AuditLog.Application` |
| `Application.Abstractions.*` | `E470.AuditLog.Application.Abstractions.*` |
| `Application.Todos.*` | `E470.AuditLog.Application.Todos.*` |
| `Application.Users.*` | `E470.AuditLog.Application.Users.*` |
| `EventBusClient` | `E470.AuditLog.EventBusClient` |
| `Infrastructure` | `E470.AuditLog.Infrastructure` |
| `Infrastructure.*` | `E470.AuditLog.Infrastructure.*` |
| `Web.Api` | `E470.AuditLog.Web.Api` |
| `Web.Api.*` | `E470.AuditLog.Web.Api.*` |
| `ArchitectureTests` | `E470.AuditLog.ArchitectureTests` |
| `ArchitectureTests.Layers` | `E470.AuditLog.ArchitectureTests.Layers` |

---

## Verification Checklist

After migration, verify:

- [ ] Solution builds without errors
- [ ] All tests pass
- [ ] Docker image builds successfully
- [ ] Aspire AppHost runs correctly
- [ ] API endpoints respond correctly
- [ ] Database migrations work
- [ ] EF Core context initializes
- [ ] Authentication works
- [ ] Authorization policies work
- [ ] Domain events dispatch correctly
- [ ] Health checks pass
- [ ] Swagger UI loads
- [ ] CI/CD pipeline runs successfully

---

## Rollback Plan

If issues arise:
1. Revert the branch: `git checkout main`
2. Delete the feature branch: `git branch -D feature/rename-to-e470-auditlog`
3. Restore from backup if files were modified locally
4. Review migration logs
5. Address issues and retry

---

## Timeline Estimate

| Phase | Estimated Time | Complexity |
|-------|----------------|------------|
| Phase 1: Aspire Projects | ✅ Complete | Low |
| Phase 2: SharedKernel | ✅ Complete | Low |
| Phase 3: Domain | 30 minutes | Medium |
| Phase 4: Application | 1-2 hours | High |
| Phase 5: EventBusClient | 15 minutes | Low |
| Phase 6: Infrastructure | 1-2 hours | High |
| Phase 7: Web.Api | 1 hour | Medium |
| Phase 8: ArchitectureTests | 30 minutes | Medium |
| Configuration Updates | 30 minutes | Medium |
| Testing & Verification | 1 hour | High |
| **Total** | **5-7 hours** | **High** |

---

## Next Steps

### Option 1: Continue Automated Migration (Recommended for Small Batches)
Continue creating files in phases, committing regularly.

### Option 2: Use IDE Refactoring Tools (Recommended for Large Scale)
1. Pull the current branch locally
2. Use Visual Studio or Rider's "Rename" refactoring
3. Commit all changes in a single batch
4. Push to the branch

### Option 3: PowerShell Script Migration
Create a PowerShell script to:
1. Copy all files to new folders
2. Update namespaces using regex
3. Update all references
4. Generate updated solution file

---

## Contact & Support

For questions or issues during migration:
- Review this document
- Check commit history in the feature branch
- Refer to `RENAME_SUMMARY.md` for completed changes

---

**Document Version**: 1.0
**Last Updated**: Migration in progress
**Status**: Phase 2 Complete, Phase 3+ Pending
