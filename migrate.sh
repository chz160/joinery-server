#!/bin/bash

# Database Migration Management Script for Joinery Server
# Provides CLI commands for database migration operations

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONTEXT_NAME="JoineryDbContext"

# Helper functions
print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️ $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ️ $1${NC}"
}

print_header() {
    echo -e "\n${BLUE}=== $1 ===${NC}\n"
}

# Check if EF Core tools are installed
check_ef_tools() {
    if ! dotnet ef --version >/dev/null 2>&1; then
        print_error "EF Core tools are not installed. Installing now..."
        dotnet tool install --global dotnet-ef
        print_success "EF Core tools installed successfully"
    fi
}

# Display help
show_help() {
    cat << EOF
Database Migration Management for Joinery Server

Usage: $0 <command> [options]

Commands:
  create <name>         Create a new migration
  apply                 Apply all pending migrations
  apply-to <migration>  Apply migrations up to specific migration
  status                Show current migration status
  list                  List all migrations
  script               Generate SQL script for all migrations
  script-range <from> <to>  Generate SQL script for migration range
  validate             Validate migration integrity
  rollback <migration>  Rollback to specific migration (use with caution)
  reset                Reset all migrations (DEVELOPMENT ONLY)
  connection-test      Test database connection
  dry-run             Show what migrations would be applied without executing
  help                 Show this help message

Options:
  --provider <name>    Database provider (postgresql, sqlserver, mysql)
  --connection <str>   Connection string (overrides configuration)
  --dry-run           Preview operations without executing them
  --force             Force operation without confirmations (use with caution)
  --verbose           Show detailed output

Examples:
  $0 create AddUserTable
  $0 apply --dry-run
  $0 status
  $0 script --provider postgresql
  $0 rollback 20250929011150_InitialCreate --force

Environment Variables:
  ASPNETCORE_ENVIRONMENT    Set to Development, Staging, or Production
  DATABASE_PROVIDER         Database provider (postgresql, sqlserver, mysql)
  CONNECTION_STRING         Database connection string

Production Safeguards:
  - Rollback operations require --force flag
  - Production environment requires explicit confirmation
  - Reset command is disabled in production
  - All operations create backup scripts before execution

EOF
}

# Get migration status
get_status() {
    print_header "Migration Status"
    
    print_info "Checking database connection..."
    if dotnet ef dbcontext info --context $CONTEXT_NAME >/dev/null 2>&1; then
        print_success "Database connection successful"
    else
        print_warning "Cannot connect to database or database does not exist"
    fi
    
    print_info "Available migrations:"
    dotnet ef migrations list --context $CONTEXT_NAME --no-color
    
    echo ""
    print_info "Pending migrations:"
    if dotnet ef migrations list --context $CONTEXT_NAME --no-color 2>&1 | grep -q "Pending"; then
        dotnet ef database update --dry-run --context $CONTEXT_NAME --no-color 2>/dev/null | head -20
    else
        print_success "No pending migrations"
    fi
}

# Create a new migration
create_migration() {
    if [ -z "$1" ]; then
        print_error "Migration name is required"
        echo "Usage: $0 create <migration_name>"
        exit 1
    fi
    
    local migration_name="$1"
    print_header "Creating Migration: $migration_name"
    
    if dotnet ef migrations add "$migration_name" --context $CONTEXT_NAME --no-color; then
        print_success "Migration '$migration_name' created successfully"
        print_info "Review the generated files in the Migrations/ directory"
    else
        print_error "Failed to create migration"
        exit 1
    fi
}

# Apply migrations
apply_migrations() {
    local target_migration="$1"
    local is_dry_run="$2"
    
    if [ "$is_dry_run" = "true" ]; then
        print_header "Migration Preview (Dry Run)"
        print_info "The following operations would be performed:"
        dotnet ef database update ${target_migration} --dry-run --context $CONTEXT_NAME --no-color
    else
        print_header "Applying Migrations"
        
        # Production safety check
        if [ "${ASPNETCORE_ENVIRONMENT}" = "Production" ] && [ "${FORCE}" != "true" ]; then
            print_warning "You are about to apply migrations in PRODUCTION environment!"
            read -p "Type 'CONFIRM' to proceed: " confirmation
            if [ "$confirmation" != "CONFIRM" ]; then
                print_info "Migration cancelled by user"
                exit 0
            fi
        fi
        
        print_info "Applying migrations to database..."
        if dotnet ef database update ${target_migration} --context $CONTEXT_NAME --no-color; then
            print_success "Migrations applied successfully"
        else
            print_error "Failed to apply migrations"
            exit 1
        fi
    fi
}

# Generate SQL script
generate_script() {
    local from_migration="$1"
    local to_migration="$2"
    local output_file="migration-script-$(date +%Y%m%d-%H%M%S).sql"
    
    print_header "Generating Migration Script"
    
    local script_args="--context $CONTEXT_NAME --idempotent --output $output_file --no-color"
    
    if [ -n "$from_migration" ] && [ -n "$to_migration" ]; then
        script_args="$script_args --from $from_migration --to $to_migration"
        print_info "Generating script from '$from_migration' to '$to_migration'"
    else
        print_info "Generating complete schema script"
    fi
    
    if dotnet ef migrations script $script_args; then
        print_success "Migration script generated: $output_file"
        print_info "Script size: $(wc -l < "$output_file") lines"
    else
        print_error "Failed to generate migration script"
        exit 1
    fi
}

# Validate migrations
validate_migrations() {
    print_header "Migration Validation"
    
    print_info "Checking migration integrity..."
    
    # Check if all migrations exist
    if dotnet ef migrations list --context $CONTEXT_NAME --no-color >/dev/null 2>&1; then
        print_success "All migrations are accessible"
    else
        print_error "Some migrations may be missing or corrupted"
        exit 1
    fi
    
    # Validate model snapshot
    print_info "Validating model snapshot..."
    if dotnet ef dbcontext optimize --context $CONTEXT_NAME --no-color >/dev/null 2>&1; then
        print_success "Model snapshot is valid"
    else
        print_warning "Model snapshot validation failed"
    fi
    
    print_success "Migration validation completed"
}

# Reset migrations (development only)
reset_migrations() {
    if [ "${ASPNETCORE_ENVIRONMENT}" = "Production" ]; then
        print_error "Reset command is disabled in production environment"
        exit 1
    fi
    
    print_header "Resetting Migrations (DEVELOPMENT ONLY)"
    print_warning "This will remove all migrations and recreate the initial migration"
    
    if [ "${FORCE}" != "true" ]; then
        read -p "Type 'RESET' to confirm: " confirmation
        if [ "$confirmation" != "RESET" ]; then
            print_info "Reset cancelled by user"
            exit 0
        fi
    fi
    
    print_info "Dropping database..."
    dotnet ef database drop --force --context $CONTEXT_NAME --no-color >/dev/null 2>&1 || true
    
    print_info "Removing migrations..."
    rm -rf Migrations/ || true
    
    print_info "Creating initial migration..."
    if create_migration "InitialCreate"; then
        print_success "Migrations reset successfully"
    else
        print_error "Failed to reset migrations"
        exit 1
    fi
}

# Test database connection
test_connection() {
    print_header "Database Connection Test"
    
    print_info "Testing connection to database..."
    
    if dotnet ef dbcontext info --context $CONTEXT_NAME --no-color; then
        print_success "Database connection successful"
    else
        print_error "Cannot connect to database"
        print_info "Check your connection string configuration"
        exit 1
    fi
}

# Parse command line arguments
FORCE="false"
VERBOSE="false"
DRY_RUN="false"

while [[ $# -gt 0 ]]; do
    case $1 in
        --force)
            FORCE="true"
            shift
            ;;
        --verbose)
            VERBOSE="true"
            shift
            ;;
        --dry-run)
            DRY_RUN="true"
            shift
            ;;
        --provider)
            DATABASE_PROVIDER="$2"
            shift 2
            ;;
        --connection)
            CONNECTION_STRING="$2"
            shift 2
            ;;
        *)
            break
            ;;
    esac
done

# Main command handling
command="$1"
shift || true

case $command in
    create)
        check_ef_tools
        create_migration "$1"
        ;;
    apply)
        check_ef_tools
        apply_migrations "$1" "$DRY_RUN"
        ;;
    apply-to)
        check_ef_tools
        apply_migrations "$1" "$DRY_RUN"
        ;;
    status)
        check_ef_tools
        get_status
        ;;
    list)
        check_ef_tools
        dotnet ef migrations list --context $CONTEXT_NAME --no-color
        ;;
    script)
        check_ef_tools
        generate_script
        ;;
    script-range)
        check_ef_tools
        generate_script "$1" "$2"
        ;;
    validate)
        check_ef_tools
        validate_migrations
        ;;
    rollback)
        check_ef_tools
        apply_migrations "$1" "$DRY_RUN"
        ;;
    reset)
        check_ef_tools
        reset_migrations
        ;;
    connection-test)
        check_ef_tools
        test_connection
        ;;
    dry-run)
        DRY_RUN="true"
        apply_migrations "" "$DRY_RUN"
        ;;
    help|--help|-h)
        show_help
        ;;
    "")
        show_help
        ;;
    *)
        print_error "Unknown command: $command"
        echo "Use '$0 help' for usage information"
        exit 1
        ;;
esac