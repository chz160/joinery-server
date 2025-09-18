# Git Repository Integration

This document describes how to use the new Git repository integration for storing and managing database queries.

## Overview

Instead of storing queries directly in the database, you can now configure Git repositories at the organization or team level. Queries are stored as SQL files in these repositories, providing:

- **Version Control**: Full Git history for all query changes
- **Collaboration**: Team members can contribute queries via pull requests
- **Organization**: Folder structure for organizing queries
- **Hierarchical Access**: Organization queries visible to all members, team queries to team members only

## Setting Up a Git Repository

### 1. Organization-Level Repository

Organization administrators can configure a Git repository for the entire organization:

```bash
POST /api/GitRepositories
{
  "name": "Acme Corp Queries",
  "repositoryUrl": "https://github.com/acme-corp/sql-queries",
  "branch": "main",
  "accessToken": "ghp_your_token_here",  // Optional for private repos
  "description": "Shared SQL queries for Acme Corp",
  "organizationId": 1
}
```

### 2. Team-Level Repository

Team administrators can configure repositories for their specific teams:

```bash
POST /api/GitRepositories
{
  "name": "Data Team Queries", 
  "repositoryUrl": "https://github.com/acme-corp/data-team-queries",
  "branch": "main",
  "description": "Analytics and reporting queries",
  "teamId": 5
}
```

## Repository Structure

Organize your queries in folders:

```
/
├── analytics/
│   ├── user_metrics.sql
│   ├── revenue_reports.sql
│   └── daily_stats.sql
├── operational/
│   ├── cleanup_tasks.sql
│   └── maintenance.sql
└── user_queries.sql
```

## Query File Format

SQL files should include metadata in comments:

```sql
-- Database: PostgreSQL
-- Description: Get active user count by registration date
-- Tags: users, analytics, daily

SELECT 
    DATE(created_at) as registration_date,
    COUNT(*) as user_count 
FROM users 
WHERE active = true
GROUP BY DATE(created_at) 
ORDER BY registration_date DESC;
```

## API Endpoints

### Repository Management

- `GET /api/GitRepositories` - List accessible repositories
- `POST /api/GitRepositories` - Create repository configuration
- `GET /api/GitRepositories/{id}` - Get repository details
- `POST /api/GitRepositories/{id}/sync` - Sync repository content
- `GET /api/GitRepositories/{id}/folders` - List folders

### Query Access

- `GET /api/Queries` - Get all queries (database + Git-based)
- `GET /api/Queries/{id}` - Get specific query
- `GET /api/Queries/search?searchTerm=analytics` - Search all queries
- `GET /api/Queries/from-git/{repositoryId}/folder?folderPath=analytics` - Get queries from folder
- `GET /api/Queries/from-git/{repositoryId}/history?filePath=analytics/user_metrics.sql` - Get file history

### Permissions

- **Organization Repositories**: Visible to all organization members
- **Team Repositories**: Visible to team members only
- **Management**: Only admins can create/sync repositories

## Usage Examples

### 1. Sync Repository Content

```bash
POST /api/GitRepositories/1/sync
```

Response:
```json
{
  "repositoryId": 1,
  "syncedAt": "2025-01-15T10:30:00Z",
  "fileCount": 25,
  "files": [
    {
      "fileName": "user_metrics.sql",
      "filePath": "analytics/user_metrics.sql", 
      "databaseType": "PostgreSQL",
      "tagCount": 3
    }
  ]
}
```

### 2. Search Across All Sources

```bash
GET /api/Queries/search?searchTerm=user
```

Returns queries from both database and Git repositories that match "user".

### 3. Browse by Folder

```bash
GET /api/Queries/from-git/1/folder?folderPath=analytics
```

Returns all queries in the "analytics" folder.

## Benefits

- **Version Control**: Track who changed what and when
- **Code Review**: Use pull requests for query reviews
- **Backup**: Queries are safely stored in Git
- **Organization**: Folder structure keeps queries organized
- **Collaboration**: Multiple team members can contribute
- **Integration**: Works alongside existing database queries

## Migration Strategy

1. Keep existing database queries for backward compatibility
2. Gradually move queries to Git repositories
3. New queries can be added to either system
4. API seamlessly combines both sources