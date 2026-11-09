using ClinicSystem.Application.Interfaces;
using ClinicSystem.Application.Exceptions;
using ClinicSystem.Application.Validation;
using ClinicSystem.Domain.Interfaces;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Domain.Models.Entities;
using ClinicSystem.Domain.Models.Identity;
using Microsoft.AspNetCore.Identity;

namespace ClinicSystem.Application.Implementations
{
    public class StaffService : IStaffService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPersonRepository _personRepository;
        private readonly IUnitOfWork _unitOfWork;

        public StaffService(
            UserManager<ApplicationUser> userManager,
            IPersonRepository personRepository,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _personRepository = personRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ICollection<StaffDTO>> GetStaffAsync(string role, CancellationToken cancellationToken)
        {
            EnsureStaffRole(role);

            var users = await _userManager.GetUsersInRoleAsync(role);
            var result = new List<StaffDTO>();

            foreach (var user in users)
            {
                var person = await _personRepository.GetByIdAsync(user.PersonId, cancellationToken);
                if (person == null)
                    continue;

                result.Add(ToDto(user, person, role));
            }

            return result.OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToList();
        }

        public async Task<StaffDTO> AddStaffAsync(string role, CreateStaffDTO dto, CancellationToken cancellationToken)
        {
            EnsureStaffRole(role);

            var firstName = PersonValidation.ValidateName(dto.FirstName, "First name");
            var lastName = PersonValidation.ValidateName(dto.LastName, "Last name");
            var phone = PersonValidation.ValidatePhone(dto.Phone);
            PersonValidation.ValidatePersonalNumber(dto.PersonalNumber);

            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new ArgumentException("Email is required");
            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new ArgumentException("Password is required");

            var email = dto.Email.Trim();

            if (await _personRepository.ExistsAsync(p => p.PersonalNumber == dto.PersonalNumber, cancellationToken))
                throw new InvalidOperationException("Person with this personal number already exists");

            if (await _userManager.FindByEmailAsync(email) != null)
                throw new InvalidOperationException("User with this email already exists");

            // ანგარიში და როლი ერთ ტრანზაქციაში: ან ორივე ჩაიწერება, ან არც ერთი
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var person = new Person
                {
                    FirstName = firstName,
                    LastName = lastName,
                    PersonalNumber = dto.PersonalNumber,
                    Phone = phone
                };

                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    Person = person
                };

                var createResult = await _userManager.CreateAsync(user, dto.Password);
                if (!createResult.Succeeded)
                    throw new ArgumentException(string.Join(", ", createResult.Errors.Select(e => e.Description)));

                EnsureSucceeded(await _userManager.AddToRoleAsync(user, role));

                await _unitOfWork.CommitAsync(cancellationToken);

                return ToDto(user, person, role);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task SetStaffActiveAsync(string userId, bool isActive, string currentUserId, bool currentUserIsSuperAdmin, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("Staff member not found");

            var isAdmin = await _userManager.IsInRoleAsync(user, Roles.Admin);
            var isReceptionist = await _userManager.IsInRoleAsync(user, Roles.Receptionist);

            // ამ მეთოდით მხოლოდ ადმინისა და რეგისტრატორის მართვა შეიძლება
            if (!isAdmin && !isReceptionist)
                throw new KeyNotFoundException("Staff member not found");

            if (isAdmin && !currentUserIsSuperAdmin)
                throw new ForbiddenException("Only a super administrator can manage administrator accounts");

            if (!isActive)
            {
                if (user.Id == currentUserId)
                    throw new InvalidOperationException("You cannot deactivate your own account");

                // დამატებითი დაცვა: სისტემა უადმინოდ არ უნდა დარჩეს
                if (isAdmin && !IsLocked(user))
                {
                    var admins = await _userManager.GetUsersInRoleAsync(Roles.Admin);
                    if (admins.Count(a => !IsLocked(a)) <= 1)
                        throw new InvalidOperationException("Cannot deactivate the last active administrator");
                }

                EnsureSucceeded(await _userManager.SetLockoutEnabledAsync(user, true));
                EnsureSucceeded(await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue));
            }
            else
            {
                EnsureSucceeded(await _userManager.SetLockoutEndDateAsync(user, null));
                EnsureSucceeded(await _userManager.ResetAccessFailedCountAsync(user));
            }
        }

        private static bool IsLocked(ApplicationUser user)
        {
            return user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
        }

        private static void EnsureStaffRole(string role)
        {
            if (role != Roles.Admin && role != Roles.Receptionist)
                throw new ArgumentException("Role must be Admin or Receptionist");
        }

        private static void EnsureSucceeded(IdentityResult result)
        {
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        private static StaffDTO ToDto(ApplicationUser user, Person person, string role)
        {
            return new StaffDTO
            {
                UserId = user.Id,
                FirstName = person.FirstName,
                LastName = person.LastName,
                Phone = person.Phone,
                Email = user.Email ?? string.Empty,
                Role = role,
                IsActive = !IsLocked(user)
            };
        }
    }
}
