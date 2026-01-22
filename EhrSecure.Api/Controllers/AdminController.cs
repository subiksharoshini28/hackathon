using EhrSecure.Api.Infrastructure;
using EhrSecure.Api.Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EhrSecure.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        AppDbContext db,
        IAuditService audit)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
        _audit = audit;
    }

    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<object>>> Users()
    {
        var users = await _db.Users.AsNoTracking().OrderBy(x => x.Email).ToListAsync();
        var result = new List<object>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            result.Add(new { u.Id, u.Email, u.PatientId, roles = roles.ToArray() });
        }

        await _audit.LogAsync("ADMIN_USERS_READ", "admin/users");
        return result;
    }

    public sealed class SetUserRoleRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    [HttpPost("users/role")]
    public async Task<ActionResult> SetUserRole([FromBody] SetUserRoleRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return NotFound();
        }

        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            return BadRequest();
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            var remove = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!remove.Succeeded)
            {
                return BadRequest(new { errors = remove.Errors.Select(e => e.Description).ToArray() });
            }
        }

        var add = await _userManager.AddToRoleAsync(user, request.Role);
        if (!add.Succeeded)
        {
            return BadRequest(new { errors = add.Errors.Select(e => e.Description).ToArray() });
        }

        await _audit.LogAsync("ADMIN_USER_ROLE_SET", $"admin/users/role/{request.Role}", user.PatientId);

        return NoContent();
    }

    [HttpGet("audit-logs")]
    public async Task<ActionResult<IReadOnlyList<AuditLog>>> AuditLogs([FromQuery] Guid? patientId = null, [FromQuery] int take = 100)
    {
        take = Math.Clamp(take, 1, 500);

        IQueryable<AuditLog> query = _db.AuditLogs.AsNoTracking().OrderByDescending(x => x.TimestampUtc);
        if (patientId.HasValue)
        {
            query = query.Where(x => x.PatientId == patientId.Value);
        }

        var logs = await query.Take(take).ToListAsync();
        await _audit.LogAsync("AUDITLOG_READ", "admin/audit-logs", patientId);

        return logs;
    }
}
