using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Permissions;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Application.Interfaces.Roles;
using NabdCare.Application.Interfaces.Users;
using NabdCare.Domain.Entities.Permissions;

namespace NabdCare.Application.Services.Permissions;

public class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        IPermissionRepository permissionRepository,
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<PermissionService> logger)
    {
        _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaginatedResult<PermissionResponseDto>> GetAllPagedAsync(PaginationRequestDto pagination)
    {
        if (pagination == null)
            throw new ArgumentNullException(nameof(pagination));

        _logger.LogInformation("Retrieving paginated permissions (Limit={Limit})", pagination.Limit);

        var result = await _permissionRepository.GetAllPagedAsync(pagination);
        var mapped = _mapper.Map<IEnumerable<PermissionResponseDto>>(result.Items);

        _logger.LogInformation("Retrieved {Count} paginated permissions", result.Items.Count());

        return new PaginatedResult<PermissionResponseDto>
        {
            Items = mapped,
            TotalCount = result.TotalCount,
            HasMore = result.HasMore,
            NextCursor = result.NextCursor
        };
    }

    public async Task<IEnumerable<PermissionResponseDto>> GetAllPermissionsAsync()
    {
        _logger.LogInformation("Retrieving all permissions");

        var permissions = await _permissionRepository.GetAllPermissionsAsync();
        var mapped = _mapper.Map<IEnumerable<PermissionResponseDto>>(permissions);

        _logger.LogInformation("Retrieved {Count} permissions", permissions.Count());

        return mapped;
    }

    public async Task<PermissionResponseDto?> GetPermissionByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Permission ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(id));

        _logger.LogInformation("Retrieving permission {PermissionId}", id);

        var permission = await _permissionRepository.GetPermissionByIdAsync(id);
        if (permission == null)
        {
            _logger.LogWarning("Permission {PermissionId} not found. Error code: {ErrorCode}", id, ErrorCodes.NOT_FOUND);
            return null;
        }

        return _mapper.Map<PermissionResponseDto>(permission);
    }

    public async Task<PermissionResponseDto> CreatePermissionAsync(CreatePermissionDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException($"Permission name is required. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(dto.Name));

        _logger.LogInformation("Creating permission {PermissionName}", dto.Name);

        var entity = _mapper.Map<AppPermission>(dto);
        var created = await _permissionRepository.CreatePermissionAsync(entity);

        _logger.LogInformation("Created permission {PermissionId} with name {PermissionName}", created.Id, created.Name);

        return _mapper.Map<PermissionResponseDto>(created);
    }

    public async Task<PermissionResponseDto?> UpdatePermissionAsync(Guid id, UpdatePermissionDto dto)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Permission ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(id));

        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        _logger.LogInformation("Updating permission {PermissionId}", id);

        var entity = _mapper.Map<AppPermission>(dto);
        var updated = await _permissionRepository.UpdatePermissionAsync(id, entity);

        if (updated == null)
        {
            _logger.LogWarning("Permission {PermissionId} not found for update. Error code: {ErrorCode}", id, ErrorCodes.NOT_FOUND);
            return null;
        }

        _logger.LogInformation("Updated permission {PermissionId}", id);

        return _mapper.Map<PermissionResponseDto>(updated);
    }

    public async Task<bool> DeletePermissionAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Permission ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(id));

        _logger.LogInformation("Deleting permission {PermissionId}", id);

        var deleted = await _permissionRepository.DeletePermissionAsync(id);

        if (deleted)
        {
            _logger.LogInformation("Deleted permission {PermissionId}", id);
        }
        else
        {
            _logger.LogWarning("Permission {PermissionId} not found for deletion. Error code: {ErrorCode}", id, ErrorCodes.NOT_FOUND);
        }

        return deleted;
    }

    public async Task<IEnumerable<PermissionResponseDto>> GetPermissionsByRoleAsync(Guid roleId)
    {
        if (roleId == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(roleId));

        _logger.LogInformation("Retrieving permissions for role {RoleId}", roleId);

        var permissions = await _permissionRepository.GetPermissionsByRoleAsync(roleId);
        var mapped = _mapper.Map<IEnumerable<PermissionResponseDto>>(permissions);

        _logger.LogInformation("Retrieved {Count} permissions for role {RoleId}", permissions.Count(), roleId);

        return mapped;
    }

    public async Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
    {
        if (roleId == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(roleId));

        if (permissionId == Guid.Empty)
            throw new ArgumentException($"Permission ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(permissionId));

        _logger.LogInformation("Assigning permission {PermissionId} to role {RoleId}", permissionId, roleId);

        var role = await _roleRepository.GetRoleByIdAsync(roleId);
        if (role == null)
        {
            _logger.LogWarning("Role {RoleId} not found. Error code: {ErrorCode}", roleId, ErrorCodes.ROLE_NOT_FOUND);
            throw new KeyNotFoundException($"Role {roleId} not found. Error code: {ErrorCodes.ROLE_NOT_FOUND}");
        }

        var result = await _permissionRepository.AssignPermissionToRoleAsync(roleId, permissionId);

        if (result)
            _logger.LogInformation("Assigned permission {PermissionId} to role {RoleId}", permissionId, roleId);

        return result;
    }

    public async Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        if (roleId == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(roleId));

        if (permissionId == Guid.Empty)
            throw new ArgumentException($"Permission ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(permissionId));

        _logger.LogInformation("Removing permission {PermissionId} from role {RoleId}", permissionId, roleId);

        var role = await _roleRepository.GetRoleByIdAsync(roleId);
        if (role == null)
        {
            _logger.LogWarning("Role {RoleId} not found. Error code: {ErrorCode}", roleId, ErrorCodes.ROLE_NOT_FOUND);
            throw new KeyNotFoundException($"Role {roleId} not found. Error code: {ErrorCodes.ROLE_NOT_FOUND}");
        }

        var result = await _permissionRepository.RemovePermissionFromRoleAsync(roleId, permissionId);

        if (result)
            _logger.LogInformation("Removed permission {PermissionId} from role {RoleId}", permissionId, roleId);

        return result;
    }

    public async Task<IEnumerable<PermissionResponseDto>> GetPermissionsByUserAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException($"User ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(userId));

        _logger.LogInformation("Retrieving permissions for user {UserId}", userId);

        var permissions = await _permissionRepository.GetPermissionsByUserAsync(userId);
        var mapped = _mapper.Map<IEnumerable<PermissionResponseDto>>(permissions);

        _logger.LogInformation("Retrieved {Count} permissions for user {UserId}", permissions.Count(), userId);

        return mapped;
    }

    public async Task<bool> AssignPermissionToUserAsync(Guid userId, Guid permissionId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException($"User ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(userId));

        if (permissionId == Guid.Empty)
            throw new ArgumentException($"Permission ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(permissionId));

        _logger.LogInformation("Assigning permission {PermissionId} to user {UserId}", permissionId, userId);

        try
        {
            var result = await _permissionRepository.AssignPermissionToUserAsync(userId, permissionId);

            if (result)
                _logger.LogInformation("Assigned permission {PermissionId} to user {UserId}", permissionId, userId);

            return result;
        }
        catch (DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;

            if (message.Contains("Users", StringComparison.OrdinalIgnoreCase) && 
                message.Contains("foreign key", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("User {UserId} not found. Error code: {ErrorCode}", userId, ErrorCodes.USER_NOT_FOUND);
                throw new KeyNotFoundException($"User with ID {userId} does not exist. Error code: {ErrorCodes.USER_NOT_FOUND}");
            }

            if (message.Contains("Permissions", StringComparison.OrdinalIgnoreCase) && 
                message.Contains("foreign key", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Permission {PermissionId} not found. Error code: {ErrorCode}", permissionId, ErrorCodes.NOT_FOUND);
                throw new KeyNotFoundException($"Permission with ID {permissionId} does not exist. Error code: {ErrorCodes.NOT_FOUND}");
            }

            _logger.LogError(ex, "Database error assigning permission {PermissionId} to user {UserId}. Error code: {ErrorCode}", 
                permissionId, userId, ErrorCodes.DATABASE_ERROR);
            throw new InvalidOperationException($"Failed to assign permission to user. Error code: {ErrorCodes.DATABASE_ERROR}", ex);
        }
    }

    public async Task<bool> RemovePermissionFromUserAsync(Guid userId, Guid permissionId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException($"User ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(userId));

        if (permissionId == Guid.Empty)
            throw new ArgumentException($"Permission ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(permissionId));

        _logger.LogInformation("Removing permission {PermissionId} from user {UserId}", permissionId, userId);

        var result = await _permissionRepository.RemovePermissionFromUserAsync(userId, permissionId);

        if (result)
            _logger.LogInformation("Removed permission {PermissionId} from user {UserId}", permissionId, userId);

        return result;
    }

    public async Task<IEnumerable<PermissionResponseDto>> GetUserEffectivePermissionsAsync(Guid userId, Guid roleId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException($"User ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(userId));

        if (roleId == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(roleId));

        _logger.LogInformation("Retrieving effective permissions for user {UserId} with role {RoleId}", userId, roleId);

        var rolePerms = await _permissionRepository.GetPermissionsByRoleAsync(roleId);
        var userPerms = await _permissionRepository.GetPermissionsByUserAsync(userId);

        var combined = rolePerms.Concat(userPerms)
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .ToList();

        var mapped = _mapper.Map<IEnumerable<PermissionResponseDto>>(combined);

        _logger.LogInformation("User {UserId} has {Count} effective permissions", userId, combined.Count);

        return mapped;
    }

    public async Task<bool> UserHasPermissionAsync(Guid userId, Guid roleId, string permissionName)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException($"User ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(userId));

        if (roleId == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(roleId));

        if (string.IsNullOrWhiteSpace(permissionName))
            throw new ArgumentException($"Permission name is required. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(permissionName));

        _logger.LogInformation("Checking if user {UserId} has permission {PermissionName}", userId, permissionName);

        var effective = await GetUserEffectivePermissionsAsync(userId, roleId);
        var hasPermission = effective.Any(p => p.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase));

        if (!hasPermission)
            _logger.LogWarning("User {UserId} does not have permission {PermissionName}. Error code: {ErrorCode}", 
                userId, permissionName, ErrorCodes.INSUFFICIENT_PERMISSIONS);

        return hasPermission;
    }

    public async Task<(Guid RoleId, Guid? ClinicId)?> GetUserForAuthorizationAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException($"User ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(userId));

        _logger.LogInformation("Retrieving authorization info for user {UserId}", userId);

        var user = await _userRepository.GetByIdForAuthorizationAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found. Error code: {ErrorCode}", userId, ErrorCodes.USER_NOT_FOUND);
            return null;
        }

        return (user.RoleId, user.ClinicId);
    }

    public async Task<IEnumerable<(Guid UserId, Guid RoleId)>> GetUsersByRoleAsync(Guid roleId)
    {
        if (roleId == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(roleId));

        _logger.LogInformation("Retrieving users for role {RoleId}", roleId);

        var users = await _userRepository.GetUsersByRoleIdAsync(roleId);
        var result = users.Select(u => (u.Id, u.RoleId)).ToList();

        _logger.LogInformation("Retrieved {Count} users for role {RoleId}", result.Count, roleId);

        return result;
    }
}