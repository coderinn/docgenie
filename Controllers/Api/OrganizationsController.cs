using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LeadsPortal.Authorization;
using LeadsPortal.Contracts;
using LeadsPortal.Data;
using LeadsPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace LeadsPortal.Controllers.Api;

[ApiController]
[Route("api/organizations")]
[Authorize]
public sealed partial class OrganizationsController(
    DocGenieDbContext dbContext,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        // Listing organizations is the one cross-tenant lookup: the predicate is limited to
        // the authenticated user's own membership rows, so it cannot expose another tenant's data.
        var organizations = await dbContext.OrganizationMembers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(member => member.UserId == userId && member.Organization.IsActive)
            .OrderBy(member => member.Organization.Name)
            .Select(member => new
            {
                member.OrganizationId,
                member.Organization.Name,
                member.Organization.Slug,
                member.Role
            })
            .ToListAsync();

        return Ok(organizations);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateOrganizationRequest request)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var name = request.Name.Trim();
        var organization = new Organization
        {
            Name = name,
            Slug = CreateSlug(name),
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = true
        };

        organization.Members.Add(new OrganizationMember
        {
            OrganizationId = organization.Id,
            UserId = userId,
            Role = OrganizationRole.Owner,
            JoinedAt = DateTimeOffset.UtcNow
        });

        dbContext.Organizations.Add(organization);
        dbContext.AuditLogs.Add(new AuditLog
        {
            OrganizationId = organization.Id,
            UserId = userId,
            Action = "Organization.Created",
            EntityName = nameof(Organization),
            EntityId = organization.Id.ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            DetailsJson = JsonSerializer.Serialize(new { organization.Name, organization.Slug })
        });

        await dbContext.SaveChangesAsync();

        return CreatedAtRoute("GetOrganization", new { orgId = organization.Id }, new
        {
            organization.Id,
            organization.Name,
            organization.Slug,
            organization.CreatedAt,
            organization.IsActive,
            Role = OrganizationRole.Owner.ToString()
        });
    }

    [HttpGet("{orgId:guid}", Name = "GetOrganization")]
    [Authorize(Policy = OrganizationPolicies.Viewer)]
    public async Task<IActionResult> Get(Guid orgId)
    {
        var organization = await dbContext.Organizations
            .AsNoTracking()
            .Where(item => item.Id == orgId)
            .Select(item => new { item.Id, item.Name, item.Slug, item.CreatedAt, item.IsActive })
            .SingleOrDefaultAsync();

        return organization is null ? NotFound() : Ok(organization);
    }

    [HttpPost("{orgId:guid}/invite")]
    [Authorize(Policy = OrganizationPolicies.Admin)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Invite(Guid orgId, InviteOrganizationMemberRequest request)
    {
        if (!Enum.IsDefined(request.Role)
            || request.Role >= OrganizationRole.Admin && !await IsCurrentUserOwnerAsync(orgId))
        {
            return Forbid();
        }

        if (!TryGetUserId(out var inviterUserId))
        {
            return Unauthorized();
        }

        var email = userManager.NormalizeEmail(request.Email.Trim());
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "A valid email address is required." });
        }

        var invitee = await userManager.FindByEmailAsync(email);
        if (invitee is not null && await dbContext.OrganizationMembers
                .AnyAsync(member => member.OrganizationId == orgId && member.UserId == invitee.Id))
        {
            return Conflict(new { message = "This user is already a member of the organization." });
        }

        var now = DateTimeOffset.UtcNow;
        var hasPendingInvitation = await dbContext.OrganizationInvitations.AnyAsync(invitation =>
            invitation.OrganizationId == orgId
            && invitation.Email == email
            && invitation.AcceptedAt == null
            && invitation.ExpiresAt > now);
        if (hasPendingInvitation)
        {
            return Conflict(new { message = "A pending invitation already exists for this email address." });
        }

        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var invitation = new OrganizationInvitation
        {
            OrganizationId = orgId,
            Email = email,
            Role = request.Role,
            InvitedByUserId = inviterUserId,
            TokenHash = HashToken(token),
            CreatedAt = now,
            ExpiresAt = now.AddDays(7)
        };

        dbContext.OrganizationInvitations.Add(invitation);
        dbContext.AuditLogs.Add(new AuditLog
        {
            OrganizationId = orgId,
            UserId = inviterUserId,
            Action = "OrganizationMember.Invited",
            EntityName = nameof(OrganizationInvitation),
            EntityId = invitation.Id.ToString(),
            Timestamp = now,
            DetailsJson = JsonSerializer.Serialize(new { invitation.Email, invitation.Role, invitation.ExpiresAt })
        });
        await dbContext.SaveChangesAsync();

        // Return the one-time token to the authorized inviter; a delivery provider can email it in a later step.
        return StatusCode(StatusCodes.Status201Created, new
        {
            invitation.Id,
            invitation.Email,
            invitation.Role,
            invitation.ExpiresAt,
            InvitationToken = token
        });
    }

    [HttpPost("{orgId:guid}/invitations/{invitationId:guid}/accept")]
    [Authorize]
    public async Task<IActionResult> AcceptInvitation(
        Guid orgId,
        Guid invitationId,
        AcceptOrganizationInvitationRequest request)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null || string.IsNullOrWhiteSpace(user.Email) || !TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var suppliedHash = HashToken(request.Token);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var invitation = await dbContext.OrganizationInvitations
            .SingleOrDefaultAsync(item => item.Id == invitationId && item.OrganizationId == orgId);
        if (invitation is null)
        {
            return NotFound();
        }

        if (!string.Equals(userManager.NormalizeEmail(user.Email), invitation.Email, StringComparison.Ordinal))
        {
            return Forbid();
        }

        var hashMatches = CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(invitation.TokenHash),
            Encoding.ASCII.GetBytes(suppliedHash));
        if (!hashMatches || invitation.AcceptedAt is not null || invitation.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return BadRequest(new { message = "The invitation is invalid, expired, or has already been accepted." });
        }

        var organizationIsActive = await dbContext.Organizations
            .AnyAsync(item => item.Id == orgId && item.IsActive);
        if (!organizationIsActive)
        {
            return NotFound();
        }

        var existingMember = await dbContext.OrganizationMembers
            .AnyAsync(member => member.OrganizationId == orgId && member.UserId == userId);
        if (existingMember)
        {
            return Conflict(new { message = "You are already a member of this organization." });
        }

        var now = DateTimeOffset.UtcNow;
        invitation.AcceptedAt = now;
        dbContext.OrganizationMembers.Add(new OrganizationMember
        {
            OrganizationId = orgId,
            UserId = userId,
            Role = invitation.Role,
            JoinedAt = now
        });
        dbContext.AuditLogs.Add(new AuditLog
        {
            OrganizationId = orgId,
            UserId = userId,
            Action = "OrganizationInvitation.Accepted",
            EntityName = nameof(OrganizationInvitation),
            EntityId = invitation.Id.ToString(),
            Timestamp = now,
            DetailsJson = JsonSerializer.Serialize(new { invitation.Role })
        });

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { OrganizationId = orgId, Role = invitation.Role.ToString() });
    }

    private async Task<bool> IsCurrentUserOwnerAsync(Guid organizationId)
    {
        if (!TryGetUserId(out var userId))
        {
            return false;
        }

        return await dbContext.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(member => member.OrganizationId == organizationId
                && member.UserId == userId
                && member.Role == OrganizationRole.Owner);
    }

    private bool TryGetUserId(out Guid userId)
    {
        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private static string CreateSlug(string name)
    {
        var slug = SlugSeparatorRegex().Replace(name.Trim().ToLowerInvariant(), "-").Trim('-');
        if (string.IsNullOrEmpty(slug))
        {
            slug = "organization";
        }

        if (slug.Length > 86)
        {
            slug = slug[..86].TrimEnd('-');
        }

        return $"{slug}-{Guid.NewGuid():N}";
    }

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex SlugSeparatorRegex();
}
