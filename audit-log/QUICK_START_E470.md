# 🚀 Quick Start - E470.AuditLog Migration

## For the Impatient Developer

This is a **TL;DR** guide to complete the E470.AuditLog renaming. For full details, see [MIGRATION_PLAN_E470.md](MIGRATION_PLAN_E470.md).

---

## ✅ What's Already Done

- ✅ Solution renamed to `E470.AuditLog.sln`
- ✅ Aspire projects renamed (AppHost, ServiceDefaults)
- ✅ SharedKernel fully migrated with E470 namespace
- ✅ Migration scripts ready to complete the rest

---

## ⚡ Quick Migration (3 minutes)

### Windows

```powershell
# Navigate to project
cd audit-log

# OPTIONAL: Preview changes first (dry run)
.\migrate-to-e470.ps1 -DryRun

# Execute migration
.\migrate-to-e470.ps1

# Verify it worked
dotnet build E470.AuditLog.sln
dotnet test E470.AuditLog.sln

# Commit
git add .
git commit -m "Complete E470 prefix migration using automated script"
git push origin feature/rename-to-e470-auditlog
```

### Linux / macOS

```bash
# Navigate to project
cd audit-log

# Make script executable
chmod +x migrate-to-e470.sh

# OPTIONAL: Preview changes first (dry run)
./migrate-to-e470.sh --dry-run

# Execute migration
./migrate-to-e470.sh

# Verify it worked
dotnet build E470.AuditLog.sln
dotnet test E470.AuditLog.sln

# Commit
git add .
git commit -m "Complete E470 prefix migration using automated script"
git push origin feature/rename-to-e470-auditlog
```

---

## 🤔 What the Script Does

The migration script will:

1. **Copy** all existing projects to new folders with E470 prefix
2. **Update** all namespaces from `Domain` → `E470.AuditLog.Domain`, etc.
3. **Update** all project references in .csproj files
4. **Update** Docker configurations
5. **Update** Dockerfile paths
6. **Leave** old folders intact (you can delete them after verification)

### Projects Migrated by Script:
- ✅ E470.AuditLog.Domain
- ✅ E470.AuditLog.Application
- ✅ E470.AuditLog.EventBusClient
- ✅ E470.AuditLog.Infrastructure
- ✅ E470.AuditLog.Web.Api
- ✅ E470.AuditLog.ArchitectureTests

---

## 📝 After Running the Script

### 1. Update Solution File

You need to manually update `E470.AuditLog.sln` to reference the new projects:

**Option A: Use IDE**
- Open `E470.AuditLog.sln` in Visual Studio or Rider
- Right-click solution → "Add Existing Project"
- Add all `E470.AuditLog.*` projects
- Remove old project references
- Save solution

**Option B: Use dotnet CLI**
```bash
# Remove old projects
dotnet sln E470.AuditLog.sln remove src/Domain/Domain.csproj
dotnet sln E470.AuditLog.sln remove src/Application/Application.csproj
dotnet sln E470.AuditLog.sln remove src/EventBusClient/EventBusClient.csproj
dotnet sln E470.AuditLog.sln remove src/Infrastructure/Infrastructure.csproj
dotnet sln E470.AuditLog.sln remove src/Web.Api/Web.Api.csproj
dotnet sln E470.AuditLog.sln remove tests/ArchitectureTests/ArchitectureTests.csproj

# Add new projects
dotnet sln E470.AuditLog.sln add src/E470.AuditLog.Domain/E470.AuditLog.Domain.csproj
dotnet sln E470.AuditLog.sln add src/E470.AuditLog.Application/E470.AuditLog.Application.csproj
dotnet sln E470.AuditLog.sln add src/E470.AuditLog.EventBusClient/E470.AuditLog.EventBusClient.csproj
dotnet sln E470.AuditLog.sln add src/E470.AuditLog.Infrastructure/E470.AuditLog.Infrastructure.csproj
dotnet sln E470.AuditLog.sln add src/E470.AuditLog.Web.Api/E470.AuditLog.Web.Api.csproj
dotnet sln E470.AuditLog.sln add tests/E470.AuditLog.ArchitectureTests/E470.AuditLog.ArchitectureTests.csproj
```

### 2. Build & Test

```bash
# Clean everything
dotnet clean E470.AuditLog.sln
rm -rf **/bin **/obj

# Restore packages
dotnet restore E470.AuditLog.sln

# Build
dotnet build E470.AuditLog.sln --configuration Release

# Run tests
dotnet test E470.AuditLog.sln --configuration Release

# Run Aspire AppHost
dotnet run --project src/E470.AudiLog.AppHost/E470.AuditLog.AppHost.csproj
```

### 3. Delete Old Folders (After Verification)

Once everything builds and tests pass:

```bash
# Backup first (just in case)
git commit -am "Checkpoint before deleting old folders"

# Delete old project folders
rm -rf src/Domain
rm -rf src/Application
rm -rf src/EventBusClient
rm -rf src/Infrastructure
rm -rf src/Web.Api
rm -rf src/SharedKernel
rm -rf src/AudiLog.AppHost
rm -rf src/AuditLog.ServiceDefaults
rm -rf tests/ArchitectureTests

# Remove old solution files (optional, if you want to clean up)
rm AuditLog.sln
rm AuditLog.slnx

# Commit deletion
git add -A
git commit -m "Remove old project folders after E470 migration"
git push origin feature/rename-to-e470-auditlog
```

---

## 🐛 Troubleshooting

### "Script not found" or "Permission denied"

**Linux/macOS:**
```bash
chmod +x migrate-to-e470.sh
./migrate-to-e470.sh
```

**Windows PowerShell:**
```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\migrate-to-e470.ps1
```

### Build errors after migration

```bash
# Nuclear option - clean everything
dotnet clean E470.AuditLog.sln
rm -rf **/bin
rm -rf **/obj
rm -rf ~/.nuget/packages/e470.auditlog.*

# Restore fresh
dotnet restore E470.AuditLog.sln

# Rebuild
dotnet build E470.AuditLog.sln
```

### Old folders already exist

```bash
# Script will skip existing folders
# Either delete them manually first, or rename them:
mv src/E470.AuditLog.Domain src/E470.AuditLog.Domain.old
# Then run script again
```

### Namespace errors after migration

The script updates all namespaces, but if you see errors:

1. Check the file manually
2. Look for patterns like `using Domain;` that should be `using E470.AuditLog.Domain;`
3. Use IDE "Find and Replace" with regex:
   - Find: `^using (Domain|Application|Infrastructure|SharedKernel)`
   - Replace: `using E470.AuditLog.$1`

---

## 🎯 Verification Checklist

After migration, check these:

- [ ] `dotnet build E470.AuditLog.sln` succeeds
- [ ] `dotnet test E470.AuditLog.sln` passes all tests
- [ ] Aspire AppHost runs: `dotnet run --project src/E470.AudiLog.AppHost/...`
- [ ] API responds: `curl http://localhost:5000/health`
- [ ] Swagger loads: `http://localhost:5000/swagger`
- [ ] No errors in Visual Studio Error List
- [ ] Solution Explorer shows all E470 projects
- [ ] Git status shows expected changes

---

## 📚 Additional Resources

- **Full Migration Plan**: [MIGRATION_PLAN_E470.md](MIGRATION_PLAN_E470.md)
- **All Changes**: [PR1_CHANGES_SUMMARY.md](PR1_CHANGES_SUMMARY.md)
- **Original Rename Summary**: [RENAME_SUMMARY.md](RENAME_SUMMARY.md)

---

## 💡 Tips

### Prefer IDE Refactoring?

Instead of using scripts, you can:

1. Open solution in Visual Studio or Rider
2. Right-click each project → Rename
3. Use IDE's refactoring to update namespaces
4. Manually update Docker and config files

**Pros**: More control, IDE handles references
**Cons**: More time-consuming, more manual steps

### Want to Verify Before Committing?

```bash
# Run with dry run first
.\migrate-to-e470.ps1 -DryRun  # Windows
./migrate-to-e470.sh --dry-run  # Linux/macOS

# Review what would change
# Then run without dry run
```

### Working with a Team?

1. **Before migration**: Ensure all work is committed
2. **During migration**: Work on a feature branch
3. **After migration**: 
   - Merge PR
   - Notify team
   - Everyone should delete local branch and pull fresh from main

---

## ⏱️ Time Estimates

| Method | Time | Difficulty |
|--------|------|------------|
| **Automated Script** | 3-5 minutes | ⭐ Easy |
| **IDE Refactoring** | 30-60 minutes | ⭐⭐ Medium |
| **Manual Migration** | 5-7 hours | ⭐⭐⭐⭐⭐ Expert |

**Recommendation**: Use the automated script! 🚀

---

## 🎉 Success!

If you:
- ✅ Ran the script
- ✅ Updated the solution file
- ✅ Built successfully
- ✅ Tests pass

**You're done!** 🎊

Commit your changes and open a PR (or update the existing PR #1).

---

## ❓ Questions?

- Check [MIGRATION_PLAN_E470.md](MIGRATION_PLAN_E470.md) for detailed answers
- Review script output for specific errors
- Check commit history in PR #1 for examples

---

**Last Updated**: Today
**Estimated Completion Time**: 3-5 minutes with script
**Difficulty**: ⭐ Easy with automation
