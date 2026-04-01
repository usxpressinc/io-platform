using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IO.Common.Core.Users;
using IO.Common.Infrastructure.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IO.Common.Endpoints;

/// <summary>
/// User context endpoints for IO.Common service
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserContextController : ControllerBase
{
    private readonly IUserContextBusinessService _userContextService;
    private readonly ILogger<UserContextController> _logger;

    public UserContextController(
        IUserContextBusinessService userContextService,
        ILogger<UserContextController> logger)
    {
        _userContextService = userContextService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new user context
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            _logger.LogInformation("Creating user context for {UserId}", request.UserId);

            var userContext = new UserContext
            {
                UserId = request.UserId,
                Name = request.Name,
                Email = request.Email,
                Roles = request.Roles ?? new List<string> { "user" },
                Preferences = request.Preferences ?? new Dictionary<string, object>(),
                Metadata = request.Metadata ?? new Dictionary<string, object>()
            };

            var createdUser = await _userContextService.CreateAsync(userContext);

            return CreatedAtAction(
                nameof(GetUser),
                new { userId = createdUser.UserId },
                createdUser);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error creating user");
            return BadRequest(new { error = ex.Message });
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict creating user");
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get user context by user ID
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(string userId)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { error = "User ID is required" });
            }

            _logger.LogInformation("Getting user context for {UserId}", userId);

            var userContext = await _userContextService.GetByUserIdAsync(userId);

            if (userContext == null)
            {
                return NotFound(new { error = "User not found" });
            }

            return Ok(userContext);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error getting user");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get user context by email
    /// </summary>
    [HttpGet("by-email/{email}")]
    public async Task<IActionResult> GetUserByEmail(string email)
    {
        try
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { error = "Email is required" });
            }

            _logger.LogInformation("Getting user context by email {Email}", email);

            var userContext = await _userContextService.GetByEmailAsync(email);

            if (userContext == null)
            {
                return NotFound(new { error = "User not found" });
            }

            return Ok(userContext);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error getting user by email");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update user context
    /// </summary>
    [HttpPut("{userId}")]
    public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { error = "User ID is required" });
            }

            _logger.LogInformation("Updating user context for {UserId}", userId);

            var userContext = await _userContextService.GetByUserIdAsync(userId);
            if (userContext == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Update fields
            userContext.Name = request.Name ?? userContext.Name;
            userContext.Email = request.Email ?? userContext.Email;
            userContext.Roles = request.Roles ?? userContext.Roles;
            userContext.Preferences = request.Preferences ?? userContext.Preferences;
            userContext.Metadata = request.Metadata ?? userContext.Metadata;

            var success = await _userContextService.UpdateAsync(userContext);

            if (success)
            {
                return Ok(userContext);
            }
            else
            {
                return StatusCode(500, new { error = "Failed to update user" });
            }
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating user");
            return BadRequest(new { error = ex.Message });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found for update");
            return NotFound(new { error = ex.Message });
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict updating user");
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Add role to user
    /// </summary>
    [HttpPost("{userId}/roles/{role}")]
    public async Task<IActionResult> AddRole(string userId, string role)
    {
        try
        {
            _logger.LogInformation("Adding role {Role} to user {UserId}", role, userId);

            var success = await _userContextService.AddRoleAsync(userId, role);

            if (success)
            {
                return Ok(new { message = $"Role {role} added successfully" });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to add role" });
            }
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error adding role");
            return BadRequest(new { error = ex.Message });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found for adding role");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding role");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Remove role from user
    /// </summary>
    [HttpDelete("{userId}/roles/{role}")]
    public async Task<IActionResult> RemoveRole(string userId, string role)
    {
        try
        {
            _logger.LogInformation("Removing role {Role} from user {UserId}", role, userId);

            var success = await _userContextService.RemoveRoleAsync(userId, role);

            if (success)
            {
                return Ok(new { message = $"Role {role} removed successfully" });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to remove role" });
            }
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error removing role");
            return BadRequest(new { error = ex.Message });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found for removing role");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update user preferences
    /// </summary>
    [HttpPut("{userId}/preferences")]
    public async Task<IActionResult> UpdatePreferences(string userId, [FromBody] Dictionary<string, object> preferences)
    {
        try
        {
            _logger.LogInformation("Updating preferences for user {UserId}", userId);

            var success = await _userContextService.UpdatePreferencesAsync(userId, preferences);

            if (success)
            {
                return Ok(new { message = "Preferences updated successfully" });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to update preferences" });
            }
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating preferences");
            return BadRequest(new { error = ex.Message });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found for updating preferences");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating preferences");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get users by role
    /// </summary>
    [HttpGet("by-role/{role}")]
    public async Task<IActionResult> GetUsersByRole(string role, [FromQuery] int limit = 100)
    {
        try
        {
            _logger.LogInformation("Getting users with role {Role}", role);

            var users = await _userContextService.GetByRoleAsync(role, limit);

            return Ok(new { 
                users,
                count = users.Count
            });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error getting users by role");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users by role");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Search users
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers([FromQuery] string q, [FromQuery] int limit = 50)
    {
        try
        {
            if (string.IsNullOrEmpty(q))
            {
                return BadRequest(new { error = "Search query is required" });
            }

            _logger.LogInformation("Searching users with query '{Query}'", q);

            var users = await _userContextService.SearchAsync(q, limit);

            return Ok(new { 
                users,
                count = users.Count,
                query = q
            });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error searching users");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete user
    /// </summary>
    [HttpDelete("{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        try
        {
            _logger.LogInformation("Deleting user {UserId}", userId);

            var success = await _userContextService.DeleteAsync(userId);

            if (success)
            {
                return Ok(new { message = "User deleted successfully" });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to delete user" });
            }
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error deleting user");
            return BadRequest(new { error = ex.Message });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found for deletion");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update last login time
    /// </summary>
    [HttpPost("{userId}/login")]
    public async Task<IActionResult> UpdateLastLogin(string userId)
    {
        try
        {
            _logger.LogInformation("Updating last login for user {UserId}", userId);

            var success = await _userContextService.UpdateLastLoginAsync(userId);

            if (success)
            {
                return Ok(new { message = "Last login updated successfully" });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to update last login" });
            }
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating last login");
            return BadRequest(new { error = ex.Message });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found for updating last login");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last login");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

/// <summary>
/// Create user request
/// </summary>
public class CreateUserRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string>? Roles { get; set; }
    public Dictionary<string, object>? Preferences { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Update user request
/// </summary>
public class UpdateUserRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public List<string>? Roles { get; set; }
    public Dictionary<string, object>? Preferences { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
