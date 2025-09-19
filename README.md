# Joinery Server

The server-side portion of Joinery - a platform for sharing and managing database queries. This ASP.NET Core Web API provides authentication via GitHub and Microsoft Entra ID, and offers read-only access to a shared repository of database queries.

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
- **Multi-source Query Access**: Support for both database queries and Git repository-based SQL files
- **Organization Management**: Create and manage organizations with role-based access control and AWS IAM integration
- **Team Management**: Complete team lifecycle with administrator permissions and member roles
- **AWS IAM Integration**: Import users from AWS IAM and enable authentication using AWS credentials
- **Git Repository Integration**: Connect and synchronize SQL queries from external Git repositories
- **Advanced Permission System**: Granular permissions for query access and folder management
- **RESTful API**: Clean, documented endpoints with comprehensive Swagger documentation
- **Health Monitoring**: Built-in health check and readiness endpoints for deployment monitoring
- **In-Memory Database**: Quick MVP setup with Entity Framework Core
- **CORS Support**: Cross-origin resource sharing configured for development

## Prerequisites

- .NET 8.0 SDK
- Visual Studio, Visual Studio Code, or any text editor
- GitHub OAuth App (for GitHub authentication)
- Microsoft Entra ID App Registration (for Microsoft authentication)
- AWS IAM credentials (for AWS authentication integration)

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
   - Homepage URL: `https://localhost:7050`
   - Authorization callback URL: `https://localhost:7050/signin-github`
3. Copy the Client ID and Client Secret

#### Microsoft Entra ID Setup

1. Go to Azure Portal > Microsoft Entra ID > App registrations
2. Create a new registration with:
   - Name: `Joinery Server`
   - Redirect URI: `https://localhost:7050/signin-microsoft`
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

### 3. Update Configuration

Update `appsettings.Development.json` with your authentication credentials:

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

### 4. Run the Application

```bash
dotnet restore
dotnet build
dotnet run
```

The application will start at:
- HTTPS: `https://localhost:7050`
- HTTP: `http://localhost:5050`

## API Endpoints

### Authentication

- `GET /api/auth/login/github` - Initiate GitHub OAuth login
- `GET /api/auth/login/microsoft` - Initiate Microsoft OAuth login
- `POST /api/auth/login/aws` - Authenticate with AWS IAM credentials
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

### Health Checks

- `GET /api/health` - Service health check with version and timestamp
- `GET /api/health/ready` - Service readiness check for deployment orchestration

## Usage

### 1. Access Swagger UI
Navigate to `/swagger` endpoint (e.g., `https://localhost:7050/swagger`) for interactive API documentation with JWT authentication support.

### 2. Authenticate  
Use one of the login endpoints to authenticate via GitHub, Microsoft, or AWS IAM:
- GitHub: `GET /api/auth/login/github`
- Microsoft: `GET /api/auth/login/microsoft`
- AWS IAM: `POST /api/auth/login/aws` (requires organization setup)

For AWS IAM authentication, send a JSON request:
```json
{
  "username": "your-aws-username",
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

### 7. Git Repository Integration
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
- **In-Memory Database**: Entity Framework Core with seeded sample data
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
  }
}
```

## Security Notes

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

## Contributing

This is a minimal viable product (MVP) focused on core functionality. Contributions should maintain simplicity while adding value.

## License

MIT License - see LICENSE file for details.