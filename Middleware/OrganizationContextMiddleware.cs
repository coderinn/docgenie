using LeadsPortal.Data;

namespace LeadsPortal.Middleware;

public sealed class OrganizationContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IOrganizationContext organizationContext)
    {
        if (context.Request.RouteValues.TryGetValue("orgId", out var routeValue)
            && Guid.TryParse(Convert.ToString(routeValue), out var organizationId))
        {
            organizationContext.SetOrganization(organizationId);
        }

        await next(context);
    }
}
