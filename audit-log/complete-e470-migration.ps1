# E470.AuditLog Complete Migration Script
# This script renames all remaining projects and updates namespaces
# Run from the audit-log directory

param(
    [switch]$DryRun = $false,
    [switch]$Verbose = $false
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "E470.AuditLog Complete Migration Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($DryRun) {
    Write-Host "[DRY RUN MODE - No changes will be made]" -ForegroundColor Yellow
    Write-Host ""
}

# Define project mappings
$projectMappings = @{
    "EventBusClient" = "E470.AuditLog.EventBusClient"
    "Application" = "E470.AuditLog.Application"
    "Infrastructure" = "E470.AuditLog.Infrastructure"
    "Web.Api" = "E470.AuditLog.Web.Api"
    "ArchitectureTests" = "E470.AuditLog.ArchitectureTests"
}

# Define namespace mappings
$namespaceMappings = @{
    "EventBusClient" = "E470.AuditLog.EventBusClient"
    "Application" = "E470.AuditLog.Application"
    "Infrastructure" = "E470.AuditLog.Infrastructure"
    "Web.Api" = "E470.AuditLog.Web.Api"
    "Domain" = "E470.AuditLog.Domain"
    "SharedKernel" = "E470.AuditLog.SharedKernel"
}

function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARN] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function Update-NamespacesInFile {
    param(
        [string]$FilePath,
        [hashtable]$Mappings
    )
    
    if (-not (Test-Path $FilePath)) {
        Write-Warning "File not found: $FilePath"
        return
    }
    
    $content = Get-Content $FilePath -Raw
    $originalContent = $content
    
    # Update namespaces
    foreach ($old in $Mappings.Keys) {
        $new = $Mappings[$old]
        $content = $content -replace "namespace $old", "namespace $new"
        $content = $content -replace "using $old", "using $new"
    }
    
    if ($content -ne $originalContent) {
        if (-not $DryRun) {
            Set-Content -Path $FilePath -Value $content -NoNewline
            Write-Info "Updated: $FilePath"
        } else {
            Write-Info "[DRY RUN] Would update: $FilePath"
        }
        return $true
    }
    
    return $false
}

function Update-ProjectReferences {
    param(
        [string]$FilePath,
        [hashtable]$Mappings
    )
    
    if (-not (Test-Path $FilePath)) {
        Write-Warning "File not found: $FilePath"
        return
    }
    
    $content = Get-Content $FilePath -Raw
    $originalContent = $content
    
    # Update project references
    foreach ($old in $Mappings.Keys) {
        $new = $Mappings[$old]
        $content = $content -replace "\\$old\\$old\.csproj", "\$new\$new.csproj"
        $content = $content -replace "\.\.$old\.ServiceDefaults\\AuditLog\.ServiceDefaults", "..\$new.ServiceDefaults\$new.ServiceDefaults"
    }
    
    if ($content -ne $originalContent) {
        if (-not $DryRun) {
            Set-Content -Path $FilePath -Value $content -NoNewline
            Write-Info "Updated references in: $FilePath"
        } else {
            Write-Info "[DRY RUN] Would update references in: $FilePath"
        }
        return $true
    }
    
    return $false
}

# Step 1: Create EventBusClient project
Write-Host ""
Write-Host "Step 1: Creating E470.AuditLog.EventBusClient project..." -ForegroundColor Cyan
$eventBusPath = "src\E470.AuditLog.EventBusClient"
if (-not $DryRun) {
    New-Item -ItemType Directory -Force -Path $eventBusPath | Out-Null
}
Write-Info "Created directory: $eventBusPath"

# Step 2: Create Application project
Write-Host ""
Write-Host "Step 2: Creating E470.AuditLog.Application project..." -ForegroundColor Cyan
$applicationPath = "src\E470.AuditLog.Application"
if (-not $DryRun) {
    New-Item -ItemType Directory -Force -Path $applicationPath | Out-Null
}
Write-Info "Created directory: $applicationPath"

# Step 3: Create Infrastructure project
Write-Host ""
Write-Host "Step 3: Creating E470.AuditLog.Infrastructure project..." -ForegroundColor Cyan
$infrastructurePath = "src\E470.AuditLog.Infrastructure"
if (-not $DryRun) {
    New-Item -ItemType Directory -Force -Path $infrastructurePath | Out-Null
}
Write-Info "Created directory: $infrastructurePath"

# Step 4: Create Web.Api project
Write-Host ""
Write-Host "Step 4: Creating E470.AuditLog.Web.Api project..." -ForegroundColor Cyan
$webApiPath = "src\E470.AuditLog.Web.Api"
if (-not $DryRun) {
    New-Item -ItemType Directory -Force -Path $webApiPath | Out-Null
}
Write-Info "Created directory: $webApiPath"

# Step 5: Create ArchitectureTests project
Write-Host ""
Write-Host "Step 5: Creating E470.AuditLog.ArchitectureTests project..." -ForegroundColor Cyan
$testsPath = "tests\E470.AuditLog.ArchitectureTests"
if (-not $DryRun) {
    New-Item -ItemType Directory -Force -Path $testsPath | Out-Null
}
Write-Info "Created directory: $testsPath"

# Step 6: Copy and update all files
Write-Host ""
Write-Host "Step 6: Copying and updating files..." -ForegroundColor Cyan

$filesToProcess = @(
    @{Source="src\EventBusClient"; Dest="src\E470.AuditLog.EventBusClient"},
    @{Source="src\Application"; Dest="src\E470.AuditLog.Application"},
    @{Source="src\Infrastructure"; Dest="src\E470.AuditLog.Infrastructure"},
    @{Source="src\Web.Api"; Dest="src\E470.AuditLog.Web.Api"},
    @{Source="tests\ArchitectureTests"; Dest="tests\E470.AuditLog.ArchitectureTests"}
)

foreach ($mapping in $filesToProcess) {
    $sourceDir = $mapping.Source
    $destDir = $mapping.Dest
    
    if (Test-Path $sourceDir) {
        Write-Info "Processing: $sourceDir -> $destDir"
        
        if (-not $DryRun) {
            # Copy all files
            Copy-Item -Path "$sourceDir\*" -Destination $destDir -Recurse -Force
            
            # Update all .cs files
            Get-ChildItem -Path $destDir -Filter "*.cs" -Recurse | ForEach-Object {
                Update-NamespacesInFile -FilePath $_.FullName -Mappings $namespaceMappings
            }
            
            # Update all .csproj files
            Get-ChildItem -Path $destDir -Filter "*.csproj" -Recurse | ForEach-Object {
                Update-ProjectReferences -FilePath $_.FullName -Mappings $projectMappings
                Update-NamespacesInFile -FilePath $_.FullName -Mappings $namespaceMappings
            }
            
            # Update appsettings files
            Get-ChildItem -Path $destDir -Filter "appsettings*.json" -Recurse | ForEach-Object {
                $content = Get-Content $_.FullName -Raw
                $content = $content -replace "AuditLog", "E470.AuditLog"
                Set-Content -Path $_.FullName -Value $content -NoNewline
            }
        } else {
            Write-Info "[DRY RUN] Would copy and update files from $sourceDir to $destDir"
        }
    }
}

# Step 7: Update solution file
Write-Host ""
Write-Host "Step 7: Updating solution files..." -ForegroundColor Cyan

if (Test-Path "E470.AuditLog.sln") {
    if (-not $DryRun) {
        $slnContent = Get-Content "E470.AuditLog.sln" -Raw
        
        # Add new projects
        $slnContent = $slnContent -replace 'SharedKernel\\SharedKernel.csproj', 'E470.AuditLog.SharedKernel\E470.AuditLog.SharedKernel.csproj'
        $slnContent = $slnContent -replace 'Domain\\Domain.csproj', 'E470.AuditLog.Domain\E470.AuditLog.Domain.csproj'
        $slnContent = $slnContent -replace 'Application\\Application.csproj', 'E470.AuditLog.Application\E470.AuditLog.Application.csproj'
        $slnContent = $slnContent -replace 'Infrastructure\\Infrastructure.csproj', 'E470.AuditLog.Infrastructure\E470.AuditLog.Infrastructure.csproj'
        $slnContent = $slnContent -replace 'EventBusClient\\EventBusClient.csproj', 'E470.AuditLog.EventBusClient\E470.AuditLog.EventBusClient.csproj'
        $slnContent = $slnContent -replace 'Web\.Api\\Web\.Api.csproj', 'E470.AuditLog.Web.Api\E470.AuditLog.Web.Api.csproj'
        $slnContent = $slnContent -replace 'ArchitectureTests\\ArchitectureTests.csproj', 'E470.AuditLog.ArchitectureTests\E470.AuditLog.ArchitectureTests.csproj'
        
        Set-Content -Path "E470.AuditLog.sln" -Value $slnContent -NoNewline
        Write-Info "Updated E470.AuditLog.sln"
    } else {
        Write-Info "[DRY RUN] Would update E470.AuditLog.sln"
    }
}

# Step 8: Update Docker files
Write-Host ""
Write-Host "Step 8: Updating Docker files..." -ForegroundColor Cyan

$dockerFile = "src\E470.AuditLog.Web.Api\Dockerfile"
if (Test-Path "src\Web.Api\Dockerfile") {
    if (-not $DryRun) {
        $dockerContent = Get-Content "src\Web.Api\Dockerfile" -Raw
        $dockerContent = $dockerContent -replace "src/Web\.Api/Web\.Api\.csproj", "src/E470.AuditLog.Web.Api/E470.AuditLog.Web.Api.csproj"
        $dockerContent = $dockerContent -replace "src/AuditLog\.ServiceDefaults/AuditLog\.ServiceDefaults\.csproj", "src/E470.AuditLog.ServiceDefaults/E470.AuditLog.ServiceDefaults.csproj"
        $dockerContent = $dockerContent -replace "src/Infrastructure/Infrastructure\.csproj", "src/E470.AuditLog.Infrastructure/E470.AuditLog.Infrastructure.csproj"
        $dockerContent = $dockerContent -replace "src/Application/Application\.csproj", "src/E470.AuditLog.Application/E470.AuditLog.Application.csproj"
        $dockerContent = $dockerContent -replace "src/Domain/Domain\.csproj", "src/E470.AuditLog.Domain/E470.AuditLog.Domain.csproj"
        $dockerContent = $dockerContent -replace "src/SharedKernel/SharedKernel\.csproj", "src/E470.AuditLog.SharedKernel/E470.AuditLog.SharedKernel.csproj"
        $dockerContent = $dockerContent -replace "src/EventBusClient/EventBusClient\.csproj", "src/E470.AuditLog.EventBusClient/E470.AuditLog.EventBusClient.csproj"
        $dockerContent = $dockerContent -replace 'WORKDIR "/src/src/Web\.Api"', 'WORKDIR "/src/src/E470.AuditLog.Web.Api"'
        $dockerContent = $dockerContent -replace 'dotnet restore "\./src/Web\.Api/Web\.Api\.csproj"', 'dotnet restore "./src/E470.AuditLog.Web.Api/E470.AuditLog.Web.Api.csproj"'
        $dockerContent = $dockerContent -replace 'dotnet build "\./Web\.Api\.csproj"', 'dotnet build "./E470.AuditLog.Web.Api.csproj"'
        $dockerContent = $dockerContent -replace 'dotnet publish "\./Web\.Api\.csproj"', 'dotnet publish "./E470.AuditLog.Web.Api.csproj"'
        $dockerContent = $dockerContent -replace 'ENTRYPOINT \["dotnet", "Web\.Api\.dll"\]', 'ENTRYPOINT ["dotnet", "E470.AuditLog.Web.Api.dll"]'
        
        Set-Content -Path $dockerFile -Value $dockerContent -NoNewline
        Write-Info "Updated Dockerfile"
    } else {
        Write-Info "[DRY RUN] Would update Dockerfile"
    }
}

# Step 9: Update GitHub workflow
Write-Host ""
Write-Host "Step 9: Updating GitHub workflow..." -ForegroundColor Cyan

$workflowFile = ".github\workflows\build.yml"
if (Test-Path $workflowFile) {
    if (-not $DryRun) {
        $workflowContent = Get-Content $workflowFile -Raw
        $workflowContent = $workflowContent -replace "AuditLog\.sln", "E470.AuditLog.sln"
        $workflowContent = $workflowContent -replace "audit-log/AuditLog\.sln", "audit-log/E470.AuditLog.sln"
        Set-Content -Path $workflowFile -Value $workflowContent -NoNewline
        Write-Info "Updated build.yml"
    } else {
        Write-Info "[DRY RUN] Would update build.yml"
    }
}

# Step 10: Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Migration Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Projects created:" -ForegroundColor Green
Write-Host "  ✓ E470.AuditLog.EventBusClient" -ForegroundColor Green
Write-Host "  ✓ E470.AuditLog.Application" -ForegroundColor Green
Write-Host "  ✓ E470.AuditLog.Infrastructure" -ForegroundColor Green
Write-Host "  ✓ E470.AuditLog.Web.Api" -ForegroundColor Green
Write-Host "  ✓ E470.AuditLog.ArchitectureTests" -ForegroundColor Green
Write-Host ""
Write-Host "Files updated:" -ForegroundColor Green
Write-Host "  ✓ All .cs files (namespaces)" -ForegroundColor Green
Write-Host "  ✓ All .csproj files (references)" -ForegroundColor Green
Write-Host "  ✓ Solution files" -ForegroundColor Green
Write-Host "  ✓ Docker files" -ForegroundColor Green
Write-Host "  ✓ GitHub workflow" -ForegroundColor Green
Write-Host ""

if ($DryRun) {
    Write-Host "[DRY RUN] No actual changes were made. Run without -DryRun to apply changes." -ForegroundColor Yellow
} else {
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Build the solution: dotnet build E470.AuditLog.sln" -ForegroundColor White
    Write-Host "2. Run tests: dotnet test E470.AuditLog.sln" -ForegroundColor White
    Write-Host "3. Commit changes: git add . && git commit -m 'Complete E470 migration'" -ForegroundColor White
    Write-Host "4. Push to remote: git push origin feature/rename-to-e470-auditlog" -ForegroundColor White
}

Write-Host ""
Write-Host "Migration script completed successfully!" -ForegroundColor Green
Write-Host ""
