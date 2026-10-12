using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using LeadsPortal.Authorization;
using LeadsPortal.Data;
using LeadsPortal.Middleware;
using LeadsPortal.Models;
using LeadsPortal.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MySQL.EntityFrameworkCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection with a MySQL connection string before starting DocGenie.");
}

var jwtSigningKey = builder.Configuration["Authentication:Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(jwtSigningKey) || Encoding.UTF8.GetByteCount(jwtSigningKey) < 32)
{
    throw new InvalidOperationException("Configure Authentication:Jwt:SigningKey with at least 32 UTF-8 bytes.");
}

var jwtIssuer = builder.Configuration["Authentication:Jwt:Issuer"] ?? "DocGenie";
var jwtAudience = builder.Configuration["Authentication:Jwt:Audience"] ?? "DocGenie.Client";
var jwtAccessTokenMinutes = builder.Configuration.GetValue<int?>("Authentication:Jwt:AccessTokenMinutes") ?? 30;
if (jwtAccessTokenMinutes is < 5 or > 120)
{
    throw new InvalidOperationException("Authentication:Jwt:AccessTokenMinutes must be between 5 and 120.");
}
builder.Services.Configure<JwtOptions>(options =>
{
    options.Issuer = jwtIssuer;
    options.Audience = jwtAudience;
    options.SigningKey = jwtSigningKey;
    options.AccessTokenMinutes = jwtAccessTokenMinutes;
});

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddDbContext<DocGenieDbContext>(options => options.UseMySQL(connectionString));
builder.Services.AddScoped<IOrganizationContext, OrganizationContext>();
builder.Services.AddHttpContextAccessor();

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = false;
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<DocGenieDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(OrganizationPolicies.Viewer,
        policy => policy.RequireAuthenticatedUser().AddRequirements(new OrganizationRoleRequirement(OrganizationRole.Viewer)));
    options.AddPolicy(OrganizationPolicies.Member,
        policy => policy.RequireAuthenticatedUser().AddRequirements(new OrganizationRoleRequirement(OrganizationRole.Member)));
    options.AddPolicy(OrganizationPolicies.Admin,
        policy => policy.RequireAuthenticatedUser().AddRequirements(new OrganizationRoleRequirement(OrganizationRole.Admin)));
    options.AddPolicy(OrganizationPolicies.Owner,
        policy => policy.RequireAuthenticatedUser().AddRequirements(new OrganizationRoleRequirement(OrganizationRole.Owner)));
});

builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, OrganizationRoleAuthorizationHandler>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("authentication", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<OrganizationContextMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
