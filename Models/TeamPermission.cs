using System;

namespace JoineryServer.Models;

/// <summary>
/// Defines granular permissions for team members
/// </summary>
[Flags]
public enum TeamPermission
{
    None = 0,
    ReadQueries = 1,
    CreateQueries = 2,
    EditQueries = 4,
    DeleteQueries = 8,
    ManageFolders = 16,
    FullAccess = ReadQueries | CreateQueries | EditQueries | DeleteQueries | ManageFolders
}

/// <summary>
/// Predefined permission levels for convenience
/// </summary>
public static class TeamPermissionLevels
{
    public static readonly TeamPermission ReadOnly = TeamPermission.ReadQueries;
    public static readonly TeamPermission Editor = TeamPermission.ReadQueries | TeamPermission.CreateQueries | TeamPermission.EditQueries;
    public static readonly TeamPermission Manager = TeamPermission.ReadQueries | TeamPermission.CreateQueries | TeamPermission.EditQueries | TeamPermission.DeleteQueries | TeamPermission.ManageFolders;
    public static readonly TeamPermission Administrator = TeamPermission.FullAccess;
}