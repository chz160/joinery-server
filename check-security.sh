#!/bin/bash
# Security check script for Joinery Server
# Run before committing to ensure no sensitive data is included

set -e

echo "🔍 Running security checks for Joinery Server..."

# Check for staged sensitive files
echo "Checking for sensitive files in git staging..."
sensitive_files=(
    "appsettings.*.local.json"
    "appsettings.Production.json"
    ".env"
    "*.env"
    "secrets.json"
    "*.key"
    "*.pem"
)

for pattern in "${sensitive_files[@]}"; do
    if git diff --cached --name-only | grep -E "$pattern" >/dev/null 2>&1; then
        echo "❌ ERROR: Sensitive file detected in staging: $pattern"
        echo "   Use template files or *.local.json files instead"
        exit 1
    fi
done

# Check for real credentials in template files
echo "Checking template files for real credentials..."
for template_file in $(git diff --cached --name-only | grep -E "(appsettings\.(Development|json)|\.example)$"); do
    if git diff --cached -- "$template_file" | grep -E "\+.*[\"'](?!your-|test-|example-|placeholder-|demo-|dev-)[A-Za-z0-9\-_]{20,}[\"']" >/dev/null 2>&1; then
        echo "❌ ERROR: Potential real credential detected in template file: $template_file"
        echo "   Template files should only contain placeholder values"
        git diff --cached -- "$template_file" | grep -E "\+.*[\"'][A-Za-z0-9\-_]{20,}[\"']" | head -3
        exit 1
    fi
done

# Check for common secret patterns in staged changes
echo "Checking for secret patterns in staged changes..."
if git diff --cached | grep -iE "(ClientSecret|SecretKey|ApiKey|password).*[\"'](?!your-|test-|example-)[A-Za-z0-9!@#$%^&*]{8,}[\"']" >/dev/null 2>&1; then
    echo "❌ ERROR: Potential secret pattern detected:"
    git diff --cached | grep -iE "(ClientSecret|SecretKey|ApiKey|password).*[\"'](?!your-|test-|example-)[A-Za-z0-9!@#$%^&*]{8,}[\"']" | head -3
    echo "   Please use placeholder values like 'your-client-id'"
    exit 1
fi

# Verify that our sensitive files are in .gitignore
echo "Verifying .gitignore coverage..."
if ! grep -q "\.local\.json" .gitignore; then
    echo "❌ ERROR: *.local.json files not in .gitignore"
    exit 1
fi

if ! grep -q "\.env" .gitignore; then
    echo "❌ ERROR: .env files not in .gitignore"
    exit 1
fi

echo "✅ All security checks passed!"
echo "📝 Remember:"
echo "   - Keep template files with placeholders in git"
echo "   - Use *.local.json files for real local credentials"
echo "   - Use environment variables for production"
echo "   - Never commit files with real secrets"