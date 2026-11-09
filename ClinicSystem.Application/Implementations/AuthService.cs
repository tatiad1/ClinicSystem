using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClinicSystem.Application.Interfaces;
using ClinicSystem.Application.Settings;
using ClinicSystem.Domain.Interfaces;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Domain.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ClinicSystem.Application.Implementations
{
    public class AuthService : IAuthService
    {
        private const string LockedMessage =
            "Account is locked or disabled. Try again later or contact the administrator.";

        private readonly JwtSettings _jwtSettings;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPatientService _patientService;
        private readonly IDoctorRepository _doctorRepository;
        private readonly IPatientRepository _patientRepository;

        public AuthService(
            IOptions<JwtSettings> jwtOptions,
            UserManager<ApplicationUser> userManager,
            IPatientService patientService,
            IDoctorRepository doctorRepository,
            IPatientRepository patientRepository)
        {
            _jwtSettings = jwtOptions.Value;
            _userManager = userManager;
            _patientService = patientService;
            _doctorRepository = doctorRepository;
            _patientRepository = patientRepository;
        }

        public async Task<AuthResponseDTO> LoginAsync(LoginDTO dto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                throw new ArgumentException("Email and password are required");

            var user = await _userManager.FindByEmailAsync(dto.Email.Trim());
            if (user == null)
                throw new UnauthorizedAccessException("Invalid email or password");

            // გათიშული ან დროებით დაბლოკილი ანგარიში პაროლის შემოწმებამდე ჩერდება
            if (await _userManager.IsLockedOutAsync(user))
                throw new UnauthorizedAccessException(LockedMessage);

            if (!await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                // არასწორი ცდა ითვლება; 5 ცდის შემდეგ ანგარიში 15 წუთით იბლოკება
                await _userManager.AccessFailedAsync(user);
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            var roles = await _userManager.GetRolesAsync(user);

            // ეს ანგარიში ექიმია ან პაციენტია? (ადმინს და Receptionist-ს არც ერთი არ აქვს)
            var doctor = await _doctorRepository.FirstOrDefaultAsync(d => d.UserId == user.Id, cancellationToken);
            if (doctor != null && !doctor.IsActive)
                throw new UnauthorizedAccessException(LockedMessage);

            var patient = await _patientRepository.FirstOrDefaultAsync(p => p.PersonId == user.PersonId, cancellationToken);

            return GenerateToken(user, roles, doctor?.Id, patient?.Id);
        }

        public async Task<PatientDTO> RegisterPatientAsync(RegisterDTO dto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                throw new ArgumentException("Email and password are required");

            // იგივე ლოგიკა, რაც პაციენტის შექმნაზე: Person + ანგარიში + Patient როლი + Patient
            return await _patientService.AddAsync(new CreatePatientDTO
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PersonalNumber = dto.PersonalNumber,
                Phone = dto.Phone,
                BirthDate = dto.BirthDate,
                Email = dto.Email,
                Password = dto.Password,
                FamilyDoctorId = null
            }, cancellationToken);
        }

        public async Task ChangePasswordAsync(string userId, ChangePasswordDTO dto, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new KeyNotFoundException("User not found");

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
                throw new ArgumentException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        private AuthResponseDTO GenerateToken(ApplicationUser user, IList<string> roles, int? doctorId, int? patientId)
        {
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            if (doctorId.HasValue)
                claims.Add(new Claim(ClinicClaims.DoctorId, doctorId.Value.ToString()));

            if (patientId.HasValue)
                claims.Add(new Claim(ClinicClaims.PatientId, patientId.Value.ToString()));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            return new AuthResponseDTO
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAt = expiresAt,
                Email = user.Email ?? string.Empty,
                Roles = roles
            };
        }
    }
}