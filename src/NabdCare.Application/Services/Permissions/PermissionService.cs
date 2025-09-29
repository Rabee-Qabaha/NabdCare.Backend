using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.DTOs.Permissions;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Application.Interfaces.Users;
using NabdCare.Domain.Entities.Users;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.Services.Permissions;

public class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        IPermissionRepository permissionRepository,
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<PermissionService> logger)
    {
        _permissionRepository = permissionRepository;
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    #region Permission CRUD

    public async Task<IEnumerable<PermissionResponseDto>> GetAllPermissionsAsync()
    {
        try
        {
            var permissions = await _permissionRepository.GetAllPermissionsAsync();
            return _mapper.Map<IEnumerable<PermissionResponseDto>>(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all permissions.");
            throw new InvalidOperationException("Failed to retrieve permissions.", ex);
        }
    }

    public async Task<PermissionResponseDto?> GetPermissionByIdAsync(Guid id)
    {
        try
        {
            var permission = await _permissionRepository.GetPermissionByIdAsync(id);
            return permission == null ? null : _mapper.Map<PermissionResponseDto>(permission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve permission {PermissionId}.", id);
            throw new InvalidOperationException($"Failed to retrieve permission {id}.", ex);
        }
    }

    public async Task<PermissionResponseDto> CreatePermissionAsync(CreatePermissionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Permission name is required.", nameof(dto.Name));

        try
        {
            var entity = _mapper.Map<AppPermission>(dto);
            var created = await _permissionRepository.CreatePermissionAsync(entity);
            return _mapper.Map<PermissionResponseDto>(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create permission {PermissionName}.", dto.Name);
            throw new InvalidOperationException($"Failed to create permission {dto.Name}.", ex);
        }
    }

    public async Task<PermissionResponseDto?> UpdatePermissionAsync(Guid id, UpdatePermissionDto dto)
    {
        try
        {
            var entity = _mapper.Map<AppPermission>(dto);
            var updated = await _permissionRepository.UpdatePermissionAsync(id, entity);
            return updated == null ? null : _mapper.Map<PermissionResponseDto>(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update permission {PermissionId}.", id);
            throw new InvalidOperationException($"Failed to update permission {id}.", ex);
        }
    }

    public async Task<bool> DeletePermissionAsync(Guid id)
    {
        try
        {
            return await _permissionRepository.DeletePermissionAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete permission {PermissionId}.", id);
            throw new InvalidOperationException($"Failed to delete permission {id}.", ex);
        }
    }

    #endregion

    #region Role/User Permission Management

    public async Task<bool> AssignPermissionToRoleAsync(UserRole role, Guid permissionId)
    {
        if (!Enum.IsDefined(typeof(UserRole), role))
            throw new ArgumentException("Invalid role.", nameof(role));

        try
        {
            return await _permissionRepository.AssignPermissionToRoleAsync(role, permissionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign permission {PermissionId} to role {Role}.", permissionId, role);
            throw new InvalidOperationException($"Failed to assign permission to role.", ex);
        }
    }

    public async Task<bool> RemovePermissionFromRoleAsync(UserRole role, Guid permissionId)
    {
        if (!Enum.IsDefined(typeof(UserRole), role))
            throw new ArgumentException("Invalid role.", nameof(role));

        try
        {
            return await _permissionRepository.RemovePermissionFromRoleAsync(role, permissionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove permission {PermissionId} from role {Role}.", permissionId, role);
            throw new InvalidOperationException($"Failed to remove permission from role.", ex);
        }
    }

    public async Task<bool> AssignPermissionToUserAsync(Guid userId, Guid permissionId)
    {
        try
        {
            var user = await _userRepository.GetUserByIdAsync(userId)
                       ?? throw new KeyNotFoundException($"User {userId} not found.");

            return await _permissionRepository.AssignPermissionToUserAsync(userId, permissionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign permission {PermissionId} to user {UserId}.", permissionId, userId);
            throw new InvalidOperationException($"Failed to assign permission to user.", ex);
        }
    }

    public async Task<bool> RemovePermissionFromUserAsync(Guid userId, Guid permissionId)
    {
        try
        {
            return await _permissionRepository.RemovePermissionFromUserAsync(userId, permissionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove permission {PermissionId} from user {UserId}.", permissionId, userId);
            throw new InvalidOperationException($"Failed to remove permission from user.", ex);
        }
    }

    #endregion

    #region Effective Permissions

    public async Task<IEnumerable<PermissionResponseDto>> GetUserEffectivePermissionsAsync(Guid userId, UserRole role)
    {
        try
        {
            var rolePerms = await _permissionRepository.GetPermissionsByRoleAsync(role);
            var userPerms = await _permissionRepository.GetPermissionsByUserAsync(userId);

            var combined = rolePerms.Concat(userPerms)
                                    .GroupBy(p => p.Id)
                                    .Select(g => g.First());

            return _mapper.Map<IEnumerable<PermissionResponseDto>>(combined);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch effective permissions for user {UserId}.", userId);
            throw new InvalidOperationException("Failed to fetch effective permissions.", ex);
        }
    }

    public async Task<bool> UserHasPermissionAsync(Guid userId, UserRole role, string permissionName)
    {
        if (string.IsNullOrWhiteSpace(permissionName))
            throw new ArgumentException("Permission name is required.", nameof(permissionName));

        var effective = await GetUserEffectivePermissionsAsync(userId, role);
        return effective.Any(p => p.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}