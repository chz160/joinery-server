# Joinery Server

The server-side portion of Joinery - a platform for sharing and managing database queries. This ASP.NET Core Web API provides authentication via GitHub and Microsoft Entra ID, and offers read-only access to a shared repository of database queries.

## Features

- **Authentication**: GitHub OAuth and Microsoft Entra ID integration
- **Read-only API**: Access to shared database queries
- **Team Management**: Create teams, add members, manage roles with administrator permissions
- **JWT Token-based Security**: Secure API access after authentication
- **RESTful API**: Clean, documented endpoints
- **In-Memory Database**: Quick setup for MVP with Entity Framework Core
- **Swagger Documentation**: Interactive API documentation
- **Health Checks**: Service monitoring endpoints

## Prerequisites

- .NET 8.0 SDK
- Visual Studio, Visual Studio Code, or any text editor
- GitHub OAuth App (for GitHub authentication)
- Microsoft Entra ID App Registration (for Microsoft authentication)

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
- `GET /api/auth/callback/github` - GitHub OAuth callback
- `GET /api/auth/callback/microsoft` - Microsoft OAuth callback

### Database Queries (Authenticated)

- `GET /api/queries` - Get all database queries
- `GET /api/queries/{id}` - Get specific query by ID
- `GET /api/queries/search?searchTerm={term}` - Search queries by name/description/tags
- `GET /api/queries/by-database/{databaseType}` - Filter by database type
- `GET /api/queries/by-tag/{tag}` - Filter by tag

### Teams (Authenticated)

- `GET /api/teams` - Get all teams for current user
- `POST /api/teams` - Create a new team
- `GET /api/teams/{id}` - Get team details with members
- `PUT /api/teams/{id}` - Update team (admins only)
- `DELETE /api/teams/{id}` - Delete team (creator only)
- `POST /api/teams/{id}/members` - Add member to team (admins only)
- `DELETE /api/teams/{id}/members/{userId}` - Remove member from team (admins only, or self)
- `PUT /api/teams/{id}/members/{userId}/role` - Update member role (admins only)

### Health Checks

- `GET /api/health` - Service health check
- `GET /api/health/ready` - Service readiness check

## Usage

1. **Access Swagger UI**: Navigate to `/swagger` endpoint (e.g., `https://localhost:7050/swagger`) for interactive API documentation

2. **Authenticate**: Use one of the login endpoints to authenticate via GitHub or Microsoft

3. **Get JWT Token**: After successful authentication, you'll receive a JWT token

4. **Access Protected Endpoints**: Include the JWT token in the Authorization header:
   ```
   Authorization: Bearer your-jwt-token
   ```

## Sample Queries

The application comes pre-seeded with sample database queries for development:

1. **Sample User Query**: Basic user retrieval query
2. **User Count by Registration Date**: Analytics query for user registrations

## Architecture

- **ASP.NET Core 8.0**: Modern web framework
- **Entity Framework Core**: ORM with In-Memory database for MVP
- **JWT Authentication**: Token-based security
- **Swagger/OpenAPI**: API documentation
- **OAuth 2.0**: GitHub and Microsoft authentication

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

- `Authentication`: OAuth provider settings
- `JWT`: Token configuration
- `Logging`: Application logging levels

## Security Notes

- JWT tokens expire based on configuration (default: 24 hours)
- All query endpoints require authentication
- OAuth providers handle user credential validation
- Secrets should be stored securely (Azure Key Vault, etc.) in production

## Contributing

This is a minimal viable product (MVP) focused on core functionality. Contributions should maintain simplicity while adding value.

## License

MIT License - see LICENSE file for details.