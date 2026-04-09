using Microsoft.AspNetCore.Identity;
using VolunteerHub.Application.Abstractions;
using VolunteerHub.Application.Common;
using VolunteerHub.Contracts.Responses;
using VolunteerHub.Contracts.Constants;
using VolunteerHub.Contracts.Requests;
using VolunteerHub.Domain.Entities;
using VolunteerHub.Infrastructure.Authentication;

namespace VolunteerHub.Infrastructure.Identity;

public class AccountService : IAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly INotificationService _notificationService;
    private readonly IVolunteerProfileRepository _volunteerProfileRepository;
    private readonly IOrganizerRepository _organizerRepository;
    private readonly ISponsorRepository _sponsorRepository;
    private readonly IAdminAuditService _adminAuditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtTokenGenerator _jwtTokenGenerator;

    public AccountService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        INotificationService notificationService,
        IVolunteerProfileRepository volunteerProfileRepository,
        IOrganizerRepository organizerRepository,
        ISponsorRepository sponsorRepository,
        IAdminAuditService adminAuditService,
        IUnitOfWork unitOfWork,
        JwtTokenGenerator jwtTokenGenerator)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _notificationService = notificationService;
        _volunteerProfileRepository = volunteerProfileRepository;
        _organizerRepository = organizerRepository;
        _sponsorRepository = sponsorRepository;
        _adminAuditService = adminAuditService;
        _unitOfWork = unitOfWork;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var allowedRoles = new[]
        {
            AppRoles.Volunteer,
            AppRoles.Organizer,
            AppRoles.Sponsor
        };

        var normalizedRole = allowedRoles
            .FirstOrDefault(x => x.Equals(request.Role, StringComparison.OrdinalIgnoreCase));

        if (normalizedRole is null)
        {
            return Result.Failure(new Error(
                "Auth.InvalidRole",
                "Role must be Volunteer, Organizer, or Sponsor."));
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(new Error("Auth.UserRegistrationFailed", errors));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, normalizedRole);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
            return Result.Failure(new Error("Auth.AssignRoleFailed", errors));
        }

        var bootstrapResult = await BootstrapProfileAsync(user, normalizedRole, cancellationToken);
        if (!bootstrapResult.IsSuccess)
        {
            await _userManager.RemoveFromRoleAsync(user, normalizedRole);
            await _userManager.DeleteAsync(user);
            return bootstrapResult;
        }

        var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationLink = BuildCallbackLink(
            request.ConfirmationCallbackUrl,
            ("userId", user.Id.ToString()),
            ("token", confirmationToken));

        await _notificationService.SendEmailConfirmationNotificationAsync(
            user.Id,
            user.Email!,
            confirmationLink,
            cancellationToken);

        await _notificationService.SendWelcomeNotificationAsync(
            user.Id,
            user.Email!,
            request.FirstName,
            cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
            return Result.Failure(Error.NotFound);

        var result = await _userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(new Error("Auth.EmailConfirmationFailed", errors));
        }

        return Result.Success();
    }

    public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive || !user.EmailConfirmed)
            return Result.Success();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink = BuildCallbackLink(
            request.ResetPasswordCallbackUrl,
            ("email", user.Email ?? string.Empty),
            ("token", token));

        await _notificationService.SendPasswordResetNotificationAsync(
            user.Id,
            user.Email!,
            resetLink,
            cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return Result.Failure(Error.NotFound);

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(new Error("Auth.PasswordResetFailed", errors));
        }

        return Result.Success();
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return Result.Failure<LoginResponse>(Error.InvalidCredentials);

        if (!user.IsActive)
            return Result.Failure<LoginResponse>(new Error("Auth.UserInactive", "This account has been disabled."));

        var result = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
            return Result.Failure<LoginResponse>(new Error("Auth.UserLocked", "This account is locked."));

        if (result.IsNotAllowed)
            return Result.Failure<LoginResponse>(new Error("Auth.EmailNotConfirmed", "Email confirmation is required before logging in."));

        if (!result.Succeeded)
        {
            return Result.Failure<LoginResponse>(Error.InvalidCredentials);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var response = _jwtTokenGenerator.Generate(user, roles.ToList());

        return Result.Success(response);
    }

    public async Task<Result> LogoutAsync(CancellationToken cancellationToken = default)
    {
        await _signInManager.SignOutAsync();
        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result.Failure(Error.NotFound);
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(new Error("Auth.PasswordChangeFailed", errors));
        }

        return Result.Success();
    }

    public async Task<Result> LockUserAsync(Guid adminUserId, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return Result.Failure(Error.NotFound);

        user.IsActive = false;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            return Result.Failure(new Error("Auth.UserLockFailed", errors));
        }

        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        await _adminAuditService.LogAsync(adminUserId, "User.Lock", nameof(ApplicationUser), user.Id, $"Locked user {user.Email}.", cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UnlockUserAsync(Guid adminUserId, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return Result.Failure(Error.NotFound);

        user.IsActive = true;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            return Result.Failure(new Error("Auth.UserUnlockFailed", errors));
        }

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);
        await _adminAuditService.LogAsync(adminUserId, "User.Unlock", nameof(ApplicationUser), user.Id, $"Unlocked user {user.Email}.", cancellationToken);
        return Result.Success();
    }

    private async Task<Result> BootstrapProfileAsync(ApplicationUser user, string role, CancellationToken cancellationToken)
    {
        var displayName = $"{user.FirstName} {user.LastName}".Trim();

        switch (role)
        {
            case AppRoles.Volunteer:
                if (!await _volunteerProfileRepository.ExistsForUserAsync(user.Id, cancellationToken))
                {
                    _volunteerProfileRepository.Add(new VolunteerProfile
                    {
                        UserId = user.Id,
                        FullName = displayName
                    });
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                break;

            case AppRoles.Organizer:
                if (!await _organizerRepository.ExistsForUserAsync(user.Id, cancellationToken))
                {
                    _organizerRepository.Add(new OrganizerProfile
                    {
                        UserId = user.Id,
                        OrganizationName = displayName,
                        Email = user.Email ?? string.Empty,
                        VerificationStatus = OrganizerVerificationStatus.Pending
                    });
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                break;

            case AppRoles.Sponsor:
                if (!await _sponsorRepository.SponsorProfileExistsForUserAsync(user.Id, cancellationToken))
                {
                    _sponsorRepository.AddSponsorProfile(new SponsorProfile
                    {
                        UserId = user.Id,
                        CompanyName = displayName,
                        Email = user.Email ?? string.Empty,
                        Status = SponsorProfileStatus.PendingApproval
                    });
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                break;

            default:
                return Result.Failure(new Error("Auth.InvalidRole", "Unsupported bootstrap role."));
        }

        return Result.Success();
    }

    private static string BuildCallbackLink(string? callbackUrl, params (string Key, string Value)[] queryParams)
    {
        if (string.IsNullOrWhiteSpace(callbackUrl))
        {
            return string.Join("&", queryParams.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));
        }

        var separator = callbackUrl.Contains('?') ? "&" : "?";
        var query = string.Join("&", queryParams.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));
        return $"{callbackUrl}{separator}{query}";
    }
}
