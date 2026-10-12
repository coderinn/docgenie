using System.ComponentModel.DataAnnotations;
using LeadsPortal.Models;

namespace LeadsPortal.Contracts;

public sealed class CreateOrganizationRequest
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;
}

public sealed class InviteOrganizationMemberRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [EnumDataType(typeof(OrganizationRole))]
    public OrganizationRole Role { get; init; } = OrganizationRole.Member;
}

public sealed class AcceptOrganizationInvitationRequest
{
    [Required, StringLength(128, MinimumLength = 32)]
    public string Token { get; init; } = string.Empty;
}
