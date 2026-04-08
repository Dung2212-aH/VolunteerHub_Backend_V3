using VolunteerHub.Application.Abstractions;
using VolunteerHub.Application.Common;
using VolunteerHub.Contracts.Requests;
using VolunteerHub.Contracts.Responses;
using VolunteerHub.Domain.Entities;

namespace VolunteerHub.Application.Services;

public class VolunteerProfileService : IVolunteerProfileService
{
    private readonly IVolunteerProfileRepository _profileRepository;
    private readonly IRecognitionRepository _recognitionRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VolunteerProfileService(IVolunteerProfileRepository profileRepository, IRecognitionRepository recognitionRepository, IAttendanceRepository attendanceRepository, IUnitOfWork unitOfWork)
    {
        _profileRepository = profileRepository;
        _recognitionRepository = recognitionRepository;
        _attendanceRepository = attendanceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<VolunteerProfileResponse>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetByUserIdWithDetailsAsync(userId, cancellationToken);
        if (profile == null) return Result.Failure<VolunteerProfileResponse>(Error.NotFound);

        profile.TotalVolunteerHours = (int)Math.Round(await _attendanceRepository.GetTotalApprovedHoursAsync(profile.Id, cancellationToken), MidpointRounding.AwayFromZero);
        _profileRepository.Update(profile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var certificates = await _recognitionRepository.GetMyCertificatesAsync(profile.Id, cancellationToken);
        var hoursByEvent = new Dictionary<Guid, double>();
        foreach (var certificate in certificates.Where(c => c.Status == CertificateStatus.Active))
        {
            hoursByEvent[certificate.EventId] = await _attendanceRepository.GetApprovedHoursForEventAsync(certificate.EventId, profile.Id, cancellationToken);
        }

        return Result.Success(MapToResponse(profile, certificates, hoursByEvent));
    }

    public async Task<Result> CreateProfileAsync(Guid userId, CreateProfileRequest request, CancellationToken cancellationToken = default)
    {
        if (await _profileRepository.ExistsForUserAsync(userId, cancellationToken))
            return Result.Failure(Error.ProfileAlreadyExists);

        var profile = new VolunteerProfile
        {
            UserId = userId,
            FullName = request.FullName,
            Phone = request.Phone,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Bio = request.Bio,
            BloodGroup = request.BloodGroup,
            Avatar = request.Avatar,
            LanguagesText = request.LanguagesText,
            InterestsText = request.InterestsText
        };
        UpdateSkills(profile, request.Skills);
        _profileRepository.Add(profile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetByUserIdWithDetailsAsync(userId, cancellationToken);
        if (profile == null) return Result.Failure(Error.NotFound);

        profile.FullName = request.FullName;
        profile.Phone = request.Phone;
        profile.Address = request.Address;
        profile.Latitude = request.Latitude;
        profile.Longitude = request.Longitude;
        profile.Bio = request.Bio;
        profile.BloodGroup = request.BloodGroup;
        profile.Avatar = request.Avatar;
        profile.LanguagesText = request.LanguagesText;
        profile.InterestsText = request.InterestsText;
        UpdateSkills(profile, request.Skills);

        _profileRepository.Update(profile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static void UpdateSkills(VolunteerProfile profile, List<string> skills)
    {
        profile.Skills.Clear();
        foreach (var name in skills.Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            profile.Skills.Add(new VolunteerSkill { Name = name, VolunteerProfileId = profile.Id });
        }
    }

    private static VolunteerProfileResponse MapToResponse(VolunteerProfile profile, List<Certificate> certificates, Dictionary<Guid, double> hoursByEvent)
    {
        return new VolunteerProfileResponse
        {
            Id = profile.Id,
            UserId = profile.UserId,
            FullName = profile.FullName,
            Phone = profile.Phone,
            Address = profile.Address,
            Latitude = profile.Latitude,
            Longitude = profile.Longitude,
            Bio = profile.Bio,
            BloodGroup = profile.BloodGroup,
            Avatar = profile.Avatar,
            TotalVolunteerHours = profile.TotalVolunteerHours,
            IsProfileComplete = !string.IsNullOrWhiteSpace(profile.FullName) && !string.IsNullOrWhiteSpace(profile.Phone) && !string.IsNullOrWhiteSpace(profile.Address) && profile.Skills.Any(),
            Skills = profile.Skills.Select(s => s.Name).ToList(),
            LanguagesText = profile.LanguagesText,
            InterestsText = profile.InterestsText,
            CompletedParticipations = certificates
                .Where(c => c.Status == CertificateStatus.Active)
                .OrderByDescending(c => c.IssuedAt)
                .Select(c => new CompletedParticipationHistoryResponse
                {
                    EventId = c.EventId,
                    EventTitle = c.Event?.Title ?? string.Empty,
                    CompletedAt = c.IssuedAt,
                    HoursEarned = hoursByEvent.GetValueOrDefault(c.EventId, 0),
                    CertificateId = c.Id,
                    CertificateNumber = c.CertificateNumber
                })
                .ToList()
        };
    }
}
