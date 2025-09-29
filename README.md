# Joinery Server

The server-side portion of Joinery - a platform for sharing and managing database queries. This ASP.NET Core Web API provides authentication via GitHub OAuth, Microsoft Entra ID, and AWS IAM, and offers read-only access to a shared repository of database queries.

## Recent Changes

### Enhanced Role Management
- **Organization Management**: Full CRUD operations for organizations with administrator and member roles
- **Team Management**: Complete team lifecycle management with role-based permissions
- **Advanced Permissions**: Granular permissions system for teams including query read/write and folder management

### Git Repository Integration
- **Git Repository Support**: Connect external Git repositories as query sources
- **Repository Synchronization**: Automatic syncing of SQL query files from Git repositories
- **Multi-scope Repositories**: Support for personal, team-level, and organization-level repository access

### Improved JWT Security
- **Enhanced Token Configuration**: Configurable JWT expiration, issuer, and audience settings
- **Secure Token Generation**: HMAC SHA256 signing with configurable secret keys
- **Environment-specific Configuration**: Different JWT settings for development and production

### Expanded API Documentation
- **Swagger Integration**: Comprehensive OpenAPI documentation with JWT authentication support
- **Interactive Testing**: Bearer token authentication directly in Swagger UI
- **Detailed Endpoint Documentation**: Complete API reference with request/response examples

### Health and Monitoring
- **Health Check Endpoints**: Service health monitoring with `/api/health` and `/api/health/ready`
- **Detailed Health Responses**: Comprehensive health status including service version and timestamps
- **Readiness Checks**: Separate readiness endpoint for deployment orchestration

## Features

- **Authentication**: GitHub OAuth, Microsoft Entra ID, and AWS IAM integration with JWT token-based security
- **Rate Limiting**: Comprehensive rate limiting with configurable limits per authentication level and endpoint category
- **Multi-source Query Access**: Support for both database queries and Git repository-based SQL files
- **Organization Management**: Create and manage organizations with role-based access control, AWS IAM, and Microsoft Entra ID integration
- **Team Management**: Complete team lifecycle with administrator permissions and member roles
- **AWS IAM Integration**: Import users from AWS IAM and enable authentication using AWS credentials
- **Microsoft Entra ID Integration**: Organization-scoped Entra ID authentication with user import and domain filtering
- **Git Repository Integration**: Connect and synchronize SQL queries from external Git repositories
- **Advanced Permission System**: Granular permissions for query access and folder management
- **RESTful API**: Clean, documented endpoints with comprehensive Swagger documentation
- **Health Monitoring**: Built-in health check and readiness endpoints for deployment monitoring
- **In-Memory Database**: Quick MVP setup with Entity Framework Core
- **CORS Configuration**: Environment-aware Cross-Origin Resource Sharing with production security

## Prerequisites

- .NET 8.0 SDK
- Visual Studio, Visual Studio Code, or any text editor
- GitHub OAuth App (for GitHub authentication)
- Microsoft Entra ID App Registration (for Microsoft authentication)
- AWS IAM credentials (for AWS authentication integration)
- Database server (optional - see [Database Setup Guide](DATABASE.md))

## Setup

### 1. Clone the Repository

```bash
git clone https://github.com/chz160/joinery-server.git
cd joinery-server
```

### 2. Configure Authentication

#### GitHub OAuth Setup

1. Go to GitHub Settings > Developer settings > OAuth Apps
2. Create a new OAuth App with:
   - Application name: `Joinery Server (Dev)`
   - Homepage URL: `https://localhost:7035`
   - Authorization callback URL: `https://localhost:7035/signin-github`
3. Copy the Client ID and Client Secret

#### Microsoft Entra ID Setup

1. Go to Azure Portal > Microsoft Entra ID > App registrations
2. Create a new registration with:
   - Name: `Joinery Server`
   - Redirect URI: `https://localhost:7035/signin-microsoft`
3. Copy the Application (client) ID, Directory (tenant) ID
4. Create a client secret in "Certificates & secrets"

### 2c. Configure AWS IAM (Optional)

For organizations that want to use AWS IAM integration:

1. Create an IAM user with programmatic access
2. Attach the following IAM policy (minimum required permissions):
```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "iam:ListUsers",
                "iam:GetUser",
                "iam:ListUserTags"
            ],
            "Resource": "*"
        }
    ]
}
```
3. For cross-account access, create a role with the above policy and note the Role ARN
4. Organizations can configure AWS IAM integration through the API after creating their organization

### 2d. Configure Microsoft Entra ID for Organizations (Optional)

For organizations that want to use Microsoft Entra ID integration:

1. **Create a new App Registration** (separate from the global Microsoft OAuth app):
   - Go to Azure Portal > Microsoft Entra ID > App registrations
   - Create a new registration with:
     - Name: `Joinery Server - [Organization Name]`
     - Supported account types: Accounts in this organizational directory only
     - No redirect URI needed for this app registration

2. **Configure API Permissions**:
   - Add Microsoft Graph API permissions:
     - `User.Read.All` (Application permission)
     - `Organization.Read.All` (Application permission)  
     - `Domain.Read.All` (Application permission)
   - Grant admin consent for the permissions

3. **Create Client Secret**:
   - Go to "Certificates & secrets"
   - Create a new client secret
   - Copy the secret value (you won't be able to see it again)

4. **Note Required Information**:
   - **Tenant ID**: Found in the app registration overview
   - **Client ID**: Application (client) ID from the app registration
   - **Client Secret**: The secret you just created
   - **Domain** (optional): Your organization's domain (e.g., "contoso.com") for user filtering

5. **Organization Configuration**:
   Organizations can configure Entra ID integration through the API after creating their organization.

### 3. Configure Secrets (IMPORTANT SECURITY STEP)

> ⚠️ **CRITICAL SECURITY WARNING**: The repository includes template configuration files with placeholder values. NEVER replace these placeholders with real credentials in tracked files!

#### Development Configuration (Recommended)

The provided configuration files (`appsettings.Development.json`, `appsettings.json`) contain safe placeholder values and should remain as templates:

1. **For local development with real credentials**, create local override files:
   ```bash
   # Create local files that are ignored by git
   cp appsettings.Development.json appsettings.Development.local.json
   ```

2. **Edit your local file** (`appsettings.Development.local.json`) with real credentials
3. **Configure ASP.NET Core** to use local files by adding this to your project:
   ```json
   // ASP.NET Core will automatically load appsettings.Development.local.json in Development environment
   ```

#### Alternative: Environment Variables (Recommended for Production)

1. **Copy the environment template**:
   ```bash
   cp .env.example .env
   ```

2. **Update `.env` file** with your real values (the application will automatically load these)

3. **Configure your deployment environment** to use these variables

### 4. Update Configuration

The application includes template configuration files with placeholder values:

```json
{
  "Authentication": {
    "GitHub": {
      "ClientId": "your-github-client-id",
      "ClientSecret": "your-github-client-secret"
    },
    "Microsoft": {
      "TenantId": "your-tenant-id",
      "ClientId": "your-microsoft-client-id",
      "ClientSecret": "your-microsoft-client-secret"
    }
  }
}
```

### 5. Database Setup (Optional)

By default, the application uses an **in-memory database** for development - no additional setup required! The database is automatically created and seeded with sample data when you run the application.

For persistent database storage or production deployment, you have several options:

#### Option 1: Quick Setup Script (Recommended)
```bash
# Run the interactive database setup script
chmod +x setup-database.sh
./setup-database.sh
```

#### Option 2: Manual Setup
For detailed configuration, see the comprehensive [Database Setup Guide](DATABASE.md) which covers:

- **Supported Database Providers**: PostgreSQL, SQL Server, SQLite, MySQL
- **Entity Framework Migrations**: Schema creation and deployment
- **Connection String Examples**: Development and production configurations
- **Production Database Setup**: Step-by-step deployment instructions
- **Seed Data Management**: Initial data and sample queries

#### Option 3: SQLite Quick Start (Persistent Development Database)
```bash
# Add SQLite provider
dotnet add package Microsoft.EntityFrameworkCore.Sqlite

# Create initial migration
dotnet ef migrations add InitialCreate

# Apply migration to create database
dotnet ef database update
```

### 6. Run the Application

```bash
dotnet restore
dotnet build
dotnet run
```

The application will start at:
- HTTPS: `https://localhost:7035`
- HTTP: `http://localhost:5256`

## API Endpoints

### Authentication

- `GET /api/auth/login/github` - Initiate GitHub OAuth login
- `GET /api/auth/login/microsoft` - Initiate Microsoft OAuth login
- `POST /api/auth/login/aws` - Authenticate with AWS IAM credentials
- `POST /api/auth/login/entra-id` - Authenticate with Entra ID credentials for organization
- `GET /api/auth/callback/github` - GitHub OAuth callback
- `GET /api/auth/callback/microsoft` - Microsoft OAuth callback

### Database Queries (Authenticated)

- `GET /api/queries` - Get all database queries (both traditional and Git-based)
- `GET /api/queries/{id}` - Get specific query by ID
- `GET /api/queries/search?searchTerm={term}` - Search queries by name/description/tags
- `GET /api/queries/by-database/{databaseType}` - Filter by database type
- `GET /api/queries/by-tag/{tag}` - Filter by tag

### Git Repositories (Authenticated)

- `GET /api/gitrepositories` - Get all Git repositories accessible to current user
- `GET /api/gitrepositories/{id}` - Get specific Git repository details with query files
- `POST /api/gitrepositories` - Create new Git repository configuration
- `POST /api/gitrepositories/{id}/sync` - Synchronize Git repository to update query files
- `GET /api/gitrepositories/{id}/folders` - Get folder structure from Git repository

### Teams (Authenticated)

- `GET /api/teams` - Get all teams for current user
- `POST /api/teams` - Create a new team
- `GET /api/teams/{id}` - Get team details with members
- `PUT /api/teams/{id}` - Update team (admins only)
- `DELETE /api/teams/{id}` - Delete team (creator only)
- `POST /api/teams/{id}/members` - Add member to team (admins only)
- `DELETE /api/teams/{id}/members/{userId}` - Remove member from team (admins only, or self)
- `PUT /api/teams/{id}/members/{userId}/role` - Update member role (admins only)

### Organizations (Authenticated)

- `GET /api/organizations` - Get all organizations for current user
- `POST /api/organizations` - Create a new organization
- `GET /api/organizations/{id}` - Get organization details with members and teams
- `PUT /api/organizations/{id}` - Update organization (admins only)
- `DELETE /api/organizations/{id}` - Delete organization (creator only)
- `POST /api/organizations/{id}/members` - Add member to organization (admins only)
- `DELETE /api/organizations/{id}/members/{userId}` - Remove member from organization (admins only, or self)
- `PUT /api/organizations/{id}/members/{userId}/role` - Update member role (admins only)

### AWS IAM Integration (Authenticated)

- `GET /api/organizations/{id}/aws-iam/config` - Get AWS IAM configuration for organization
- `POST /api/organizations/{id}/aws-iam/config` - Configure AWS IAM for organization (admins only)
- `DELETE /api/organizations/{id}/aws-iam/config` - Remove AWS IAM configuration (admins only)
- `POST /api/organizations/{id}/aws-iam/import-users` - Import users from AWS IAM (admins only)

### Microsoft Entra ID Integration (Authenticated)

- `GET /api/organizations/{id}/entra-id/config` - Get Entra ID configuration for organization
- `POST /api/organizations/{id}/entra-id/config` - Configure Entra ID for organization (admins only)
- `DELETE /api/organizations/{id}/entra-id/config` - Remove Entra ID configuration (admins only)  
- `POST /api/organizations/{id}/entra-id/import-users` - Import users from Entra ID (admins only)

### Health Checks

- `GET /api/health` - Service health check with version and timestamp
- `GET /api/health/ready` - Service readiness check for deployment orchestration

## Rate Limiting

The API implements comprehensive rate limiting to protect against abuse and ensure fair usage.

### Rate Limit Headers

All API responses include standard rate limiting headers:

- `X-RateLimit-Limit`: Maximum requests allowed in the current window
- `X-RateLimit-Remaining`: Number of requests remaining in the current window  
- `X-RateLimit-Reset`: Unix timestamp when the rate limit resets
- `X-RateLimit-Policy`: Applied policy level (Anonymous, Authenticated, Admin)
- `Retry-After`: Seconds to wait before retrying (included on 429 responses)

### Rate Limit Tiers

**Anonymous Users (IP-based)**:
- Development: 10 requests/minute, 500 requests/hour
- Production: 60 requests/minute, 1000 requests/hour

**Authenticated Users (User ID-based)**:
- Development: 30 requests/minute, 2000 requests/hour
- Production: 120 requests/minute, 5000 requests/hour

**Admin Users**:
- Unlimited (rate limits bypassed)

### Endpoint-Specific Limits

**Authentication Endpoints** (`/api/auth/*`):
- Anonymous: 5 requests/minute (dev), 10 requests/minute (prod)
- Authenticated: 10 requests/minute (dev), 20 requests/minute (prod)

**Health Endpoints** (`/api/health/*`):
- No rate limiting (always accessible for monitoring)

### Rate Limit Exceeded Response

When rate limits are exceeded, the API returns HTTP 429 with details:

```json
{
  "error": "rate_limit_exceeded",
  "message": "Rate limit exceeded. Too many requests.",
  "details": {
    "limit": 10,
    "remaining": 0,
    "reset_time": "2025-01-15T10:30:00Z",
    "retry_after_seconds": 45,
    "client_id": "ip:192.168.1.100",
    "endpoint": "/api/organizations",
    "auth_level": "Anonymous"
  }
}
```

### Configuration

Rate limiting is configured in `appsettings.json`:

```json
{
  "RateLimit": {
    "EnableDistributedCache": false,
    "Redis": {
      "ConnectionString": "localhost:6379"
    },
    "Global": {
      "Anonymous": {
        "RequestsPerMinute": 60,
        "RequestsPerHour": 1000,
        "RequestsPerDay": 10000
      },
      "Authenticated": {
        "RequestsPerMinute": 120,
        "RequestsPerHour": 5000,
        "RequestsPerDay": 50000
      }
    }
  }
}
```

## Usage

### 1. Access Swagger UI
Navigate to `/swagger` endpoint (e.g., `https://localhost:7035/swagger`) for interactive API documentation with JWT authentication support.

### 2. Authenticate  
Use one of the login endpoints to authenticate via GitHub, Microsoft, AWS IAM, or Entra ID:
- GitHub: `GET /api/auth/login/github`
- Microsoft: `GET /api/auth/login/microsoft`
- AWS IAM: `POST /api/auth/login/aws` (requires organization setup)
- Entra ID: `POST /api/auth/login/entra-id` (requires organization setup)

For AWS IAM authentication, send a JSON request:
```json
{
  "username": "your-aws-username",
  "organizationName": "your-organization"
}
```

For Entra ID authentication, send a JSON request:
```json
{
  "userPrincipalName": "user@yourdomain.com",
  "organizationName": "your-organization"
}
```

### 3. Get JWT Token
After successful authentication, you'll receive a JWT token in the callback response.

### 4. Access Protected Endpoints
Include the JWT token in the Authorization header for all authenticated requests:
```
Authorization: Bearer your-jwt-token
```

### 5. Organization and Team Management
- Create organizations and invite members with different roles (Member, Administrator)
- Create teams within organizations with granular permissions
- Administrators can manage members, roles, and team permissions
- Support for role-based access control across all resources

### 6. AWS IAM Integration
Organization administrators can configure AWS IAM integration to:
- Import existing AWS IAM users as organization members
- Allow AWS IAM users to authenticate using their AWS credentials
- Automatically synchronize user information from AWS IAM

**To configure AWS IAM for an organization:**
1. Ensure you have AWS IAM credentials with ListUsers, GetUser, and ListUserTags permissions
2. Use `POST /api/organizations/{id}/aws-iam/config` with AWS credentials
3. Import users with `POST /api/organizations/{id}/aws-iam/import-users`
4. Users can then authenticate using `POST /api/auth/login/aws`

### 7. Microsoft Entra ID Integration
Organization administrators can configure Microsoft Entra ID integration to:
- Import existing Entra ID users as organization members
- Allow Entra ID users to authenticate using their organizational credentials
- Filter users by domain for multi-tenant scenarios
- Automatically synchronize user information from Entra ID

**To configure Entra ID for an organization:**
1. Create an Entra ID app registration with required Microsoft Graph permissions
2. Use `POST /api/organizations/{id}/entra-id/config` with tenant ID, client ID, and client secret
3. Optionally specify a domain filter to restrict users to a specific domain
4. Import users with `POST /api/organizations/{id}/entra-id/import-users`
5. Users can then authenticate using `POST /api/auth/login/entra-id`

**Note:** Organizations can only configure one authentication method (AWS IAM or Entra ID) at a time. To switch methods, first remove the existing configuration.

### 8. Git Repository Integration
- Connect external Git repositories containing SQL query files
- Support for personal, team-level, and organization-level repository access
- Automatic synchronization of query files from connected repositories
- Repository folder structure browsing and management

## Sample Queries

The application comes pre-seeded with sample database queries for development:

### Database Queries
1. **Sample User Query**: Basic user retrieval query demonstrating database access patterns
2. **User Count by Registration Date**: Analytics query for user registrations with date grouping

### Git Repository Integration
- Connect external Git repositories containing SQL query files
- Automatically discover and synchronize `.sql` files from repository folders
- Support for query metadata including database type, tags, and descriptions
- Git commit history tracking for query file changes

### Query Sources
- **Traditional Database Queries**: Stored directly in the application database
- **Git Repository Queries**: Synchronized from external Git repositories
- **Multi-database Support**: Queries can target different database types (PostgreSQL, MySQL, SQL Server, etc.)
- **Tagging System**: Organize queries with custom tags for easy discovery

## Architecture

### Technology Stack
- **ASP.NET Core 8.0**: Modern web framework with built-in dependency injection
- **Entity Framework Core**: ORM with In-Memory database for rapid MVP development
- **JWT Authentication**: Stateless token-based security with configurable parameters
- **OAuth 2.0 Integration**: GitHub and Microsoft Entra ID authentication providers
- **Swagger/OpenAPI 3.0**: Interactive API documentation with authentication support

### Key Components
- **Controllers**: RESTful API endpoints for authentication, queries, teams, organizations, and Git repositories
- **Services**: Business logic layer including Git repository service and team permission service  
- **Data Models**: Entity models for users, teams, organizations, queries, and Git repositories
- **Authentication**: Multi-provider OAuth with JWT token generation and validation
- **Health Checks**: Built-in monitoring endpoints for deployment orchestration

### Data Architecture
- **Flexible Database Support**: Entity Framework Core with multiple provider support (see [Database Setup Guide](DATABASE.md))
- **Development**: In-Memory database with seeded sample data for rapid development
- **Production**: PostgreSQL, SQL Server, SQLite, or MySQL with full migration support
- **Multi-source Queries**: Support for both database queries and Git repository-based SQL files
- **Role-based Access**: Hierarchical permissions through organizations and teams
- **Git Integration**: External repository synchronization with automatic query file discovery

## Development

### Running Tests

```bash
dotnet test
```

### Building for Production

```bash
dotnet publish -c Release -o out
```

### Configuration

Key configuration sections in `appsettings.json`:

#### Authentication Settings
- `Authentication:GitHub`: GitHub OAuth configuration
  - `ClientId`: GitHub OAuth application client ID
  - `ClientSecret`: GitHub OAuth application client secret
- `Authentication:Microsoft`: Microsoft Entra ID configuration  
  - `Instance`: Microsoft identity platform endpoint
  - `TenantId`: Azure AD tenant identifier
  - `ClientId`: Application (client) ID from Azure portal
  - `ClientSecret`: Application client secret
  - `CallbackPath`: OAuth callback endpoint path

#### JWT Token Configuration
- `JWT:SecretKey`: Secret key for token signing (minimum 256 bits)
- `JWT:Issuer`: Token issuer identifier
- `JWT:Audience`: Token audience identifier  
- `JWT:ExpirationHours`: Token expiration time in hours

#### Logging Configuration
- `Logging:LogLevel`: Application logging levels by namespace

#### Example Configuration
```json
{
  "Authentication": {
    "GitHub": {
      "ClientId": "your-github-client-id",
      "ClientSecret": "your-github-client-secret"
    },
    "Microsoft": {
      "Instance": "https://login.microsoftonline.com/",
      "TenantId": "your-tenant-id",
      "ClientId": "your-microsoft-client-id",
      "ClientSecret": "your-microsoft-client-secret",
      "CallbackPath": "/signin-microsoft"
    }
  },
  "JWT": {
    "SecretKey": "your-secret-key-for-jwt-tokens-must-be-at-least-256-bits",
    "Issuer": "JoineryServer",
    "Audience": "JoineryClients",
    "ExpirationHours": 24
  },
  "Cors": {
    "AllowedOrigins": ["https://your-production-domain.com"],
    "AllowCredentials": true,
    "PreflightMaxAge": 86400
  }
}
```

## Container Development & Deployment

This section explains the containerization approach for Joinery Server, including why the Dockerfile lives in this repository and how it relates to infrastructure orchestration.

### Dockerfile Ownership & Rationale

The `Dockerfile` for Joinery Server lives in this application repository, **not in the infrastructure repository**. This approach follows industry best practices for several key reasons:

#### Why Application Repository Owns the Dockerfile
- **Tight Coupling**: Build instructions are tightly coupled to the application code and should be versioned together
- **Code + Build Consistency**: Changes to application dependencies, runtime requirements, or build steps are automatically versioned with the code changes
- **Developer Experience**: Developers can build, test, and run containers locally using the same build process as production
- **Atomic Updates**: Application updates and build configuration changes happen atomically in a single commit/PR

#### Infrastructure Repository Responsibilities
The [joinery-infra](https://github.com/chz160/joinery-infra) repository handles:
- **Orchestration**: Docker Compose files for full stack deployment
- **CI/CD Pipelines**: Build automation, testing, and deployment workflows  
- **Environment Configuration**: Production, staging, and development environment setups
- **Deployment Scripts**: Infrastructure provisioning and deployment automation

### Recommended Repository Structure

```
joinery-server/                     # Application Repository (THIS REPO)
├── Dockerfile                      # Container build instructions (LIVES HERE)
├── Controllers/                    # API controllers
├── Services/                       # Business logic services
├── Models/                         # Data models
├── Data/                          # Entity Framework context
├── Program.cs                     # Application startup
├── JoineryServer.csproj           # Project file
└── README.md                      # This documentation

joinery-infra/                      # Infrastructure Repository (SEPARATE REPO)
├── docker-compose.yml             # Full stack orchestration
├── docker-compose.prod.yml        # Production overrides
├── .github/workflows/             # CI/CD pipelines
├── terraform/                     # Infrastructure as code
├── helm/                          # Kubernetes deployment
└── scripts/                       # Deployment automation
```

### Local Development with Docker

Build and run locally using the Dockerfile in this repository:

```bash
# Build the container image
docker build -t joinery-server .

# Run locally with development settings
docker run -d \
  --name joinery-server \
  -p 5256:5256 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e Authentication__GitHub__ClientId="your-dev-client-id" \
  --env-file .env.development \
  joinery-server

# Access the application
# HTTP: http://localhost:5256
# Health check: http://localhost:5256/api/health
# Swagger UI: http://localhost:5256/swagger
```

**Docker Image Features**:
- Multi-stage build for optimized production image
- Non-root user for enhanced security
- Built-in health checks
- Multi-platform support (amd64, arm64)
- Automatic publishing to Docker Hub on main branch commits

### Integration with Infrastructure Repository

The infrastructure repository references this image for orchestration:

#### Example Docker Compose (from joinery-infra repository)
```yaml
services:
  api:
    image: chz160/joinery-server:latest    # Built from THIS repo's Dockerfile
    ports:
      - "5256:5256"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Authentication__GitHub__ClientId=${GITHUB_CLIENT_ID}
    depends_on:
      - db
    networks:
      - joinery-network

  db:
    image: postgres:15
    environment:
      - POSTGRES_DB=joinerydb
      - POSTGRES_USER=joineryuser
      - POSTGRES_PASSWORD=${DB_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - joinery-network

  frontend:
    image: chz160/joinery-frontend:latest
    ports:
      - "3000:80"
    environment:
      - REACT_APP_API_URL=http://api:5256
    depends_on:
      - api
    networks:
      - joinery-network

volumes:
  postgres_data:

networks:
  joinery-network:
    driver: bridge
```

### CI/CD Workflow Integration

The relationship between this repository and the infrastructure repository follows this pattern:

1. **Application Repository (joinery-server)**: 
   - Developers make code changes
   - CI pipeline builds Docker image using local Dockerfile
   - Image is pushed to container registry (GitHub Container Registry or Docker Hub)
   - Tagged with commit SHA and version tags

2. **Infrastructure Repository (joinery-infra)**:
   - References the latest stable image tags from joinery-server
   - Orchestrates deployment using Docker Compose or Kubernetes
   - Manages environment-specific configuration
   - Handles production deployment pipelines

#### CI Pipeline Implementation

This repository includes automated Docker image building and publishing via GitHub Actions with conditional registry selection.

**Workflow**: `.github/workflows/docker-publish.yml`
- **Triggers**: Push to `main` branch, pull requests
- **Images**: Multi-platform builds (linux/amd64, linux/arm64)
- **Registry**: Docker Hub or On-Premises registry (based on available secrets)
- **Tags**: `latest` for main branch, commit SHA (short and long), branch names

#### Registry Selection Logic

The workflow automatically selects the appropriate Docker registry based on available GitHub secrets:

1. **On-Premises Registry** (Priority 1): If `DOCKER_REGISTRY_URL` is configured
2. **Docker Hub** (Priority 2): If `DOCKER_HUB_USERNAME` and `DOCKER_HUB_ACCESS_TOKEN` are configured
3. **Skip Build**: If no registry credentials are found

**Docker Hub Configuration**:
Set these secrets in repository Settings > Secrets and variables > Actions:

| Secret Name | Description | Required | Example |
|-------------|-------------|----------|---------|
| `DOCKER_HUB_USERNAME` | Docker Hub username | Yes | `chz160` |
| `DOCKER_HUB_ACCESS_TOKEN` | Docker Hub access token | Yes | `dckr_pat_...` |

**On-Premises Registry Configuration**:
Set these secrets for on-premises registry (takes priority over Docker Hub):

| Secret Name | Description | Required | Example |
|-------------|-------------|----------|---------|
| `DOCKER_REGISTRY_URL` | Registry URL (without protocol) | Yes | `registry.company.com` |
| `DOCKER_REGISTRY_USERNAME` | Registry username | Yes | `ci-user` |
| `DOCKER_REGISTRY_PASSWORD` | Registry password or token | Yes | `secure-password` |

#### Setup Instructions

**For Docker Hub**:
1. Go to [Docker Hub](https://hub.docker.com/) > Account Settings > Security
2. Create new access token with "Read, Write, Delete" permissions
3. Add `DOCKER_HUB_USERNAME` and `DOCKER_HUB_ACCESS_TOKEN` secrets to GitHub repository

**For On-Premises Registry**:
1. Obtain registry credentials from your administrator
2. Add `DOCKER_REGISTRY_URL`, `DOCKER_REGISTRY_USERNAME`, and `DOCKER_REGISTRY_PASSWORD` secrets to GitHub repository
3. Ensure the registry supports multi-platform builds (linux/amd64, linux/arm64)

**Registry Selection Logging**:
The workflow logs clearly indicate which registry is selected during each run:
- ✅ Docker Hub registry detected with credentials
- ✅ On-premises registry detected: registry.company.com
- ❌ No registry credentials found!
3. Add secrets to GitHub repository settings
4. Push to main branch triggers automatic image build and publish to the selected registry

```yaml
# In joinery-infra/.github/workflows/deploy.yml  
name: Deploy Stack
on:
  workflow_dispatch:
    inputs:
      api_version:
        description: 'API version to deploy'
        required: true
        default: 'latest'

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Deploy with specific API version
        run: |
          export API_IMAGE=chz160/joinery-server:${{ github.event.inputs.api_version }}
          docker-compose -f docker-compose.prod.yml up -d
```

### Benefits of This Approach

- **Version Synchronization**: Application code and build instructions stay in sync
- **Developer Productivity**: Local development matches production build exactly  
- **Clear Separation**: Infrastructure concerns separated from application concerns
- **Scalable Architecture**: Multiple applications can follow the same pattern
- **Better Testing**: Application containers can be tested independently of infrastructure

### Getting Started

1. **Clone this repository** and build locally:
   ```bash
   git clone https://github.com/chz160/joinery-server.git
   cd joinery-server
   docker build -t joinery-server .
   ```

2. **For full stack development**, clone the infrastructure repository:
   ```bash
   git clone https://github.com/chz160/joinery-infra.git
   cd joinery-infra
   docker-compose up -d
   ```

3. **For production deployment**, see the [joinery-infra repository](https://github.com/chz160/joinery-infra) for detailed orchestration and deployment instructions.

## Production Deployment

> 🚀 **Important:** This section provides comprehensive guidance for deploying Joinery Server to production environments with security best practices.

### Environment-Specific Configuration

#### Option 1: Configuration Files (Recommended for Container Deployments)

Create production configuration files using the provided templates:

```bash
# Copy the production template
cp appsettings.Production.json.example appsettings.Production.json

# Edit with production values (DO NOT commit this file)
# Configure your deployment to include this file at runtime
```

**Production appsettings.Production.json structure:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "JoineryServer": "Information"
    }
  },
  "AllowedHosts": "your-production-domain.com",
  "Authentication": {
    "GitHub": {
      "ClientId": "your-production-github-client-id",
      "ClientSecret": "your-production-github-client-secret"
    },
    "Microsoft": {
      "TenantId": "your-production-tenant-id",
      "ClientId": "your-production-microsoft-client-id",
      "ClientSecret": "your-production-microsoft-client-secret"
    }
  },
  "JWT": {
    "SecretKey": "your-production-jwt-secret-key-256-bits-minimum",
    "Issuer": "JoineryServer",
    "Audience": "JoineryClients",
    "ExpirationHours": 24
  },
  "Cors": {
    "AllowedOrigins": ["https://your-production-frontend.com", "https://your-app-domain.com"],
    "AllowCredentials": true,
    "PreflightMaxAge": 86400
  }
}
```

#### Option 2: Environment Variables (Recommended for Cloud Deployments)

Use environment variables for cloud-native deployments:

```bash
# Copy and configure environment variables
cp .env.example .env.production

# Set required environment variables in your deployment system
export ASPNETCORE_ENVIRONMENT=Production
export Authentication__GitHub__ClientId="your-production-github-client-id"
export Authentication__GitHub__ClientSecret="your-production-github-client-secret"
export Authentication__Microsoft__TenantId="your-production-tenant-id"
export Authentication__Microsoft__ClientId="your-production-microsoft-client-id"
export Authentication__Microsoft__ClientSecret="your-production-microsoft-client-secret"
export JWT__SecretKey="your-production-jwt-secret-key-256-bits-minimum"
export JWT__Issuer="JoineryServer"
export JWT__Audience="JoineryClients"
export JWT__ExpirationHours="24"
export ASPNETCORE_URLS="http://+:5000;https://+:5001"
```

### Required Secrets and Configuration

#### Critical Production Secrets (NEVER in source control):
- **`Authentication__GitHub__ClientSecret`**: GitHub OAuth application secret
- **`Authentication__Microsoft__ClientSecret`**: Microsoft Entra ID application secret
- **`JWT__SecretKey`**: JWT token signing key (minimum 256 bits)
- **`ConnectionStrings__DefaultConnection`**: Database connection string (if using external database)

#### Required Configuration Values:
- **`Authentication__GitHub__ClientId`**: GitHub OAuth application client ID
- **`Authentication__Microsoft__TenantId`**: Azure AD tenant ID
- **`Authentication__Microsoft__ClientId`**: Microsoft application client ID
- **`JWT__Issuer`**: Token issuer identifier (e.g., "JoineryServer")
- **`JWT__Audience`**: Token audience identifier (e.g., "JoineryClients")
- **`AllowedHosts`**: Allowed host names for the application

#### Optional Configuration:
- **`JWT__ExpirationHours`**: Token expiration time (default: 24 hours)
- **`Logging__LogLevel__*`**: Application logging levels
- **`ASPNETCORE_URLS`**: Server URLs and ports
- **`Cors__AllowedOrigins`**: Array of allowed frontend origins (production security)
- **`Cors__AllowCredentials`**: Enable credential support for CORS requests (default: true)
- **`Cors__PreflightMaxAge`**: Cache duration for preflight requests in seconds (default: 86400)

### Managed Secrets Stores (Highly Recommended)

#### Azure Key Vault Integration
```bash
# Install Azure Key Vault configuration provider
dotnet add package Microsoft.Extensions.Configuration.AzureKeyVault

# Configure in Program.cs (add to your deployment)
builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-keyvault.vault.azure.net/"),
    new DefaultAzureCredential()
);
```

**Key Vault Secret Names:**
- `Authentication--GitHub--ClientSecret`
- `Authentication--Microsoft--ClientSecret`
- `JWT--SecretKey`
- `ConnectionStrings--DefaultConnection`

#### AWS Secrets Manager Integration
```bash
# Install AWS Secrets Manager configuration provider
dotnet add package Amazon.Extensions.Configuration.SystemsManager

# Environment variables for AWS credentials
export AWS_REGION=us-east-1
export AWS_ACCESS_KEY_ID=your-access-key
export AWS_SECRET_ACCESS_KEY=your-secret-key
```

#### Alternative Secret Stores:
- **HashiCorp Vault**: Enterprise secret management
- **Google Secret Manager**: Google Cloud secret management
- **Kubernetes Secrets**: Container orchestration secrets
- **Docker Secrets**: Docker Swarm secret management

### Production Deployment Checklist

#### 1. Pre-deployment Setup
- [ ] **OAuth Applications Configured:**
  - [ ] GitHub OAuth app created with production callback URLs
  - [ ] Microsoft Entra ID app registration configured
  - [ ] Callback URLs match your production domain
- [ ] **Secrets Management:**
  - [ ] Production secrets stored in managed secret store (Azure Key Vault, AWS Secrets Manager)
  - [ ] JWT secret key generated (minimum 256 bits)
  - [ ] All placeholder values replaced with production values
- [ ] **Database Setup:**
  - [ ] Production database server provisioned and configured
  - [ ] Database schema deployed using Entity Framework migrations
  - [ ] Database user created with appropriate permissions
  - [ ] Connection strings tested and secured
  - [ ] See [Database Setup Guide](DATABASE.md) for detailed instructions
- [ ] **Infrastructure Preparation:**
  - [ ] Production server/container environment configured
  - [ ] HTTPS certificates installed and configured
  - [ ] DNS records configured for production domain

#### 2. Build and Package
```bash
# Clean build
dotnet clean
dotnet restore

# Build for production
dotnet build -c Release

# Publish application
dotnet publish JoineryServer.csproj -c Release -o ./publish

# Verify published files
ls -la ./publish/
```

#### 3. Configuration Deployment
- [ ] **Environment Variables Set:**
  ```bash
  # Verify critical environment variables
  echo $ASPNETCORE_ENVIRONMENT  # Should be "Production"
  echo $Authentication__GitHub__ClientId
  echo $JWT__Issuer
  # DO NOT echo secrets in production logs
  ```
- [ ] **Configuration Files:**
  - [ ] `appsettings.Production.json` deployed (if using file-based config)
  - [ ] No template files with placeholder values in production
- [ ] **Security Verification:**
  - [ ] No `.local.json` files in production
  - [ ] No `.env` files with real secrets committed to source control
  - [ ] All secrets retrieved from managed secret store

#### 4. Application Startup
```bash
# Start application in production mode
cd ./publish
export ASPNETCORE_ENVIRONMENT=Production
dotnet JoineryServer.dll

# Alternative: Use systemd service, Docker, or process manager
```

#### 5. Health Verification
```bash
# Test health endpoints
curl -f http://localhost:5000/api/health
# Expected: {"Status":"Healthy","Timestamp":"...","Version":"1.0.0","Service":"Joinery Server"}

curl -f http://localhost:5000/api/health/ready
# Expected: {"Status":"Ready","Timestamp":"...","Message":"Service is ready to accept requests"}

# Test HTTPS (if configured)
curl -f https://your-production-domain.com/api/health

# Verify authentication endpoints respond
curl -I https://your-production-domain.com/api/auth/login/github
# Expected: HTTP redirect to GitHub OAuth
```

#### 6. Security Verification
- [ ] **HTTPS Enforcement:**
  - [ ] Application only accepts HTTPS in production
  - [ ] HTTP requests redirect to HTTPS
  - [ ] SSL/TLS certificates valid and properly configured
- [ ] **Authentication Testing:**
  - [ ] GitHub OAuth flow works end-to-end
  - [ ] Microsoft Entra ID OAuth flow works end-to-end
  - [ ] JWT tokens generated with correct expiration
- [ ] **API Security:**
  - [ ] Protected endpoints return 401 without authentication
  - [ ] JWT tokens properly validated
  - [ ] CORS configured for production domains only (no wildcard origins in production)
  - [ ] CORS preflight requests handled correctly
  - [ ] CORS credentials properly restricted to trusted origins

#### 7. Monitoring and Logging
- [ ] **Application Logging:**
  - [ ] Log levels configured appropriately for production
  - [ ] No sensitive data logged (secrets, tokens, passwords)
  - [ ] Structured logging implemented for monitoring tools
- [ ] **Health Monitoring:**
  - [ ] Health check endpoints monitored
  - [ ] Application performance monitoring configured
  - [ ] Error tracking and alerting set up

### Production Environment Variables Template

```bash
# Production Environment Configuration
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:5001;http://+:5000
ASPNETCORE_HTTPS_PORT=5001

# Domain Configuration
AllowedHosts=your-production-domain.com

# Authentication (retrieve from secure secret store)
Authentication__GitHub__ClientId=prod-github-client-id
Authentication__GitHub__ClientSecret=prod-github-client-secret
Authentication__Microsoft__TenantId=prod-tenant-id
Authentication__Microsoft__ClientId=prod-microsoft-client-id
Authentication__Microsoft__ClientSecret=prod-microsoft-client-secret

# JWT Configuration (retrieve secret from secure store)
JWT__SecretKey=production-jwt-secret-key-minimum-256-bits
JWT__Issuer=JoineryServer
JWT__Audience=JoineryClients
JWT__ExpirationHours=24

# CORS Configuration for frontend origins
Cors__AllowedOrigins__0=https://your-production-frontend.com
Cors__AllowedOrigins__1=https://your-app-domain.com
Cors__AllowCredentials=true
Cors__PreflightMaxAge=86400

# Database (if using external database)
ConnectionStrings__DefaultConnection=production-database-connection-string

# Logging
Logging__LogLevel__Default=Information
Logging__LogLevel__Microsoft__AspNetCore=Warning
```

### Docker Configuration

The application is optimized for containerized deployments with the following features:

#### Docker Image Configuration
- **Base Image**: `mcr.microsoft.com/dotnet/aspnet:8.0-jammy`
- **Non-root User**: Runs as `joinery` user for enhanced security
- **Ports**: HTTP on 5256 (SSL termination handled by reverse proxy)
- **Health Check**: Built-in monitoring of `/api/health` endpoint
- **Multi-platform**: Supports both amd64 and arm64 architectures

> **SSL/TLS Configuration**: The container runs HTTP only. In production, SSL termination should be handled by a reverse proxy (nginx, traefik, cloud load balancer, etc.) or container orchestration platform.

#### Docker Environment Variables
```bash
# Required environment variables for Docker deployment
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5256

# Authentication secrets (from secure store)
Authentication__GitHub__ClientId=your-github-client-id
Authentication__GitHub__ClientSecret=your-github-client-secret
Authentication__Microsoft__TenantId=your-tenant-id
Authentication__Microsoft__ClientId=your-microsoft-client-id
Authentication__Microsoft__ClientSecret=your-microsoft-client-secret

# JWT Configuration
JWT__SecretKey=your-production-jwt-secret-256-bits-minimum
JWT__Issuer=JoineryServer
JWT__Audience=JoineryClients
JWT__ExpirationHours=24
```

#### Docker Security Best Practices
- Container runs as non-root user (`joinery:joinery`)
- Minimal base image with only required dependencies
- Health checks for container orchestration
- Multi-stage build for optimized image size
- Automated security scanning in CI/CD pipeline

#### SSL/TLS Termination in Production

> **Important**: The Docker container serves HTTP only. SSL/TLS should be terminated at the reverse proxy layer for security and performance benefits.

**Recommended SSL Termination Architecture:**

```
Internet → Load Balancer/Reverse Proxy (SSL Termination) → Container (HTTP)
```

**Common SSL Termination Solutions:**
- **Cloud Load Balancers**: AWS ALB, Azure App Gateway, GCP Load Balancer
- **Reverse Proxies**: nginx, traefik, Apache HTTP Server
- **Container Orchestration**: Kubernetes Ingress, Docker Swarm routing mesh
- **CDN Services**: CloudFlare, AWS CloudFront, Azure CDN

**Example nginx SSL termination config:**
```nginx
server {
    listen 443 ssl;
    server_name your-domain.com;
    
    ssl_certificate /path/to/certificate.crt;
    ssl_certificate_key /path/to/private.key;
    
    location / {
        proxy_pass http://joinery-container:5256;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### Security Warnings

> ⚠️ **CRITICAL SECURITY WARNINGS:**

1. **Never Store Secrets in Source Control:**
   - Never commit `appsettings.Production.json` with real values
   - Never commit `.env` files with production secrets
   - Use `.gitignore` to exclude all production configuration files

2. **Use Managed Secret Stores:**
   - Azure Key Vault, AWS Secrets Manager, or equivalent
   - Rotate secrets regularly (every 90 days recommended)
   - Use different secrets for different environments

3. **Network Security:**
   - Enable HTTPS only in production
   - Configure proper CORS policies for production domains
   - Use reverse proxy (nginx, Apache) for additional security

4. **Monitoring and Auditing:**
   - Enable audit logging for authentication events
   - Monitor for unusual authentication patterns
   - Set up alerts for health check failures

5. **Regular Security Maintenance:**
   - Keep .NET runtime updated
   - Update NuGet packages regularly for security patches
   - Review and rotate OAuth application secrets
   - Monitor GitHub/Microsoft security advisories

### Deployment Examples

#### Docker Deployment

The repository includes a production-ready `Dockerfile` for containerized deployment. This Dockerfile uses multi-stage builds for optimal image size and security.

```bash
# Build and run with Docker
docker build -t joinery-server .
docker run -d \
  -p 5000:80 -p 5001:443 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e Authentication__GitHub__ClientId="your-client-id" \
  --env-file .env.production \
  joinery-server
```

#### Systemd Service
```ini
# /etc/systemd/system/joinery-server.service
[Unit]
Description=Joinery Server
After=network.target

[Service]
Type=notify
WorkingDirectory=/opt/joinery-server
ExecStart=/usr/bin/dotnet JoineryServer.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=joinery-server
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
EnvironmentFile=/opt/joinery-server/.env.production

[Install]
WantedBy=multi-user.target
```

```bash
# Install and start service
sudo systemctl enable joinery-server
sudo systemctl start joinery-server
sudo systemctl status joinery-server
```

## Security Notes

> ⚠️ **CRITICAL**: This application handles sensitive credentials and authentication tokens. Follow these security practices carefully.

### 🔐 Credential Management

#### NEVER Commit These Files:
- Local configuration overrides (`appsettings.*.local.json`)
- `.env` files containing real values  
- Any file containing API keys, client secrets, or JWT secret keys
- Modified template files with real credentials

#### Safe to Commit (Templates Only):
- `appsettings.Development.json` (with placeholder values only)
- `appsettings.json` (with placeholder values only)
- `*.example` files
- Configuration templates without real secrets

#### Types of Sensitive Information:
- **OAuth Client Secrets**: GitHub and Microsoft authentication secrets
- **JWT Secret Keys**: Used for token signing and validation
- **Database Connection Strings**: If using external databases
- **API Keys**: Any third-party service keys
- **Certificates**: SSL certificates and private keys
- **Environment Variables**: Production configuration values

#### Safe Configuration Practices:
1. **Keep Templates in Git**: Template files with placeholders can be tracked
2. **Use Local Overrides**: Create `*.local.json` files for real credentials  
3. **Environment Variables**: Use environment variables for production deployments
4. **Secret Management**: Use Azure Key Vault, AWS Secrets Manager, or similar for production
5. **Local Development**: Keep development credentials separate from production
6. **Regular Rotation**: Rotate secrets regularly, especially before major releases

### 🛡️ Pre-commit Security Checks

#### Recommended Tools:
1. **git-secrets**: Scan for sensitive data before commits
   ```bash
   # Install git-secrets
   git clone https://github.com/awslabs/git-secrets.git
   cd git-secrets && make install
   
   # Configure for your repo
   cd /path/to/your/repo
   git secrets --install
   git secrets --register-aws
   git secrets --add-provider -- cat .gitsecrets
   ```

2. **detect-secrets**: Advanced secret scanning
   ```bash
   pip install detect-secrets
   detect-secrets scan --all-files
   ```

3. **GitHub Secret Scanning**: Enable on your repository
   - Go to repository Settings > Security & analysis
   - Enable "Secret scanning" and "Push protection"

4. **Pre-commit Hooks**: Add to `.pre-commit-config.yaml`
   ```yaml
   repos:
     - repo: https://github.com/Yelp/detect-secrets
       rev: v1.4.0
       hooks:
         - id: detect-secrets
           args: ['--baseline', '.secrets.baseline']
   ```

### 🔍 Secret Detection Patterns

The following patterns indicate potential secrets:
- `ClientSecret`: OAuth client secrets
- `SecretKey`: JWT signing keys  
- `ConnectionString`: Database connections
- `ApiKey` or `API_KEY`: Service API keys
- `Password` or `Pass`: Authentication passwords
- Base64 encoded strings longer than 40 characters
- Hexadecimal strings longer than 32 characters
- AWS Access Key patterns (AKIA...)
- Private key headers (-----BEGIN PRIVATE KEY-----)

### JWT Token Security
- JWT tokens expire based on configuration (default: 24 hours, 1 hour in development)
- Tokens are signed using HMAC SHA256 with configurable secret keys
- Separate token configuration for development and production environments
- Token validation includes issuer, audience, lifetime, and signature verification

### Authentication & Authorization
- All query and management endpoints require authentication
- OAuth providers (GitHub, Microsoft) handle user credential validation
- Role-based access control for organizations and teams
- Granular permissions system for different operations

### Production Security Considerations
- Store secrets securely using Azure Key Vault or similar secret management systems
- Use environment-specific configuration files
- Implement HTTPS in production (configured by default)
- Consider token refresh mechanisms for long-running applications
- Regularly rotate JWT secret keys and OAuth client secrets
- Enable secret scanning on your repositories
- Use infrastructure as code for reproducible deployments
- Monitor for unusual authentication patterns
- Implement proper logging while avoiding logging sensitive data

### 🚨 Emergency Response
If sensitive data is accidentally committed:
1. **Immediately rotate** all exposed credentials
2. **Force push** to remove from history (if possible)
3. **Review access logs** for unauthorized usage
4. **Update security documentation** to prevent recurrence
5. **Consider using** `git filter-branch` or BFG Repo-Cleaner for history cleanup

## Contributing

This is a minimal viable product (MVP) focused on core functionality. Contributions should maintain simplicity while adding value.

### 🔒 Security Requirements for Contributors

Before contributing, please ensure:

1. **Never commit sensitive data**:
   - Use only template files (`.example` files) in commits
   - Test with placeholder credentials only
   - Review changes for accidentally included secrets

2. **Run security checks**:
   ```bash
   # Check for secrets before committing
   git diff --cached | grep -E "(secret|key|password|token)" 
   
   # Ensure template files are used
   git diff --cached --name-only | grep -E "appsettings.*\.json$" | grep -v "\.example$"
   ```

3. **Follow security best practices**:
   - Document any new configuration requirements in template files
   - Update security documentation for new features
   - Consider security implications of API changes
   - Test authentication and authorization flows

4. **Security-focused pull request checklist**:
   - [ ] No real credentials committed
   - [ ] Template files updated if needed
   - [ ] Security documentation updated
   - [ ] .gitignore updated for new sensitive file types
   - [ ] New endpoints have proper authentication
   - [ ] Input validation implemented for user data

## License

MIT License - see LICENSE file for details.