#!/bin/bash
# Security check script for Joinery Server
# Run before committing to ensure no sensitive data is included

set -e

echo "🔍 Running security checks for Joinery Server..."

# Check for staged sensitive files
echo "Checking for sensitive files in git staging..."
sensitive_files=(
    "appsettings.Development.json"
    "appsettings.Production.json"
    "appsettings.*.json"
    ".env"
    "*.env"
    "secrets.json"
    "*.key"
    "*.pem"
)

for pattern in "${sensitive_files[@]}"; do
    if git diff --cached --name-only | grep -E "$pattern" | grep -v "\.example$" >/dev/null 2>&1; then
        echo "❌ ERROR: Sensitive file detected in staging: $pattern"
        echo "   Make sure you're using template files (.example) instead"
        exit 1
    fi
done

# Check for common secret patterns in staged changes
echo "Checking for secret patterns in staged changes..."
secret_patterns=(
    "ClientSecret.*[\"']?[A-Za-z0-9\-_]{20,}[\"']?"
    "SecretKey.*[\"']?[A-Za-z0-9\-_]{32,}[\"']?"
    "ApiKey.*[\"']?[A-Za-z0-9\-_]{20,}[\"']?"
    "password.*[\"']?(?!your-|test-|example-)[A-Za-z0-9!@#$%^&*]{8,}[\"']?"
)

for pattern in "${secret_patterns[@]}"; do
    if git diff --cached | grep -iE "$pattern" | grep -v "your-" | grep -v "example-" | grep -v "test-" >/dev/null 2>&1; then
        echo "❌ ERROR: Potential secret pattern detected:"
        git diff --cached | grep -iE "$pattern" | grep -v "your-" | grep -v "example-" | grep -v "test-" || true
        echo "   Please use placeholder values like 'your-client-id'"
        exit 1
    fi
done

# Check that template files exist for any new config files
echo "Checking for missing template files..."
for config_file in $(git diff --cached --name-only | grep -E "appsettings\..*\.json$"); do
    template_file="${config_file}.example"
    if [[ ! -f "$template_file" ]]; then
        echo "❌ ERROR: Missing template file for $config_file"
        echo "   Please create $template_file with placeholder values"
        exit 1
    fi
done

# Verify that our sensitive files are in .gitignore
echo "Verifying .gitignore coverage..."
if ! grep -q "appsettings.Development.json" .gitignore; then
    echo "❌ ERROR: appsettings.Development.json not in .gitignore"
    exit 1
fi

if ! grep -q "\.env" .gitignore; then
    echo "❌ ERROR: .env files not in .gitignore"
    exit 1
fi

echo "✅ All security checks passed!"
echo "📝 Remember:"
echo "   - Only commit template files (.example)"
echo "   - Use placeholder values in templates"
echo "   - Keep real credentials in local config files"
echo "   - Use environment variables for production"