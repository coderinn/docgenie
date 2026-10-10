using System.Security.Claims;
using LeadsPortal.Data;
using LeadsPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LeadsPortal.Authorization;

public sealed record OrganizationRoleRequirement(OrganizationRole MinimumRole) : IAuthorizationRequirement;

public sealed class OrganizationRoleAuthorizationHandler(
    DocGenieDbContext dbContext,
    IOrganizationContext organizationContext,
    IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<OrganizationRoleRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrganizationRoleRequirement requirement)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (httpContext is null
            || !Guid.TryParse(userIdClaim, out var userId)
            || !Guid.TryParse(Convert.ToString(httpContext.Request.RouteValues["orgId"]), out var routeOrganizationId)
            || routeOrganizationId != organizationContext.OrganizationId)
        {
            return;
        }

        var isActiveOrganization = await dbContext.Organizations
            .AsNoTracking()
            .AnyAsync(organization => organization.Id == routeOrganizationId && organization.IsActive);
        if (!isActiveOrganization)
        {
            return;
        }

        var membership = await dbContext.OrganizationMembers
            .AsNoTracking()
            .SingleOrDefaultAsync(member => member.OrganizationId == routeOrganizationId && member.UserId == userId);

        if (membership is not null && membership.Role >= requirement.MinimumRole)
        {
            context.Succeed(requirement);
        }
    }
}
