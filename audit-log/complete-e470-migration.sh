#!/bin/bash

# E470.AuditLog Complete Migration Script
# This script renames all remaining projects and updates namespaces
# Run from the audit-log directory

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Options
DRY_RUN=false
VERBOSE=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --dry-run)
            DRY_RUN=true
            shift
            ;;
        --verbose)
            VERBOSE=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

echo -e "${CYAN}========================================"
echo "E470.AuditLog Complete Migration Script"
echo -e "========================================${NC}"
echo ""

if [ "$DRY_RUN" = true ]; then
    echo -e "${YELLOW}[DRY RUN MODE - No changes will be made]${NC}"
    echo ""
fi

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

update_namespaces() {
    local file=$1
    
    if [ ! -f "$file" ]; then
        log_warn "File not found: $file"
        return
    fi
    
    if [ "$DRY_RUN" = false ]; then
        # Update namespaces
        sed -i.bak 's/namespace EventBusClient/namespace E470.AuditLog.EventBusClient/g' "$file"
        sed -i.bak 's/namespace Application/namespace E470.AuditLog.Application/g' "$file"
        sed -i.bak 's/namespace Infrastructure/namespace E470.AuditLog.Infrastructure/g' "$file"
        sed -i.bak 's/namespace Web\.Api/namespace E470.AuditLog.Web.Api/g' "$file"
        sed -i.bak 's/namespace Domain/namespace E470.AuditLog.Domain/g' "$file"
        sed -i.bak 's/namespace SharedKernel/namespace E470.AuditLog.SharedKernel/g' "$file"
        
        # Update using statements
        sed -i.bak 's/using EventBusClient/using E470.AuditLog.EventBusClient/g' "$file"
        sed -i.bak 's/using Application/using E470.AuditLog.Application/g' "$file"
        sed -i.bak 's/using Infrastructure/using E470.AuditLog.Infrastructure/g' "$file"
        sed -i.bak 's/using Web\.Api/using E470.AuditLog.Web.Api/g' "$file"
        sed -i.bak 's/using Domain/using E470.AuditLog.Domain/g' "$file"
        sed -i.bak 's/using SharedKernel/using E470.AuditLog.SharedKernel/g' "$file"
        
        # Remove backup file
        rm -f "${file}.bak"
        
        log_info "Updated: $file"
    else
        log_info "[DRY RUN] Would update: $file"
    fi
}

update_project_references() {
    local file=$1
    
    if [ ! -f "$file" ]; then
        log_warn "File not found: $file"
        return
    fi
    
    if [ "$DRY_RUN" = false ]; then
        sed -i.bak 's|\\EventBusClient\\EventBusClient\.csproj|\\E470.AuditLog.EventBusClient\\E470.AuditLog.EventBusClient.csproj|g' "$file"
        sed -i.bak 's|\\Application\\Application\.csproj|\\E470.AuditLog.Application\\E470.AuditLog.Application.csproj|g' "$file"
        sed -i.bak 's|\\Infrastructure\\Infrastructure\.csproj|\\E470.AuditLog.Infrastructure\\E470.AuditLog.Infrastructure.csproj|g' "$file"
        sed -i.bak 's|\\Domain\\Domain\.csproj|\\E470.AuditLog.Domain\\E470.AuditLog.Domain.csproj|g' "$file"
        sed -i.bak 's|\\SharedKernel\\SharedKernel\.csproj|\\E470.AuditLog.SharedKernel\\E470.AuditLog.SharedKernel.csproj|g' "$file"
        sed -i.bak 's|\\Web\.Api\\Web\.Api\.csproj|\\E470.AuditLog.Web.Api\\E470.AuditLog.Web.Api.csproj|g' "$file"
        
        rm -f "${file}.bak"
        log_info "Updated references in: $file"
    else
        log_info "[DRY RUN] Would update references in: $file"
    fi
}

# Step 1: Create EventBusClient project
echo ""
echo -e "${CYAN}Step 1: Creating E470.AuditLog.EventBusClient project...${NC}"
EVENT_BUS_PATH="src/E470.AuditLog.EventBusClient"
if [ "$DRY_RUN" = false ]; then
    mkdir -p "$EVENT_BUS_PATH"
fi
log_info "Created directory: $EVENT_BUS_PATH"

# Step 2: Create Application project
echo ""
echo -e "${CYAN}Step 2: Creating E470.AuditLog.Application project...${NC}"
APP_PATH="src/E470.AuditLog.Application"
if [ "$DRY_RUN" = false ]; then
    mkdir -p "$APP_PATH"
fi
log_info "Created directory: $APP_PATH"

# Step 3: Create Infrastructure project
echo ""
echo -e "${CYAN}Step 3: Creating E470.AuditLog.Infrastructure project...${NC}"
INFRA_PATH="src/E470.AuditLog.Infrastructure"
if [ "$DRY_RUN" = false ]; then
    mkdir -p "$INFRA_PATH"
fi
log_info "Created directory: $INFRA_PATH"

# Step 4: Create Web.Api project
echo ""
echo -e "${CYAN}Step 4: Creating E470.AuditLog.Web.Api project...${NC}"
WEB_API_PATH="src/E470.AuditLog.Web.Api"
if [ "$DRY_RUN" = false ]; then
    mkdir -p "$WEB_API_PATH"
fi
log_info "Created directory: $WEB_API_PATH"

# Step 5: Create ArchitectureTests project
echo ""
echo -e "${CYAN}Step 5: Creating E470.AuditLog.ArchitectureTests project...${NC}"
TESTS_PATH="tests/E470.AuditLog.ArchitectureTests"
if [ "$DRY_RUN" = false ]; then
    mkdir -p "$TESTS_PATH"
fi
log_info "Created directory: $TESTS_PATH"

# Step 6: Copy and update files
echo ""
echo -e "${CYAN}Step 6: Copying and updating files...${NC}"

declare -A file_mappings=(
    ["src/EventBusClient"]="src/E470.AuditLog.EventBusClient"
    ["src/Application"]="src/E470.AuditLog.Application"
    ["src/Infrastructure"]="src/E470.AuditLog.Infrastructure"
    ["src/Web.Api"]="src/E470.AuditLog.Web.Api"
    ["tests/ArchitectureTests"]="tests/E470.AuditLog.ArchitectureTests"
)

for source in "${!file_mappings[@]}"; do
    dest="${file_mappings[$source]}"
    
    if [ -d "$source" ]; then
        log_info "Processing: $source -> $dest"
        
        if [ "$DRY_RUN" = false ]; then
            # Copy all files
            cp -r "$source/"* "$dest/" 2>/dev/null || true
            
            # Update all .cs files
            find "$dest" -name "*.cs" -type f | while read -r file; do
                update_namespaces "$file"
            done
            
            # Update all .csproj files
            find "$dest" -name "*.csproj" -type f | while read -r file; do
                update_project_references "$file"
                update_namespaces "$file"
            done
            
            # Update appsettings files
            find "$dest" -name "appsettings*.json" -type f | while read -r file; do
                sed -i.bak 's/AuditLog/E470.AuditLog/g' "$file"
                rm -f "${file}.bak"
            done
        else
            log_info "[DRY RUN] Would copy and update files from $source to $dest"
        fi
    fi
done

# Step 7: Update solution file
echo ""
echo -e "${CYAN}Step 7: Updating solution files...${NC}"

if [ -f "E470.AuditLog.sln" ]; then
    if [ "$DRY_RUN" = false ]; then
        sed -i.bak 's|SharedKernel\\SharedKernel.csproj|E470.AuditLog.SharedKernel\\E470.AuditLog.SharedKernel.csproj|g' "E470.AuditLog.sln"
        sed -i.bak 's|Domain\\Domain.csproj|E470.AuditLog.Domain\\E470.AuditLog.Domain.csproj|g' "E470.AuditLog.sln"
        sed -i.bak 's|Application\\Application.csproj|E470.AuditLog.Application\\E470.AuditLog.Application.csproj|g' "E470.AuditLog.sln"
        sed -i.bak 's|Infrastructure\\Infrastructure.csproj|E470.AuditLog.Infrastructure\\E470.AuditLog.Infrastructure.csproj|g' "E470.AuditLog.sln"
        sed -i.bak 's|EventBusClient\\EventBusClient.csproj|E470.AuditLog.EventBusClient\\E470.AuditLog.EventBusClient.csproj|g' "E470.AuditLog.sln"
        sed -i.bak 's|Web\.Api\\Web\.Api.csproj|E470.AuditLog.Web.Api\\E470.AuditLog.Web.Api.csproj|g' "E470.AuditLog.sln"
        sed -i.bak 's|ArchitectureTests\\ArchitectureTests.csproj|E470.AuditLog.ArchitectureTests\\E470.AuditLog.ArchitectureTests.csproj|g' "E470.AuditLog.sln"
        
        rm -f "E470.AuditLog.sln.bak"
        log_info "Updated E470.AuditLog.sln"
    else
        log_info "[DRY RUN] Would update E470.AuditLog.sln"
    fi
fi

# Step 8: Update Docker files
echo ""
echo -e "${CYAN}Step 8: Updating Docker files...${NC}"

DOCKER_FILE="src/E470.AuditLog.Web.Api/Dockerfile"
if [ -f "src/Web.Api/Dockerfile" ]; then
    if [ "$DRY_RUN" = false ]; then
        cp "src/Web.Api/Dockerfile" "$DOCKER_FILE"
        
        sed -i.bak 's|src/Web\.Api/Web\.Api\.csproj|src/E470.AuditLog.Web.Api/E470.AuditLog.Web.Api.csproj|g' "$DOCKER_FILE"
        sed -i.bak 's|src/AuditLog\.ServiceDefaults/AuditLog\.ServiceDefaults\.csproj|src/E470.AuditLog.ServiceDefaults/E470.AuditLog.ServiceDefaults.csproj|g' "$DOCKER_FILE"
        sed -i.bak 's|src/Infrastructure/Infrastructure\.csproj|src/E470.AuditLog.Infrastructure/E470.AuditLog.Infrastructure.csproj|g' "$DOCKER_FILE"
        sed -i.bak 's|src/Application/Application\.csproj|src/E470.AuditLog.Application/E470.AuditLog.Application.csproj|g' "$DOCKER_FILE"
        sed -i.bak 's|src/Domain/Domain\.csproj|src/E470.AuditLog.Domain/E470.AuditLog.Domain.csproj|g' "$DOCKER_FILE"
        sed -i.bak 's|src/SharedKernel/SharedKernel\.csproj|src/E470.AuditLog.SharedKernel/E470.AuditLog.SharedKernel.csproj|g' "$DOCKER_FILE"
        sed -i.bak 's|src/EventBusClient/EventBusClient\.csproj|src/E470.AuditLog.EventBusClient/E470.AuditLog.EventBusClient.csproj|g' "$DOCKER_FILE"
        sed -i.bak 's|WORKDIR "/src/src/Web\.Api"|WORKDIR "/src/src/E470.AuditLog.Web.Api"|g' "$DOCKER_FILE"
        sed -i.bak 's|dotnet restore "./src/Web\.Api/Web\.Api\.csproj"|dotnet restore "./src/E470.AuditLog.Web.Api/E470.AuditLog.Web.Api.csproj"|g' "$DOCKER_FILE"
        sed -i.bak 's|dotnet build "./Web\.Api\.csproj"|dotnet build "./E470.AuditLog.Web.Api.csproj"|g' "$DOCKER_FILE"
        sed -i.bak 's|dotnet publish "./Web\.Api\.csproj"|dotnet publish "./E470.AuditLog.Web.Api.csproj"|g' "$DOCKER_FILE"
        sed -i.bak 's|ENTRYPOINT \["dotnet", "Web\.Api\.dll"\]|ENTRYPOINT ["dotnet", "E470.AuditLog.Web.Api.dll"]|g' "$DOCKER_FILE"
        
        rm -f "${DOCKER_FILE}.bak"
        log_info "Updated Dockerfile"
    else
        log_info "[DRY RUN] Would update Dockerfile"
    fi
fi

# Step 9: Update GitHub workflow
echo ""
echo -e "${CYAN}Step 9: Updating GitHub workflow...${NC}"

WORKFLOW_FILE=".github/workflows/build.yml"
if [ -f "$WORKFLOW_FILE" ]; then
    if [ "$DRY_RUN" = false ]; then
        sed -i.bak 's|AuditLog\.sln|E470.AuditLog.sln|g' "$WORKFLOW_FILE"
        sed -i.bak 's|audit-log/AuditLog\.sln|audit-log/E470.AuditLog.sln|g' "$WORKFLOW_FILE"
        rm -f "${WORKFLOW_FILE}.bak"
        log_info "Updated build.yml"
    else
        log_info "[DRY RUN] Would update build.yml"
    fi
fi

# Step 10: Summary
echo ""
echo -e "${CYAN}========================================"
echo "Migration Summary"
echo -e "========================================${NC}"
echo ""
echo -e "${GREEN}Projects created:${NC}"
echo -e "  ${GREEN}✓${NC} E470.AuditLog.EventBusClient"
echo -e "  ${GREEN}✓${NC} E470.AuditLog.Application"
echo -e "  ${GREEN}✓${NC} E470.AuditLog.Infrastructure"
echo -e "  ${GREEN}✓${NC} E470.AuditLog.Web.Api"
echo -e "  ${GREEN}✓${NC} E470.AuditLog.ArchitectureTests"
echo ""
echo -e "${GREEN}Files updated:${NC}"
echo -e "  ${GREEN}✓${NC} All .cs files (namespaces)"
echo -e "  ${GREEN}✓${NC} All .csproj files (references)"
echo -e "  ${GREEN}✓${NC} Solution files"
echo -e "  ${GREEN}✓${NC} Docker files"
echo -e "  ${GREEN}✓${NC} GitHub workflow"
echo ""

if [ "$DRY_RUN" = true ]; then
    echo -e "${YELLOW}[DRY RUN] No actual changes were made. Run without --dry-run to apply changes.${NC}"
else
    echo -e "${CYAN}Next steps:${NC}"
    echo "1. Build the solution: dotnet build E470.AuditLog.sln"
    echo "2. Run tests: dotnet test E470.AuditLog.sln"
    echo "3. Commit changes: git add . && git commit -m 'Complete E470 migration'"
    echo "4. Push to remote: git push origin feature/rename-to-e470-auditlog"
fi

echo ""
echo -e "${GREEN}Migration script completed successfully!${NC}"
echo ""
