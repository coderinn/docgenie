# DocGenie

DocGenie is an ASP.NET Core MVC application with a MySQL-backed, multi-tenant document-generation API. The repository currently targets .NET 10, so the EF Core, Identity, and MySQL provider packages are aligned to .NET 10. Use MySQL 8.0.16 or later so the schema's check constraints are enforced.

## Local configuration

Set a MySQL connection string and a JWT signing key in user-secrets or the environment before starting the app. The key must contain at least 32 UTF-8 bytes. For a local MySQL account, configure the connection string with your own username and password:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=DocGenie;User ID=docgenie;Password=<local-password>;"
dotnet user-secrets set "Authentication:Jwt:SigningKey" "<generate-a-random-secret-of-at-least-32-bytes>"
dotnet run
```

Do not put database passwords or signing keys in tracked JSON files.

## EF Core migrations

Restore the pinned local EF tool, then add and apply a migration:

```powershell
dotnet tool restore
$env:ConnectionStrings__DefaultConnection = "Server=localhost;Port=3306;Database=DocGenie;User ID=docgenie;Password=<local-password>;"
dotnet ef migrations add InitialMySqlSchema --context DocGenieDbContext
dotnet ef database update --context DocGenieDbContext
```

`DocGenieDbContextFactory` makes EF tooling independent of the HTTP host and JWT configuration; the configured connection string is required by the MySQL provider. This migration creates the schema in MySQL. In deployed environments, use a managed secret store for the database connection string and JWT signing key.

## Authentication and tenant APIs

All API authentication uses short-lived JWT bearer tokens. `POST /api/auth/register` and `POST /api/auth/login` return an access token. Send it as `Authorization: Bearer <token>`. `GET /api/organizations` lists only organizations the authenticated user belongs to.

- `POST /api/organizations` creates an organization and gives the caller the `Owner` role.
- `GET /api/organizations/{orgId}` requires organization membership.
- `POST /api/organizations/{orgId}/invite` lets an Owner invite any role; an Admin can invite Members and Viewers.
- `POST /api/organizations/{orgId}/invitations/{invitationId}/accept` accepts an invite for the authenticated account whose email matches the invitation.

An invite token is single-use, expires after seven days, and is stored in the database only as a SHA-256 hash. Until an email provider is configured, the invite endpoint returns the raw token once to the authorized caller so it can be delivered through a trusted channel. Do not log or persist the returned token in client telemetry.

## Tenant isolation

Tenant-owned EF Core sets have global query filters tied to the `orgId` route value. Organization role authorization checks the current user’s membership in that organization on each request; tenant roles are deliberately not embedded in JWTs. `GET /api/organizations` is the only filter bypass: it constrains results to the authenticated user’s membership rows.

Organizations are deactivated with `IsActive` instead of being hard-deleted. Foreign keys preserve template, job, invitation, and audit history; job rows cascade only with their generation job, and template versions cascade only with their template.
