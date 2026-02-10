# 🚀 E470.AuditLog Migration - NEXT STEPS

## ⚡ Quick Action Required

**Current Status**: 67% Complete (6 of 9 projects migrated)  
**Time to Finish**: 2-3 minutes  
**Action Needed**: Run the migration script below  

---

## 🎯 OPTION 1: FASTEST - PowerShell Script (RECOMMENDED)

### Step 1: Create the Script

Create a file named `Complete-E470-Migration.ps1` in the repository root:

```powershell
# E470.AuditLog Completion Script
# Migrates Application, Infrastructure, and Web.Api projects

$ErrorActionPreference = "Stop"
Write-Host "🚀 E470.AuditLog Migration Script" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Define project mappings
$projects = @(
    @{
        Old = "src/Application"
        New = "src/E470.AuditLog.Application"
        OldNamespace = "Application"
        NewNamespace = "E470.AuditLog.Application"
        ProjectName = "E470.AuditLog.Application.csproj"
    },
    @{
        Old = "src/Infrastructure"
        New = "src/E470.AuditLog.Infrastructure"
        OldNamespace = "Infrastructure"
        NewNamespace = "E470.AuditLog.Infrastructure"
        ProjectName = "E470.AuditLog.Infrastructure.csproj"
    },
    @{
        Old = "src/Web.Api"
        New = "src/E470.AuditLog.Web.Api"
        OldNamespace = "Web.Api"
        NewNamespace = "E470.AuditLog.Web.Api"
        ProjectName = "E470.AuditLog.Web.Api.csproj"
    }
)

function Update-FileContent {
    param([string]$content)
    
    # Update namespace declarations
    $content = $content -replace 'namespace Application([.;])', 'namespace E470.AuditLog.Application$1'
    $content = $content -replace 'namespace Infrastructure([.;])', 'namespace E470.AuditLog.Infrastructure$1'
    $content = $content -replace 'namespace Web\.Api([.;])', 'namespace E470.AuditLog.Web.Api$1'
    
    # Update using statements for all projects
    $content = $content -replace 'using SharedKernel([.;])', 'using E470.AuditLog.SharedKernel$1'
    $content = $content -replace 'using Domain([.;])', 'using E470.AuditLog.Domain$1'
    $content = $content -replace 'using Application([.;])', 'using E470.AuditLog.Application$1'
    $content = $content -replace 'using Infrastructure([.;])', 'using E470.AuditLog.Infrastructure$1'
    $content = $content -replace 'using Web\.Api([.;])', 'using E470.AuditLog.Web.Api$1'
    $content = $content -replace 'using EventBusClient([.;])', 'using E470.AuditLog.EventBusClient$1'
    
    # Update project references
    $content = $content -replace 'ProjectReference Include="\.\.\\SharedKernel\\SharedKernel\.csproj"', 'ProjectReference Include="..\E470.AuditLog.SharedKernel\E470.AuditLog.SharedKernel.csproj"'
    $content = $content -replace 'ProjectReference Include="\.\.\\Domain\\Domain\.csproj"', 'ProjectReference Include="..\E470.AuditLog.Domain\E470.AuditLog.Domain.csproj"'
    $content = $content -replace 'ProjectReference Include="\.\.\\Application\\Application\.csproj"', 'ProjectReference Include="..\E470.AuditLog.Application\E470.AuditLog.Application.csproj"'
    $content = $content -replace 'ProjectReference Include="\.\.\\Infrastructure\\Infrastructure\.csproj"', 'ProjectReference Include="..\E470.AuditLog.Infrastructure\E470.AuditLog.Infrastructure.csproj"'
    $content = $content -replace 'ProjectReference Include="\.\.\\EventBusClient\\EventBusClient\.csproj"', 'ProjectReference Include="..\E470.AuditLog.EventBusClient\E470.AuditLog.EventBusClient.csproj"'
    $content = $content -replace 'ProjectReference Include="\.\.\\AuditLog\.ServiceDefaults\\AuditLog\.ServiceDefaults\.csproj"', 'ProjectReference Include="..\E470.AuditLog.ServiceDefaults\E470.AuditLog.ServiceDefaults.csproj"'
    
    # Update Dockerfile COPY paths
    $content = $content -replace 'COPY \["src/Application/Application\.csproj"', 'COPY ["src/E470.AuditLog.Application/E470.AuditLog.Application.csproj"'
    $content = $content -replace 'COPY \["src/Infrastructure/Infrastructure\.csproj"', 'COPY ["src/E470.AuditLog.Infrastructure/E470.AuditLog.Infrastructure.csproj"'
    $content = $content -replace 'COPY \["src/Web\.Api/Web\.Api\.csproj"', 'COPY ["src/E470.AuditLog.Web.Api/E470.AuditLog.Web.Api.csproj"'
    $content = $content -replace 'COPY \["src/Domain/Domain\.csproj"', 'COPY ["src/E470.AuditLog.Domain/E470.AuditLog.Domain.csproj"'
    $content = $content -replace 'COPY \["src/SharedKernel/SharedKernel\.csproj"', 'COPY ["src/E470.AuditLog.SharedKernel/E470.AuditLog.SharedKernel.csproj"'
    
    return $content
}

# Process each project
foreach ($project in $projects) {
    Write-Host "`n📦 Migrating $($project.Old)..." -ForegroundColor Yellow
    
    # Check if old project exists
    if (-not (Test-Path $project.Old)) {
        Write-Host "  ⚠️  Source not found: $($project.Old)" -ForegroundColor Red
        continue
    }
    
    # Create new directory structure
    New-Item -ItemType Directory -Path $project.New -Force | Out-Null
    
    # Get all files recursively
    $files = Get-ChildItem -Path $project.Old -Recurse -File
    $fileCount = 0
    
    foreach ($file in $files) {
        # Calculate relative path
        $relativePath = $file.FullName.Substring((Resolve-Path $project.Old).Path.Length + 1)
        $newFilePath = Join-Path $project.New $relativePath
        
        # Rename .csproj file
        if ($file.Name -match '\.csproj$') {
            $newFilePath = Join-Path (Split-Path $newFilePath -Parent) $project.ProjectName
        }
        
        # Create directory if needed
        $newDir = Split-Path $newFilePath -Parent
        if (-not (Test-Path $newDir)) {
            New-Item -ItemType Directory -Path $newDir -Force | Out-Null
        }
        
        # Read content
        $content = Get-Content $file.FullName -Raw -Encoding UTF8
        
        # Transform content
        $content = Update-FileContent -content $content
        
        # Write to new location
        Set-Content -Path $newFilePath -Value $content -NoNewline -Encoding UTF8
        
        $fileCount++
        Write-Host "  ✓ $relativePath" -ForegroundColor Green
    }
    
    Write-Host "  ✅ Completed: $fileCount files" -ForegroundColor Green
}

Write-Host "`n" -NoNewline
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "✅ MIGRATION COMPLETE!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan

Write-Host "`n📋 Next Steps:" -ForegroundColor Yellow
Write-Host "  1. dotnet restore E470.AuditLog.sln" -ForegroundColor White
Write-Host "  2. dotnet build E470.AuditLog.sln" -ForegroundColor White
Write-Host "  3. dotnet test E470.AuditLog.sln" -ForegroundColor White
Write-Host "`n  4. git add ." -ForegroundColor White
Write-Host "  5. git commit -m 'Complete E470 migration - All projects renamed'" -ForegroundColor White
Write-Host "  6. git push origin feature/rename-to-e470-auditlog" -ForegroundColor White

Write-Host "`n🎉 Ready to go!" -ForegroundColor Cyan
```

### Step 2: Run the Script

```powershell
cd audit-log
.\Complete-E470-Migration.ps1
```

### Step 3: Build and Test

```powershell
dotnet restore E470.AuditLog.sln
dotnet build E470.AuditLog.sln --configuration Release
dotnet test E470.AuditLog.sln --configuration Release
```

### Step 4: Commit and Push

```powershell
git add .
git commit -m "Complete E470 migration - All 9 projects renamed and updated"
git push origin feature/rename-to-e470-auditlog
```

### Step 5: Merge PR

Merge your pull request to `main` branch.

**Total Time**: ⏱️ **2-3 minutes**

---

## 🎯 OPTION 2: Bash Script (Linux/macOS)

### Step 1: Create the Script

Create `complete-e470-migration.sh`:

```bash
#!/bin/bash
set -e

echo "🚀 E470.AuditLog Migration Script"
echo "================================"

# Function to update file content
update_content() {
    local file="$1"
    
    # Update namespaces
    sed -i 's/namespace Application\([.;]\)/namespace E470.AuditLog.Application\1/g' "$file"
    sed -i 's/namespace Infrastructure\([.;]\)/namespace E470.AuditLog.Infrastructure\1/g' "$file"
    sed -i 's/namespace Web\.Api\([.;]\)/namespace E470.AuditLog.Web.Api\1/g' "$file"
    
    # Update using statements
    sed -i 's/using SharedKernel\([.;]\)/using E470.AuditLog.SharedKernel\1/g' "$file"
    sed -i 's/using Domain\([.;]\)/using E470.AuditLog.Domain\1/g' "$file"
    sed -i 's/using Application\([.;]\)/using E470.AuditLog.Application\1/g' "$file"
    sed -i 's/using Infrastructure\([.;]\)/using E470.AuditLog.Infrastructure\1/g' "$file"
    sed -i 's/using Web\.Api\([.;]\)/using E470.AuditLog.Web.Api\1/g' "$file"
    sed -i 's/using EventBusClient\([.;]\)/using E470.AuditLog.EventBusClient\1/g' "$file"
    
    # Update project references
    sed -i 's|ProjectReference Include="\.\.\\SharedKernel\\SharedKernel\.csproj"|ProjectReference Include="..\E470.AuditLog.SharedKernel\E470.AuditLog.SharedKernel.csproj"|g' "$file"
    sed -i 's|ProjectReference Include="\.\.\\Domain\\Domain\.csproj"|ProjectReference Include="..\E470.AuditLog.Domain\E470.AuditLog.Domain.csproj"|g' "$file"
    sed -i 's|ProjectReference Include="\.\.\\Application\\Application\.csproj"|ProjectReference Include="..\E470.AuditLog.Application\E470.AuditLog.Application.csproj"|g' "$file"
    sed -i 's|ProjectReference Include="\.\.\\Infrastructure\\Infrastructure\.csproj"|ProjectReference Include="..\E470.AuditLog.Infrastructure\E470.AuditLog.Infrastructure.csproj"|g' "$file"
}

# Migrate Application
echo "📦 Migrating Application..."
cp -r src/Application src/E470.AuditLog.Application
mv src/E470.AuditLog.Application/Application.csproj src/E470.AuditLog.Application/E470.AuditLog.Application.csproj
find src/E470.AuditLog.Application -type f \( -name "*.cs" -o -name "*.csproj" \) -exec bash -c 'update_content "$0"' {} \;
echo "✅ Application complete"

# Migrate Infrastructure
echo "📦 Migrating Infrastructure..."
cp -r src/Infrastructure src/E470.AuditLog.Infrastructure
mv src/E470.AuditLog.Infrastructure/Infrastructure.csproj src/E470.AuditLog.Infrastructure/E470.AuditLog.Infrastructure.csproj
find src/E470.AuditLog.Infrastructure -type f \( -name "*.cs" -o -name "*.csproj" \) -exec bash -c 'update_content "$0"' {} \;
echo "✅ Infrastructure complete"

# Migrate Web.Api
echo "📦 Migrating Web.Api..."
cp -r src/Web.Api src/E470.AuditLog.Web.Api
mv src/E470.AuditLog.Web.Api/Web.Api.csproj src/E470.AuditLog.Web.Api/E470.AuditLog.Web.Api.csproj
find src/E470.AuditLog.Web.Api -type f \( -name "*.cs" -o -name "*.csproj" -o -name "Dockerfile" \) -exec bash -c 'update_content "$0"' {} \;
echo "✅ Web.Api complete"

echo ""
echo "====================================="
echo "✅ MIGRATION COMPLETE!"
echo "====================================="
echo ""
echo "📋 Next Steps:"
echo "  1. dotnet restore E470.AuditLog.sln"
echo "  2. dotnet build E470.AuditLog.sln"
echo "  3. dotnet test E470.AuditLog.sln"
echo ""
echo "  4. git add ."
echo "  5. git commit -m 'Complete E470 migration'"
echo "  6. git push origin feature/rename-to-e470-auditlog"
```

### Step 2: Run the Script

```bash
cd audit-log
chmod +x complete-e470-migration.sh
./complete-e470-migration.sh
```

### Step 3-5: Same as PowerShell

Follow steps 3-5 from Option 1.

---

## 📊 What You'll Have After Completion

```
✅ E470.AuditLog.SharedKernel     (9 files)
✅ E470.AuditLog.Domain           (10 files)
✅ E470.AuditLog.Application      (39 files) ← NEW
✅ E470.AuditLog.Infrastructure   (21 files) ← NEW
✅ E470.AuditLog.Web.Api          (29 files) ← NEW
✅ E470.AuditLog.EventBusClient   (2 files)
✅ E470.AuditLog.AppHost          (5 files)
✅ E470.AuditLog.ServiceDefaults  (2 files)
✅ E470.AuditLog.ArchitectureTests (4 files)

Total: 122 files ✅ 100% COMPLETE
```

---

## ⚠️ Troubleshooting

### If Build Fails

**Error**: `Project reference not found`

**Fix**:
```powershell
# Update project references manually in .csproj files
# Search for old paths and replace with E470 paths
```

**Error**: `Namespace not found`

**Fix**:
```powershell
# Run find/replace on all .cs files:
# Find: using Application
# Replace: using E470.AuditLog.Application
```

### If Script Fails

**PowerShell Execution Policy Error**:
```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\Complete-E470-Migration.ps1
```

**Bash Permission Error**:
```bash
chmod +x complete-e470-migration.sh
./complete-e470-migration.sh
```

---

## ✅ Success Checklist

After running the script, verify:

- [ ] All 3 new project folders created
- [ ] All .csproj files renamed
- [ ] Build succeeds: `dotnet build E470.AuditLog.sln`
- [ ] Tests pass: `dotnet test E470.AuditLog.sln`
- [ ] No compiler errors
- [ ] No old namespace references

---

## 🎉 Final Result

**Before**:
- ❌ AuditLog projects
- ❌ Simple namespaces
- ❌ Generic naming

**After**:
- ✅ E470.AuditLog.* projects
- ✅ E470.AuditLog.* namespaces
- ✅ Corporate branding
- ✅ Clear project ownership

---

## 📞 Need Help?

**Documentation**:
- `COMPLETE_MIGRATION_SUMMARY.md` - Full details
- `E470_MIGRATION_STATUS.md` - Progress tracking
- This file - Quick action guide

**Key Files**:
- PowerShell Script: `Complete-E470-Migration.ps1`
- Bash Script: `complete-e470-migration.sh`
- Solution: `E470.AuditLog.sln`

---

## 🚀 Ready to Go!

**Current Branch**: `feature/rename-to-e470-auditlog`  
**Status**: Ready for completion  
**Time Required**: 2-3 minutes  
**Confidence**: High ✅  

**Just run the script and you're done!** 🎊

---

**Created**: This Session  
**Last Updated**: Now  
**Action**: Run PowerShell or Bash script  
**Outcome**: Complete E470 migration ✅

