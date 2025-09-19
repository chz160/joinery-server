# Joinery Server

Joinery Server is a .NET 8 ASP.NET Core Web API that provides database query sharing with OAuth authentication (GitHub and Microsoft Entra ID). It includes JWT token-based security, Git repository integration for SQL query management, and organization/team management with role-based access control.

**ALWAYS reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.**

## Working Effectively

### Prerequisites
- .NET 8.0 SDK (already installed: version 8.0.119)
- No additional SDKs or tools required - all dependencies managed via NuGet

### Build, Test and Run
- **Bootstrap and build:**
  - `cd /home/runner/work/joinery-server/joinery-server`
  - `dotnet restore` - restores NuGet packages (~10 seconds)
  - `dotnet build` - builds the solution (~10 seconds)
- **Test:**
  - `dotnet test` - no tests exist, completes immediately
- **Run the application:**
  - `dotnet run` - starts on http://localhost:5256 (HTTPS port 7035 may not be available in all environments)
  - **NEVER CANCEL** - Application starts in ~5 seconds but may take longer during first run
  - Access Swagger UI at: http://localhost:5256/swagger (loads fully functional API documentation)
  - Health check: http://localhost:5256/api/health
  - Readiness check: http://localhost:5256/api/health/ready

### Code Quality and Formatting
- **CRITICAL:** Always run formatting before committing:
  - `dotnet format JoineryServer.sln` - fixes code formatting issues
  - `dotnet format JoineryServer.sln --verify-no-changes` - verifies formatting (~13 seconds)
  - **NEVER CANCEL** - Format verification takes 13+ seconds, set timeout to 30+ minutes
  - The codebase currently has formatting issues that MUST be fixed before any commit

### Production Build
- `dotnet publish JoineryServer.csproj -c Release -o out` - creates production build (~5 seconds)
- **Note:** Use project file instead of solution file to avoid MSBuild warnings

## Validation

### Manual Validation Scenarios
After making ANY changes, ALWAYS validate these scenarios:

1. **Basic Application Health:**
   - Start application: `dotnet run`
   - Verify health endpoint returns JSON: `curl http://localhost:5256/api/health`
   - Verify Swagger UI loads: open http://localhost:5256/swagger in browser
   - Stop application with Ctrl+C

2. **Authentication Flow Validation (when auth code changes):**
   - Configure OAuth apps in GitHub/Microsoft (see README.md setup instructions)
   - Update `appsettings.Development.json` with valid credentials
   - Test GitHub login: navigate to `/api/auth/login/github`
   - Test Microsoft login: navigate to `/api/auth/login/microsoft`
   - Verify JWT tokens are generated correctly

3. **API Endpoint Validation:**
   - Test protected endpoints return 401 without authentication: `curl -w "%{http_code}" http://localhost:5256/api/Organizations`
   - Verify Swagger API schema is accessible: `curl http://localhost:5256/swagger/v1/swagger.json`
   - Test organization and team management endpoints
   - Test Git repository integration endpoints
   - Validate query management functionality

### Configuration Requirements
- **Development:** Uses `appsettings.Development.json`
- **JWT tokens expire in 1 hour** (development) vs 24 hours (production)
- **In-memory database** - data resets on each application restart
- **OAuth setup required** for authentication testing (see README.md)

## Common Tasks

### Repository Structure
```
/home/runner/work/joinery-server/joinery-server/
├── Controllers/           # API controllers (Auth, Health, Organizations, Teams, etc.)
├── Data/                 # Entity Framework DbContext
├── Models/               # Entity models (User, Organization, Team, etc.)
├── Services/             # Business logic (GitRepositoryService, TeamPermissionService)
├── Properties/           # Launch settings
├── appsettings.json      # Production configuration
├── appsettings.Development.json  # Development configuration
├── JoineryServer.csproj  # Project file
├── JoineryServer.sln     # Solution file
├── Program.cs            # Application startup and configuration
└── README.md            # Setup and usage documentation
```

### Key Files to Monitor
- **Program.cs** - Application configuration, authentication setup, Swagger configuration
- **appsettings.Development.json** - Development configuration including OAuth credentials
- **Controllers/** - API endpoint definitions
- **Services/GitRepositoryService.cs** - Git repository integration logic
- **Services/TeamPermissionService.cs** - Authorization and permissions

### Common Patterns
- **Authentication:** All API endpoints except health checks require JWT Bearer tokens
- **Authorization:** Role-based access control via Organization/Team membership
- **Error Handling:** Controllers return appropriate HTTP status codes with error details
- **Configuration:** Uses ASP.NET Core configuration system with environment-specific files

### Troubleshooting
- **Build fails:** Run `dotnet restore` first, then `dotnet build`
- **Formatting errors:** Run `dotnet format JoineryServer.sln` before committing
- **OAuth errors:** Verify credentials in `appsettings.Development.json` match OAuth app settings
- **Database errors:** Application uses in-memory database - restart application to reset data
- **Port conflicts:** Application uses ports 5256 (HTTP) and 7035 (HTTPS) - ensure they're available

### Development Workflow
1. **Make code changes**
2. **Build and test:** `dotnet build` (never skip this step)
3. **Format code:** `dotnet format JoineryServer.sln`
4. **Validate manually:** Start application and test affected functionality
5. **Verify formatting:** `dotnet format JoineryServer.sln --verify-no-changes`
6. **Production build test:** `dotnet publish JoineryServer.csproj -c Release -o out` (optional but recommended)

### Dependencies and Packages
- **ASP.NET Core 8.0** - Web framework
- **Entity Framework Core In-Memory** - Database provider
- **Microsoft.Identity.Web** - Microsoft authentication
- **AspNet.Security.OAuth.GitHub** - GitHub authentication  
- **Swashbuckle.AspNetCore** - Swagger/OpenAPI documentation
- **JWT Bearer authentication** - Token-based security

### Performance and Timing
- **Restore:** ~10 seconds
- **Build:** ~5 seconds  
- **Format check:** ~9 seconds - NEVER CANCEL, set timeout to 30+ minutes
- **Publish:** ~5 seconds
- **Application startup:** ~5 seconds
- **Health check response:** <1 second

### Important Notes
- **No test suite exists** - manual validation is critical
- **Database is in-memory** - all data is lost on application restart
- **OAuth apps must be configured** for authentication testing
- **Swagger UI provides interactive API testing** at /swagger endpoint
- **Git integration** allows loading SQL queries from external repositories
- **Role-based permissions** control access to organizations, teams, and queries