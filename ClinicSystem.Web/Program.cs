using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using ClinicSystem.Application.Implementations;
using ClinicSystem.Application.Interfaces;
using ClinicSystem.Application.Settings;
using ClinicSystem.Domain.Interfaces;
using ClinicSystem.Domain.Models.Entities;
using ClinicSystem.Domain.Models.Identity;
using ClinicSystem.Infrastructure;
using ClinicSystem.Infrastructure.Data;
using ClinicSystem.Infrastructure.Repositories;
using ClinicSystem.Infrastructure.Services;
using ClinicSystem.Web.BackgroundServices;
using ClinicSystem.Web.Filters;
using ClinicSystem.Web.Middleware;
using ClinicSystem.Web.Responses;
using ClinicSystem.Web.Seeding;
using ClinicSystem.Web.Services;
using ClinicSystem.Web.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers(options =>
{
    // ვალიდაცია ყველა action-მდე ერთი ფილტრით
    options.Filters.Add<ValidationFilter>();

    // სავალდებულო ველებს ჩვენი validator-ები ამოწმებენ და არა MVC-ის ჩაშენებული წესი
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
})
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    // არასწორი JSON (მაგ. თარიღი ტექსტია) ბრუნდება ApiResponse-ის ფორმით
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    e => e.Key,
                    e => e.Value!.Errors.Select(er => er.ErrorMessage).ToArray());

            var response = new ApiResponse<object>
            {
                Success = false,
                Message = "Validation error(s)",
                Data = null,
                Errors = errors
            };

            return new BadRequestObjectResult(response);
        };
    });

// FluentValidation: ყველა validator ერთად
builder.Services.AddValidatorsFromAssemblyContaining<CreateSpecialtyValidator>();

builder.Services.AddOpenApi();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Clinic API",
        Version = "v1"
    });

    // XML კომენტარები (/// summary) Swagger-ში
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Paste the JWT token here (without the word Bearer)",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

// CORS (frontend-ისთვის)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// DbContext
builder.Services.AddDbContext<ClinicDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity: 5 არასწორი ცდის შემდეგ ანგარიში 15 წუთით იბლოკება
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ClinicDbContext>();

// JWT პარამეტრები
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("Jwt settings are missing in appsettings.json");

if (string.IsNullOrWhiteSpace(jwtSettings.Key) || jwtSettings.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key is missing or shorter than 32 characters in appsettings.json");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role
    };

    // ყოველ მოთხოვნაზე: გათიშული ანგარიშის ან ექიმის ტოკენი მაშინვე წყვეტს მუშაობას
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var services = context.HttpContext.RequestServices;

            var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var user = userId == null ? null : await userManager.FindByIdAsync(userId);

            if (user == null || await userManager.IsLockedOutAsync(user))
            {
                context.Fail("Account is disabled.");
                return;
            }

            var doctorIdValue = context.Principal?.FindFirstValue(ClinicClaims.DoctorId);
            if (int.TryParse(doctorIdValue, out var doctorId))
            {
                var doctorRepository = services.GetRequiredService<IDoctorRepository>();
                var doctor = await doctorRepository.FirstOrDefaultAsync(d => d.Id == doctorId);

                if (doctor == null || !doctor.IsActive)
                    context.Fail("Doctor account is disabled.");
            }
        }
    };
});

// ყველა მოთხოვნას ლოგინი სჭირდება, გარდა იმათი, ვისაც [AllowAnonymous] აწერია
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// მიმდინარე მომხმარებელი (ტოკენიდან)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// შეხსენებების პარამეტრები
builder.Services.Configure<ReminderSettings>(builder.Configuration.GetSection("Reminders"));

// ელფოსტის პარამეტრები: პაროლი უნდა მოვიდეს საიდუმლო კონფიგურაციიდან,
// მაგალითად გარემოს ცვლადიდან Email__Password.
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
var emailSettings = builder.Configuration.GetSection("Email").Get<EmailSettings>() ?? new EmailSettings();
if (emailSettings.Enabled)
{
    if (string.IsNullOrWhiteSpace(emailSettings.SmtpHost)
        || emailSettings.SmtpPort <= 0
        || string.IsNullOrWhiteSpace(emailSettings.SenderEmail)
        || string.IsNullOrWhiteSpace(emailSettings.Username)
        || string.IsNullOrWhiteSpace(emailSettings.Password))
    {
        throw new InvalidOperationException(
            "Email delivery is enabled, but one or more Email settings are missing.");
    }
}

// Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// გარე მომსახურებები (ინტერფეისი Domain-შია, რეალიზაცია Infrastructure-ში)
if (emailSettings.Enabled)
    builder.Services.AddScoped<INotificationSender, SmtpNotificationSender>();
else
    builder.Services.AddScoped<INotificationSender, LoggingNotificationSender>();

builder.Services.AddSingleton<EmailNotificationQueue>();
builder.Services.AddSingleton<IEmailNotificationQueue>(sp =>
    sp.GetRequiredService<EmailNotificationQueue>());
builder.Services.AddHostedService<EmailNotificationWorker>();

// Repositories
builder.Services.AddScoped<ISpecialtyRepository, SpecialtyRepository>();
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IDoctorScheduleRepository, DoctorScheduleRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IMedicalProfileRepository, MedicalProfileRepository>();
builder.Services.AddScoped<IReferralRepository, ReferralRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IConsultationRepository, ConsultationRepository>();

// Services
builder.Services.AddScoped<ISpecialtyService, SpecialtyService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IDoctorScheduleService, DoctorScheduleService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IMedicalProfileService, MedicalProfileService>();
builder.Services.AddScoped<IReferralService, ReferralService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IConsultationService, ConsultationService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<IAppointmentReminderService, AppointmentReminderService>();
builder.Services.AddScoped<IAccountWelcomeEmailService, AccountWelcomeEmailService>();
builder.Services.AddScoped<DevelopmentDataSeeder>();

// ფონური სერვისი: ვიზიტამდე შეხსენებები
builder.Services.AddHostedService<AppointmentReminderWorker>();

// AutoMapper
builder.Services.AddAutoMapper(cfg => { },
    typeof(Program).Assembly,
    typeof(SpecialtyService).Assembly);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// როლების და პირველი ადმინის შექმნა, თუ ჯერ არ არსებობს
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { Roles.Patient, Roles.Doctor, Roles.Receptionist, Roles.Admin, Roles.SuperAdmin };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var adminEmail = builder.Configuration["SeedAdmin:Email"];
    var adminPassword = builder.Configuration["SeedAdmin:Password"];

    if (!string.IsNullOrWhiteSpace(adminEmail)
        && !string.IsNullOrWhiteSpace(adminPassword))
    {
        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                Person = new Person
                {
                    FirstName = "System",
                    LastName = "Admin",
                    PersonalNumber = "00000000000"
                }
            };

            var createResult = await userManager.CreateAsync(admin, adminPassword);
            if (!createResult.Succeeded)
                throw new InvalidOperationException(
                    $"Could not create the configured seed administrator: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
        }

        // The configured seed administrator is the initial super administrator.
        foreach (var role in new[] { Roles.Admin, Roles.SuperAdmin })
        {
            if (!await userManager.IsInRoleAsync(admin, role))
            {
                var roleResult = await userManager.AddToRoleAsync(admin, role);
                if (!roleResult.Succeeded)
                    throw new InvalidOperationException(
                        $"Could not assign the {role} role to the configured seed administrator: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
            }
        }
    }

    // Demo data is added only in Development and only when the clinic catalogue
    // is empty. Accounts are created through UserManager, with one shared demo password.
    if (app.Environment.IsDevelopment())
    {
        var demoSeeder = scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>();
        await demoSeeder.SeedIfEmptyAsync();
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
