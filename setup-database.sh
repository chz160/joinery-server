#!/bin/bash

# Joinery Server Database Setup Script
# This script helps set up different database providers for Joinery Server

set -e

echo "🗃️  Joinery Server Database Setup"
echo "================================"
echo ""

# Check if dotnet-ef tool is installed
if ! command -v dotnet-ef &> /dev/null; then
    echo "📦 Installing Entity Framework Core tools..."
    dotnet tool install --global dotnet-ef
    echo "✅ EF Core tools installed"
else
    echo "✅ EF Core tools already installed"
fi

echo ""
echo "Select database provider:"
echo "1) SQLite (recommended for development)"
echo "2) PostgreSQL (recommended for production)"
echo "3) SQL Server"
echo "4) MySQL"
echo ""

read -p "Enter your choice (1-4): " choice

case $choice in
    1)
        echo "📦 Installing SQLite provider..."
        dotnet add package Microsoft.EntityFrameworkCore.Sqlite
        PROVIDER="SQLite"
        CONNECTION_STRING="Data Source=joinery.db"
        ;;
    2)
        echo "📦 Installing PostgreSQL provider..."
        dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
        PROVIDER="PostgreSQL"
        CONNECTION_STRING="Host=localhost;Database=joinerydb;Username=joineryuser;Password=joinerypass"
        ;;
    3)
        echo "📦 Installing SQL Server provider..."
        dotnet add package Microsoft.EntityFrameworkCore.SqlServer
        PROVIDER="SQL Server"
        CONNECTION_STRING="Server=(localdb)\\mssqllocaldb;Database=JoineryDB;Trusted_Connection=true;MultipleActiveResultSets=true"
        ;;
    4)
        echo "📦 Installing MySQL provider..."
        dotnet add package Pomelo.EntityFrameworkCore.MySql
        PROVIDER="MySQL"
        CONNECTION_STRING="Server=localhost;Database=joinerydb;Uid=joineryuser;Pwd=joinerypass;"
        ;;
    *)
        echo "❌ Invalid choice. Exiting."
        exit 1
        ;;
esac

echo "✅ $PROVIDER provider installed"
echo ""

# Update appsettings.Development.json with connection string
echo "📝 Updating appsettings.Development.json..."
if [ -f "appsettings.Development.json" ]; then
    # Create backup
    cp appsettings.Development.json appsettings.Development.json.backup
    
    # Use jq to add connection string if available, otherwise show manual instructions
    if command -v jq &> /dev/null; then
        jq --arg conn "$CONNECTION_STRING" '.ConnectionStrings.DefaultConnection = $conn' appsettings.Development.json > appsettings.Development.json.tmp && mv appsettings.Development.json.tmp appsettings.Development.json
        echo "✅ Connection string added to appsettings.Development.json"
    else
        echo "⚠️  Please manually add the following to appsettings.Development.json:"
        echo ""
        echo '  "ConnectionStrings": {'
        echo "    \"DefaultConnection\": \"$CONNECTION_STRING\""
        echo '  },'
        echo ""
    fi
else
    echo "❌ appsettings.Development.json not found. Please create it manually."
fi

echo ""
echo "🔧 Creating initial migration..."
if dotnet ef migrations add InitialCreate --context JoineryDbContext; then
    echo "✅ Migration created successfully"
else
    echo "❌ Failed to create migration. Please check your database configuration."
    exit 1
fi

echo ""
echo "🚀 Applying migration to database..."
if dotnet ef database update --context JoineryDbContext; then
    echo "✅ Database schema created successfully"
else
    echo "❌ Failed to apply migration. Please check your database connection."
    exit 1
fi

echo ""
echo "🎉 Database setup complete!"
echo "📋 Summary:"
echo "   - Provider: $PROVIDER"
echo "   - Connection: $CONNECTION_STRING"
echo "   - Migration: InitialCreate applied"
echo ""
echo "💡 Next steps:"
echo "   1. Update Program.cs to use your selected database provider"
echo "   2. Run 'dotnet run' to start the application"
echo "   3. Visit https://localhost:7035/swagger to test the API"
echo ""
echo "📖 For detailed configuration, see DATABASE.md"