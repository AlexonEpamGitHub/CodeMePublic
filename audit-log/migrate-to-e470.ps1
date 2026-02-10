# E470.AuditLog Migration Script
# This script automates the migration of all projects and namespaces to include E470.AuditLog prefix

param(
    [switch]$DryRun = $false,
    [switch]$Verbose = $false
)

$ErrorActionPreference = "Stop"

# Configuration
$rootPath = $PSScriptRoot
$srcPath = Join-Path $rootPath "src"
$testsPath = Join-Path $rootPath "tests"

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "E470.AuditLog Migration Script" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

if ($DryRun) {
    Write-Host "[DRY RUN MODE] No changes will be made" -ForegroundColor Yellow
    Write-Host ""
}

# Namespace mappings
$namespaceMappings = @{
    "^using SharedKernel" = "using E470.AuditLog.SharedKernel"
    "^namespace SharedKernel" = "namespace E470.AuditLog.SharedKernel"
    "^using Domain" = "using E470.AuditLog.Domain"
    "^namespace Domain" = "namespace E470.AuditLog.Domain"
    "^using Application" = "using E470.AuditLog.Application"
    "^namespace Application" = "namespace E470.AuditLog.Application"
    "^using EventBusClient" = "using E470.AuditLog.EventBusClient"
    "^namespace EventBusClient" = "namespace E470.AuditLog.EventBusClient"
    "^using Infrastructure" = "using E470.AuditLog.Infrastructure"
    "^namespace Infrastructure" = "namespace E470.AuditLog.Infrastructure"
    "^using Web\.Api" = "using E470.AuditLog.Web.Api"
    "^namespace Web\.Api" = "namespace E470.AuditLog.Web.Api"
    "^using ArchitectureTests" = "using E470.AuditLog.ArchitectureTests"
    "^namespace ArchitectureTests" = "namespace E470.AuditLog.ArchitectureTests"
}

# Project reference mappings
$projectReferenceMappings = @{
    '\.\.\\SharedKernel\\SharedKernel\.csproj' = '..\E470.AuditLog.SharedKernel\E470.AuditLog.SharedKernel.csproj'
    '\.\.\\Domain\\Domain\.csproj' = '..\E470.AuditLog.Domain\E470.AuditLog.Domain.csproj'
    '\.\.\\Application\\Application\.csproj' = '..\E470.AuditLog.Application\E470.AuditLog.Application.csproj'
    '\.\.\\EventBusClient\\EventBusClient\.csproj' = '..\E470.AuditLog.EventBusClient\E470.AuditLog.EventBusClient.csproj'
    '\.\.\\Infrastructure\\Infrastructure\.csproj' = '..\E470.AuditLog.Infrastructure\E470.AuditLog.Infrastructure.csproj'
    '\.\.\\AuditLog\.ServiceDefaults\\AuditLog\.ServiceDefaults\.csproj' = '..\E470.AuditLog.ServiceDefaults\E470.AuditLog.ServiceDefaults.csproj'
}

# InternalsVisibleTo mappings
$internalsVisibleToMappings = @{
    '"Application\.UnitTests"' = '"E470.AuditLog.Application.UnitTests"'
    '"ArchitectureTests"' = '"E470.AuditLog.ArchitectureTests"'
}

function Update-FileContent {
    param(
        [string]$FilePath,
        [hashtable]$Mappings,
        [string]$Description
    )
    
    if (-not (Test-Path $FilePath)) {
        return $false
    }
    
    $content = Get-Content $FilePath -Raw
    $originalContent = $content
    $changed = $false
    
    foreach ($pattern in $Mappings.Keys) {
        $replacement = $Mappings[$pattern]
        if ($content -match $pattern) {
            $content = $content -replace $pattern, $replacement
            $changed = $true
            
            if ($Verbose) {
                Write-Host "  Updated: $pattern -> $replacement" -ForegroundColor Gray
            }
        }
    }
    
    if ($changed) {
        Write-Host "  ✓ $Description`: " -NoNewline -ForegroundColor Green
        Write-Host (Split-Path $FilePath -Leaf) -ForegroundColor White
        
        if (-not $DryRun) {
            Set-Content -Path $FilePath -Value $content -NoNewline
        }
    }
    
    return $changed
}

function Copy-ProjectWithRename {
    param(
        [string]$OldPath,
        [string]$NewPath,
        [string]$ProjectName
    )
    
    Write-Host "`nMigrating: $ProjectName" -ForegroundColor Cyan
    Write-Host "From: $OldPath" -ForegroundColor Gray
    Write-Host "To:   $NewPath" -ForegroundColor Gray
    
    if (-not (Test-Path $OldPath)) {
        Write-Host "  ⚠ Source path not found, skipping" -ForegroundColor Yellow
        return
    }
    
    if (Test-Path $NewPath) {
        Write-Host "  ⚠ Target already exists, skipping" -ForegroundColor Yellow
        return
    }
    
    if (-not $DryRun) {
        # Create target directory
        New-Item -ItemType Directory -Path $NewPath -Force | Out-Null
        
        # Copy all files
        Get-ChildItem -Path $OldPath -Recurse -File | ForEach-Object {
            $relativePath = $_.FullName.Substring($OldPath.Length + 1)
            $targetPath = Join-Path $NewPath $relativePath
            $targetDir = Split-Path $targetPath -Parent
            
            if (-not (Test-Path $targetDir)) {
                New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
            }
            
            Copy-Item -Path $_.FullName -Destination $targetPath -Force
        }
    }
    
    Write-Host "  ✓ Project copied" -ForegroundColor Green
    
    # Update all .cs files
    $csFiles = Get-ChildItem -Path $(if ($DryRun) { $OldPath } else { $NewPath }) -Filter "*.cs" -Recurse
    $updatedCount = 0
    
    foreach ($file in $csFiles) {
        if (Update-FileContent -FilePath $file.FullName -Mappings $namespaceMappings -Description "Namespace") {
            $updatedCount++
        }
    }
    
    # Update .csproj files
    $csprojFiles = Get-ChildItem -Path $(if ($DryRun) { $OldPath } else { $NewPath }) -Filter "*.csproj" -Recurse
    
    foreach ($file in $csprojFiles) {
        Update-FileContent -FilePath $file.FullName -Mappings $projectReferenceMappings -Description "Project Reference" | Out-Null
        Update-FileContent -FilePath $file.FullName -Mappings $internalsVisibleToMappings -Description "InternalsVisibleTo" | Out-Null
    }
    
    Write-Host "  ✓ Updated $updatedCount C# files" -ForegroundColor Green
}

# ============================================
# PHASE 3: Domain Project
# ============================================
Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "PHASE 3: Domain Project Migration" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

Copy-ProjectWithRename `
    -OldPath (Join-Path $srcPath "Domain") `
    -NewPath (Join-Path $srcPath "E470.AuditLog.Domain") `
    -ProjectName "E470.AuditLog.Domain"

# ============================================
# PHASE 4: Application Project
# ============================================
Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "PHASE 4: Application Project Migration" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

Copy-ProjectWithRename `
    -OldPath (Join-Path $srcPath "Application") `
    -NewPath (Join-Path $srcPath "E470.AuditLog.Application") `
    -ProjectName "E470.AuditLog.Application"

# ============================================
# PHASE 5: EventBusClient Project
# ============================================
Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "PHASE 5: EventBusClient Project Migration" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

Copy-ProjectWithRename `
    -OldPath (Join-Path $srcPath "EventBusClient") `
    -NewPath (Join-Path $srcPath "E470.AuditLog.EventBusClient") `
    -ProjectName "E470.AuditLog.EventBusClient"

# ============================================
# PHASE 6: Infrastructure Project
# ============================================
Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "PHASE 6: Infrastructure Project Migration" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

Copy-ProjectWithRename `
    -OldPath (Join-Path $srcPath "Infrastructure") `
    -NewPath (Join-Path $srcPath "E470.AuditLog.Infrastructure") `
    -ProjectName "E470.AuditLog.Infrastructure"

# ============================================
# PHASE 7: Web.Api Project
# ============================================
Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "PHASE 7: Web.Api Project Migration" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

Copy-ProjectWithRename `
    -OldPath (Join-Path $srcPath "Web.Api") `
    -NewPath (Join-Path $srcPath "E470.AuditLog.Web.Api") `
    -ProjectName "E470.AuditLog.Web.Api"

# ============================================
# PHASE 8: ArchitectureTests Project
# ============================================
Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "PHASE 8: ArchitectureTests Project Migration" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

Copy-ProjectWithRename `
    -OldPath (Join-Path $testsPath "ArchitectureTests") `
    -NewPath (Join-Path $testsPath "E470.AuditLog.ArchitectureTests") `
    -ProjectName "E470.AuditLog.ArchitectureTests"

# ============================================
# Configuration Files Update
# ============================================
Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "Configuration Files Update" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

# Docker Compose files
$dockerFiles = @(
    (Join-Path $rootPath "docker\compose.webapi.yml"),
    (Join-Path $rootPath "docker\compose.webapi.override.yml")
)

$dockerMappings = @{
    'dockerfile: src/Web\.Api/Dockerfile' = 'dockerfile: src/E470.AuditLog.Web.Api/Dockerfile'
    'image: auditlog-webapi' = 'image: e470auditlog-webapi'
    'container_name: auditlog-webapi' = 'container_name: e470auditlog-webapi'
}

foreach ($file in $dockerFiles) {
    if (Test-Path $file) {
        Update-FileContent -FilePath $file -Mappings $dockerMappings -Description "Docker Configuration"
    }
}

# Dockerfile
$dockerfilePath = Join-Path $srcPath "E470.AuditLog.Web.Api\Dockerfile"
if (Test-Path $dockerfilePath) {
    $dockerfileMappings = @{
        'src/Web\.Api/' = 'src/E470.AuditLog.Web.Api/'
        'src/Infrastructure/' = 'src/E470.AuditLog.Infrastructure/'
        'src/Application/' = 'src/E470.AuditLog.Application/'
        'src/Domain/' = 'src/E470.AuditLog.Domain/'
        'src/EventBusClient/' = 'src/E470.AuditLog.EventBusClient/'
        'src/SharedKernel/' = 'src/E470.AuditLog.SharedKernel/'
        'src/AuditLog\.ServiceDefaults/' = 'src/E470.AuditLog.ServiceDefaults/'
        'Web\.Api\.csproj' = 'E470.AuditLog.Web.Api.csproj'
        'Infrastructure\.csproj' = 'E470.AuditLog.Infrastructure.csproj'
        'Application\.csproj' = 'E470.AuditLog.Application.csproj'
        'Domain\.csproj' = 'E470.AuditLog.Domain.csproj'
        'EventBusClient\.csproj' = 'E470.AuditLog.EventBusClient.csproj'
        'SharedKernel\.csproj' = 'E470.AuditLog.SharedKernel.csproj'
        'Web\.Api\.dll' = 'E470.AuditLog.Web.Api.dll'
    }
    Update-FileContent -FilePath $dockerfilePath -Mappings $dockerfileMappings -Description "Dockerfile"
}

# ============================================
# Summary
# ============================================
Write-Host "`n===============================================" -ForegroundColor Cyan
Write-Host "Migration Complete!" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "`n[DRY RUN] No actual changes were made." -ForegroundColor Yellow
    Write-Host "Run without -DryRun parameter to apply changes." -ForegroundColor Yellow
} else {
    Write-Host "`nNext Steps:" -ForegroundColor Yellow
    Write-Host "1. Update solution file (E470.AuditLog.sln)" -ForegroundColor White
    Write-Host "2. Delete old project folders" -ForegroundColor White
    Write-Host "3. Build solution: dotnet build E470.AuditLog.sln" -ForegroundColor White
    Write-Host "4. Run tests: dotnet test E470.AuditLog.sln" -ForegroundColor White
    Write-Host "5. Commit changes: git add . && git commit -m 'Complete E470 prefix migration'" -ForegroundColor White
}

Write-Host "`nFor detailed migration plan, see MIGRATION_PLAN_E470.md" -ForegroundColor Cyan
Write-Host ""
