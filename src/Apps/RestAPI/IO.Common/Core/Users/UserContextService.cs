using Microsoft.Extensions.Logging;
using IO.Common.Infrastructure.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IO.Common.Core.Users;

/// <summary>
/// User context business logic service
/// </summary>
public interface IUserContextBusinessService
{
    Task<UserContext> CreateAsync(UserContext userContext);
    Task<UserContext?> GetByUserIdAsync(string userId);
    Task<UserContext?> GetByEmailAsync(string email);
    Task<bool> UpdateAsync(UserContext userContext);
    Task<bool> AddRoleAsync(string userId, string role);
    Task<bool> RemoveRoleAsync(string userId, string role);
    Task<bool> UpdatePreferencesAsync(string userId, Dictionary<string, object> preferences);
    Task<List<UserContext>> GetByRoleAsync(string role, int limit = 100);
    Task<List<UserContext>> SearchAsync(string query, int limit = 50);
    Task<bool> DeleteAsync(string userId);
    Task<bool> UpdateLastLoginAsync(string userId);
}

/// <summary>
/// User context business logic implementation
/// </summary>
public class UserContextBusinessService : IUserContextBusinessService
{
    private readonly UserContextRepository _userContextRepository;
    private readonly ILogger<UserContextBusinessService> _logger;

    public UserContextBusinessService(
        UserContextRepository userContextRepository,
        ILogger<UserContextBusinessService> logger)
    {
        _userContextRepository = userContextRepository;
        _logger = logger;
    }

    public async Task<UserContext> CreateAsync(UserContext userContext)
    {
        try
        {
            _logger.LogInformation("Creating user context for {UserId}", userContext.UserId);

            // Validate request
            ValidateUserContext(userContext, forCreate: true);

            // Check if user already exists
            var existingUser = await _userContextRepository.GetByUserIdAsync(userContext.UserId);
            if (existingUser != null)
            {
                throw new ConflictException("User", userContext.UserId, "User already exists");
            }

            // Check if email already exists
            var existingEmail = await _userContextRepository.GetByEmailAsync(userContext.Email);
            if (existingEmail != null)
            {
                throw new ConflictException("Email", userContext.Email, "Email already exists");
            }

            // Set default values
            userContext.IsActive = true;
            userContext.LastLoginAt = DateTime.UtcNow;

            return await _userContextRepository.CreateAsync(userContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user context for {UserId}", userContext.UserId);
            throw;
        }
    }

    public async Task<UserContext?> GetByUserIdAsync(string userId)
    {
        try
        {
            _logger.LogDebug("Getting user context for {UserId}", userId);

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ValidationException("User ID is required");
            }

            return await _userContextRepository.GetByUserIdAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user context for {UserId}", userId);
            throw;
        }
    }

    public async Task<UserContext?> GetByEmailAsync(string email)
    {
        try
        {
            _logger.LogDebug("Getting user context by email {Email}", email);

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ValidationException("Email is required");
            }

            if (!IsValidEmail(email))
            {
                throw new ValidationException($"Invalid email format: {email}");
            }

            return await _userContextRepository.GetByEmailAsync(email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user context by email {Email}", email);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(UserContext userContext)
    {
        try
        {
            _logger.LogInformation("Updating user context for {UserId}", userContext.UserId);

            // Validate request
            ValidateUserContext(userContext, forCreate: false);

            // Check if user exists
            var existingUser = await _userContextRepository.GetByUserIdAsync(userContext.UserId);
            if (existingUser == null)
            {
                throw new NotFoundException("User", userContext.UserId);
            }

            // Check if email is being changed and if it's already taken
            if (!string.Equals(existingUser.Email, userContext.Email, StringComparison.OrdinalIgnoreCase))
            {
                var existingEmail = await _userContextRepository.GetByEmailAsync(userContext.Email);
                if (existingEmail != null && existingEmail.UserId != userContext.UserId)
                {
                    throw new ConflictException("Email", userContext.Email, "Email already exists");
                }
            }

            return await _userContextRepository.UpdateAsync(userContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user context for {UserId}", userContext.UserId);
            throw;
        }
    }

    public async Task<bool> AddRoleAsync(string userId, string role)
    {
        try
        {
            _logger.LogInformation("Adding role {Role} to user {UserId}", role, userId);

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ValidationException("User ID is required");
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                throw new ValidationException("Role is required");
            }

            // Check if user exists
            var user = await _userContextRepository.GetByUserIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            // Check if user already has the role
            if (user.Roles.Contains(role))
            {
                _logger.LogWarning("User {UserId} already has role {Role}", userId, role);
                return true;
            }

            return await _userContextRepository.AddRoleAsync(userId, role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding role {Role} to user {UserId}", role, userId);
            throw;
        }
    }

    public async Task<bool> RemoveRoleAsync(string userId, string role)
    {
        try
        {
            _logger.LogInformation("Removing role {Role} from user {UserId}", role, userId);

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ValidationException("User ID is required");
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                throw new ValidationException("Role is required");
            }

            // Check if user exists
            var user = await _userContextRepository.GetByUserIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            // Check if user has the role
            if (!user.Roles.Contains(role))
            {
                _logger.LogWarning("User {UserId} does not have role {Role}", userId, role);
                return true;
            }

            return await _userContextRepository.RemoveRoleAsync(userId, role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {Role} from user {UserId}", role, userId);
            throw;
        }
    }

    public async Task<bool> UpdatePreferencesAsync(string userId, Dictionary<string, object> preferences)
    {
        try
        {
            _logger.LogInformation("Updating preferences for user {UserId}", userId);

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ValidationException("User ID is required");
            }

            if (preferences == null)
            {
                throw new ValidationException("Preferences are required");
            }

            // Check if user exists
            var user = await _userContextRepository.GetByUserIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            return await _userContextRepository.UpdatePreferencesAsync(userId, preferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating preferences for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<UserContext>> GetByRoleAsync(string role, int limit = 100)
    {
        try
        {
            _logger.LogInformation("Getting users with role {Role}", role);

            if (string.IsNullOrWhiteSpace(role))
            {
                throw new ValidationException("Role is required");
            }

            return await _userContextRepository.GetByRoleAsync(role, limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users with role {Role}", role);
            throw;
        }
    }

    public async Task<List<UserContext>> SearchAsync(string query, int limit = 50)
    {
        try
        {
            _logger.LogInformation("Searching users with query '{Query}'", query);

            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ValidationException("Search query is required");
            }

            if (query.Length < 2)
            {
                throw new ValidationException("Search query must be at least 2 characters");
            }

            return await _userContextRepository.SearchAsync(query, limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users with query '{Query}'", query);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Deleting user context for {UserId}", userId);

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ValidationException("User ID is required");
            }

            // Check if user exists
            var user = await _userContextRepository.GetByUserIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            return await _userContextRepository.DeleteAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user context for {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> UpdateLastLoginAsync(string userId)
    {
        try
        {
            _logger.LogDebug("Updating last login for user {UserId}", userId);

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ValidationException("User ID is required");
            }

            // Check if user exists
            var user = await _userContextRepository.GetByUserIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            user.LastLoginAt = DateTime.UtcNow;
            return await _userContextRepository.UpdateAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last login for user {UserId}", userId);
            throw;
        }
    }

    private void ValidateUserContext(UserContext userContext, bool forCreate)
    {
        if (userContext == null)
        {
            throw new ValidationException("User context is required");
        }

        if (forCreate && string.IsNullOrWhiteSpace(userContext.UserId))
        {
            throw new ValidationException("User ID is required for creation");
        }

        if (string.IsNullOrWhiteSpace(userContext.Name))
        {
            throw new ValidationException("Name is required");
        }

        if (string.IsNullOrWhiteSpace(userContext.Email))
        {
            throw new ValidationException("Email is required");
        }

        if (!IsValidEmail(userContext.Email))
        {
            throw new ValidationException($"Invalid email format: {userContext.Email}");
        }

        if (userContext.Roles == null || !userContext.Roles.Any())
        {
            throw new ValidationException("At least one role is required");
        }

        // Validate roles
        var validRoles = new[] { "admin", "user", "driver", "dispatcher", "manager", "analyst" };
        foreach (var role in userContext.Roles)
        {
            if (!validRoles.Contains(role.ToLowerInvariant()))
            {
                throw new ValidationException($"Invalid role: {role}");
            }
        }
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
