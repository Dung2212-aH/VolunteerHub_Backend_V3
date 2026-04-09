using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using VolunteerHub.Application.Abstractions;
using VolunteerHub.Infrastructure.Authentication;
using VolunteerHub.Infrastructure.Identity;
using VolunteerHub.Infrastructure.Persistence;
using VolunteerHub.Infrastructure.Persistence.Repositories;

namespace VolunteerHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;
        }).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();

        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        services.Configure<JwtOptions>(jwtSection);

        var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
            string.IsNullOrWhiteSpace(jwtOptions.Audience) ||
            string.IsNullOrWhiteSpace(jwtOptions.SecretKey))
        {
            throw new InvalidOperationException("JWT configuration is missing. Set Jwt:Issuer, Jwt:Audience, and Jwt:SecretKey.");
        }

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IVolunteerProfileRepository, VolunteerProfileRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IOrganizerRepository, OrganizerRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IApplicationApprovalRepository, ApplicationApprovalRepository>();
        services.AddScoped<IRecognitionRepository, RecognitionRepository>();
        services.AddScoped<ISponsorRepository, SponsorRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IRatingRepository, RatingRepository>();
        services.AddScoped<IAdminRepository, AdminRepository>();

        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IVolunteerProfileService, VolunteerHub.Application.Services.VolunteerProfileService>();
        services.AddScoped<IEventService, VolunteerHub.Application.Services.EventService>();
        services.AddScoped<IOrganizerProfileService, VolunteerHub.Application.Services.OrganizerProfileService>();
        services.AddScoped<IOrganizerVerificationService, VolunteerHub.Application.Services.OrganizerVerificationService>();
        services.AddScoped<IAttendanceService, VolunteerHub.Application.Services.AttendanceService>();
        services.AddScoped<IEventApplicationService, VolunteerHub.Application.Services.EventApplicationService>();
        services.AddScoped<IApplicationReviewService, VolunteerHub.Application.Services.ApplicationReviewService>();
        services.AddScoped<ICertificateEligibilityService, VolunteerHub.Application.Services.CertificateEligibilityService>();
        services.AddScoped<ICertificateService, VolunteerHub.Application.Services.CertificateService>();
        services.AddScoped<IBadgeService, VolunteerHub.Application.Services.BadgeService>();
        services.AddScoped<IEmailSender, VolunteerHub.Infrastructure.Services.ConsoleEmailSender>();
        services.AddScoped<INotificationService, VolunteerHub.Application.Services.NotificationService>();
        services.AddScoped<IRatingService, VolunteerHub.Application.Services.RatingService>();
        services.AddScoped<IFeedbackService, VolunteerHub.Application.Services.FeedbackService>();
        services.AddScoped<ISponsorProfileService, VolunteerHub.Application.Services.SponsorProfileService>();
        services.AddScoped<ISponsorManagementService, VolunteerHub.Application.Services.SponsorManagementService>();
        services.AddScoped<IAdminAuditService, VolunteerHub.Application.Services.AdminAuditService>();
        services.AddScoped<ISkillCatalogService, VolunteerHub.Application.Services.SkillCatalogService>();
        services.AddScoped<IComplaintModerationService, VolunteerHub.Application.Services.ComplaintModerationService>();
        services.AddScoped<IImpactReportService, VolunteerHub.Application.Services.ImpactReportService>();
        services.AddScoped<JwtTokenGenerator>();
        return services;
    }
}
