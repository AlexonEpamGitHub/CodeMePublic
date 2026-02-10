#!/bin/bash

# E470.AuditLog Migration Script (Linux/macOS)
# This script automates the migration of all projects and namespaces to include E470.AuditLog prefix

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
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
            echo "Usage: $0 [--dry-run] [--verbose]"
            exit 1
            ;;
    esac
done

ROOT_PATH="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SRC_PATH="$ROOT_PATH/src"
TESTS_PATH="$ROOT_PATH/tests"

echo -e "${CYAN}===============================================${NC}"
echo -e "${CYAN}E470.AuditLog Migration Script${NC}"
echo -e "${CYAN}===============================================${NC}"
echo ""

if [ "$DRY_RUN" = true ]; then
    echo -e "${YELLOW}[DRY RUN MODE] No changes will be made${NC}"
    echo ""
fi

# Function to update file content
update_file_content() {
    local file_path=$1
    local description=$2
    
    if [ ! -f "$file_path" ]; then
        return 1
    fi
    
    local temp_file="${file_path}.tmp"
    local changed=false
    
    # Create a copy for modification
    cp "$file_path" "$temp_file"
    
    # Namespace replacements
    sed -i 's/^using SharedKernel/using E470.AuditLog.SharedKernel/g' "$temp_file"
    sed -i 's/^namespace SharedKernel/namespace E470.AuditLog.SharedKernel/g' "$temp_file"
    sed -i 's/^using Domain/using E470.AuditLog.Domain/g' "$temp_file"
    sed -i 's/^namespace Domain/namespace E470.AuditLog.Domain/g' "$temp_file"
    sed -i 's/^using Application/using E470.AuditLog.Application/g' "$temp_file"
    sed -i 's/^namespace Application/namespace E470.AuditLog.Application/g' "$temp_file"
    sed -i 's/^using EventBusClient/using E470.AuditLog.EventBusClient/g' "$temp_file"
    sed -i 's/^namespace EventBusClient/namespace E470.AuditLog.EventBusClient/g' "$temp_file"
    sed -i 's/^using Infrastructure/using E470.AuditLog.Infrastructure/g' "$temp_file"
    sed -i 's/^namespace Infrastructure/namespace E470.AuditLog.Infrastructure/g' "$temp_file"
    sed -i 's/^using Web\.Api/using E470.AuditLog.Web.Api/g' "$temp_file"
    sed -i 's/^namespace Web\.Api/namespace E470.AuditLog.Web.Api/g' "$temp_file"
    sed -i 's/^using ArchitectureTests/using E470.AuditLog.ArchitectureTests/g' "$temp_file"
    sed -i 's/^namespace ArchitectureTests/namespace E470.AuditLog.ArchitectureTests/g' "$temp_file"
    
    # Project reference replacements
    sed -i 's|\\SharedKernel\\SharedKernel\.csproj|\\E470.AuditLog.SharedKernel\\E470.AuditLog.SharedKernel.csproj|g' "$temp_file"
    sed -i 's|\\Domain\\Domain\.csproj|\\E470.AuditLog.Domain\\E470.AuditLog.Domain.csproj|g' "$temp_file"
    sed -i 's|\\Application\\Application\.csproj|\\E470.AuditLog.Application\\E470.AuditLog.Application.csproj|g' "$temp_file"
    sed -i 's|\\EventBusClient\\EventBusClient\.csproj|\\E470.AuditLog.EventBusClient\\E470.AuditLog.EventBusClient.csproj|g' "$temp_file"
    sed -i 's|\\Infrastructure\\Infrastructure\.csproj|\\E470.AuditLog.Infrastructure\\E470.AuditLog.Infrastructure.csproj|g' "$temp_file"
    sed -i 's|\\AuditLog\.ServiceDefaults\\AuditLog\.ServiceDefaults\.csproj|\\E470.AuditLog.ServiceDefaults\\E470.AuditLog.ServiceDefaults.csproj|g' "$temp_file"
    
    # InternalsVisibleTo replacements
    sed -i 's/"Application\.UnitTests"/"E470.AuditLog.Application.UnitTests"/g' "$temp_file"
    sed -i 's/"ArchitectureTests"/"E470.AuditLog.ArchitectureTests"/g' "$temp_file"
    
    # Check if file changed
    if ! cmp -s "$file_path" "$temp_file"; then
        changed=true
        echo -e "  ${GREEN}✓${NC} $description: $(basename "$file_path")"
        
        if [ "$DRY_RUN" = false ]; then
            mv "$temp_file" "$file_path"
        else
            rm "$temp_file"
        fi
    else
        rm "$temp_file"
    fi
    
    return 0
}

# Function to copy project with rename
copy_project_with_rename() {
    local old_path=$1
    local new_path=$2
    local project_name=$3
    
    echo ""
    echo -e "${CYAN}Migrating: $project_name${NC}"
    echo -e "From: $old_path"
    echo -e "To:   $new_path"
    
    if [ ! -d "$old_path" ]; then
        echo -e "  ${YELLOW}⚠${NC} Source path not found, skipping"
        return
    fi
    
    if [ -d "$new_path" ]; then
        echo -e "  ${YELLOW}⚠${NC} Target already exists, skipping"
        return
    fi
    
    if [ "$DRY_RUN" = false ]; then
        # Copy directory
        cp -r "$old_path" "$new_path"
        echo -e "  ${GREEN}✓${NC} Project copied"
    else
        echo -e "  ${YELLOW}[DRY RUN]${NC} Would copy project"
    fi
    
    # Update all .cs files
    local updated_count=0
    local search_path="$old_path"
    if [ "$DRY_RUN" = false ]; then
        search_path="$new_path"
    fi
    
    while IFS= read -r -d '' file; do
        if update_file_content "$file" "Namespace"; then
            ((updated_count++))
        fi
    done < <(find "$search_path" -name "*.cs" -type f -print0)
    
    # Update .csproj files
    while IFS= read -r -d '' file; do
        update_file_content "$file" "Project Reference"
    done < <(find "$search_path" -name "*.csproj" -type f -print0)
    
    echo -e "  ${GREEN}✓${NC} Updated $updated_count C# files"
}

# ============================================
# PHASE 3: Domain Project
# ============================================
echo ""
echo -e "${CYAN}============================================${NC}"
echo -e "${CYAN}PHASE 3: Domain Project Migration${NC}"
echo -e "${CYAN}============================================${NC}"

copy_project_with_rename \
    "$SRC_PATH/Domain" \
    "$SRC_PATH/E470.AuditLog.Domain" \
    "E470.AuditLog.Domain"

# ============================================
# PHASE 4: Application Project
# ============================================
echo ""
echo -e "${CYAN}============================================${NC}"
echo -e "${CYAN}PHASE 4: Application Project Migration${NC}"
echo -e "${CYAN}============================================${NC}"

copy_project_with_rename \
    "$SRC_PATH/Application" \
    "$SRC_PATH/E470.AuditLog.Application" \
    "E470.AuditLog.Application"

# ============================================
# PHASE 5: EventBusClient Project
# ============================================
echo ""
echo -e "${CYAN}============================================${NC}"
echo -e "${CYAN}PHASE 5: EventBusClient Project Migration${NC}"
echo -e "${CYAN}============================================${NC}"

copy_project_with_rename \
    "$SRC_PATH/EventBusClient" \
    "$SRC_PATH/E470.AuditLog.EventBusClient" \
    "E470.AuditLog.EventBusClient"

# ============================================
# PHASE 6: Infrastructure Project
# ============================================
echo ""
echo -e "${CYAN}============================================${NC}"
echo -e "${CYAN}PHASE 6: Infrastructure Project Migration${NC}"
echo -e "${CYAN}============================================${NC}"

copy_project_with_rename \
    "$SRC_PATH/Infrastructure" \
    "$SRC_PATH/E470.AuditLog.Infrastructure" \
    "E470.AuditLog.Infrastructure"

# ============================================
# PHASE 7: Web.Api Project
# ============================================
echo ""
echo -e "${CYAN}============================================${NC}"
echo -e "${CYAN}PHASE 7: Web.Api Project Migration${NC}"
echo -e "${CYAN}============================================${NC}"

copy_project_with_rename \
    "$SRC_PATH/Web.Api" \
    "$SRC_PATH/E470.AuditLog.Web.Api" \
    "E470.AuditLog.Web.Api"

# ============================================
# PHASE 8: ArchitectureTests Project
# ============================================
echo ""
echo -e "${CYAN}============================================${NC}"
echo -e "${CYAN}PHASE 8: ArchitectureTests Project Migration${NC}"
echo -e "${CYAN}============================================${NC}"

copy_project_with_rename \
    "$TESTS_PATH/ArchitectureTests" \
    "$TESTS_PATH/E470.AuditLog.ArchitectureTests" \
    "E470.AuditLog.ArchitectureTests"

# ============================================
# Configuration Files Update
# ============================================
echo ""
echo -e "${CYAN}============================================${NC}"
echo -e "${CYAN}Configuration Files Update${NC}"
echo -e "${CYAN}============================================${NC}"

# Docker Compose files
if [ -f "$ROOT_PATH/docker/compose.webapi.yml" ]; then
    update_file_content "$ROOT_PATH/docker/compose.webapi.yml" "Docker Configuration"
fi

if [ -f "$ROOT_PATH/docker/compose.webapi.override.yml" ]; then
    update_file_content "$ROOT_PATH/docker/compose.webapi.override.yml" "Docker Configuration"
fi

# Dockerfile
DOCKERFILE_PATH="$SRC_PATH/E470.AuditLog.Web.Api/Dockerfile"
if [ -f "$DOCKERFILE_PATH" ]; then
    if [ "$DRY_RUN" = false ]; then
        sed -i 's|src/Web\.Api/|src/E470.AuditLog.Web.Api/|g' "$DOCKERFILE_PATH"
        sed -i 's|src/Infrastructure/|src/E470.AuditLog.Infrastructure/|g' "$DOCKERFILE_PATH"
        sed -i 's|src/Application/|src/E470.AuditLog.Application/|g' "$DOCKERFILE_PATH"
        sed -i 's|src/Domain/|src/E470.AuditLog.Domain/|g' "$DOCKERFILE_PATH"
        sed -i 's|src/EventBusClient/|src/E470.AuditLog.EventBusClient/|g' "$DOCKERFILE_PATH"
        sed -i 's|src/SharedKernel/|src/E470.AuditLog.SharedKernel/|g' "$DOCKERFILE_PATH"
        sed -i 's|src/AuditLog\.ServiceDefaults/|src/E470.AuditLog.ServiceDefaults/|g' "$DOCKERFILE_PATH"
        sed -i 's|Web\.Api\.csproj|E470.AuditLog.Web.Api.csproj|g' "$DOCKERFILE_PATH"
        sed -i 's|Infrastructure\.csproj|E470.AuditLog.Infrastructure.csproj|g' "$DOCKERFILE_PATH"
        sed -i 's|Application\.csproj|E470.AuditLog.Application.csproj|g' "$DOCKERFILE_PATH"
        sed -i 's|Domain\.csproj|E470.AuditLog.Domain.csproj|g' "$DOCKERFILE_PATH"
        sed -i 's|EventBusClient\.csproj|E470.AuditLog.EventBusClient.csproj|g' "$DOCKERFILE_PATH"
        sed -i 's|SharedKernel\.csproj|E470.AuditLog.SharedKernel.csproj|g' "$DOCKERFILE_PATH"
        sed -i 's|Web\.Api\.dll|E470.AuditLog.Web.Api.dll|g' "$DOCKERFILE_PATH"
        echo -e "  ${GREEN}✓${NC} Dockerfile updated"
    else
        echo -e "  ${YELLOW}[DRY RUN]${NC} Would update Dockerfile"
    fi
fi

# ============================================
# Summary
# ============================================
echo ""
echo -e "${CYAN}===============================================${NC}"
echo -e "${GREEN}Migration Complete!${NC}"
echo -e "${CYAN}===============================================${NC}"

if [ "$DRY_RUN" = true ]; then
    echo ""
    echo -e "${YELLOW}[DRY RUN] No actual changes were made.${NC}"
    echo -e "${YELLOW}Run without --dry-run parameter to apply changes.${NC}"
else
    echo ""
    echo -e "${YELLOW}Next Steps:${NC}"
    echo "1. Update solution file (E470.AuditLog.sln)"
    echo "2. Delete old project folders"
    echo "3. Build solution: dotnet build E470.AuditLog.sln"
    echo "4. Run tests: dotnet test E470.AuditLog.sln"
    echo "5. Commit changes: git add . && git commit -m 'Complete E470 prefix migration'"
fi

echo ""
echo -e "${CYAN}For detailed migration plan, see MIGRATION_PLAN_E470.md${NC}"
echo ""
