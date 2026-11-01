using AutoMapper;
using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Interfaces;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Domain.Models.Entities;
using ClinicSystem.Domain.Models.Identity;
using Microsoft.AspNetCore.Identity;

namespace ClinicSystem.Application.Implementations
{
    public class DoctorService : IDoctorService
    {
        private readonly IDoctorRepository _doctorRepository;
        private readonly IPersonRepository _personRepository;
        private readonly ISpecialtyRepository _specialtyRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public DoctorService(
            IDoctorRepository doctorRepository,
            IPersonRepository personRepository,
            ISpecialtyRepository specialtyRepository,
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _doctorRepository = doctorRepository;
            _personRepository = personRepository;
            _specialtyRepository = specialtyRepository;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ICollection<DoctorDTO>> GetAllAsync(int? specialtyId, CancellationToken cancellationToken)
        {
            var doctors = await _doctorRepository.GetAllWithDetailsAsync(specialtyId, cancellationToken);
            return doctors.Select(_mapper.Map<DoctorDTO>).ToList();
        }

        public async Task<DoctorDTO?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var doctor = await _doctorRepository.GetWithDetailsAsync(id, cancellationToken);
            if (doctor == null)
                return null;

            return _mapper.Map<DoctorDTO>(doctor);
        }

        public async Task<DoctorDTO> AddAsync(CreateDoctorDTO dto, CancellationToken cancellationToken)
        {
            var firstName = ValidateName(dto.FirstName, "First name");
            var lastName = ValidateName(dto.LastName, "Last name");
            var phone = ValidatePhone(dto.Phone);
            ValidatePersonalNumber(dto.PersonalNumber);

            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new ArgumentException("Email is required");
            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new ArgumentException("Password is required");

            var email = dto.Email.Trim();

            if (!await _specialtyRepository.ExistsAsync(s => s.Id == dto.SpecialtyId, cancellationToken))
                throw new KeyNotFoundException("Specialty not found");

            if (await _personRepository.ExistsAsync(p => p.PersonalNumber == dto.PersonalNumber, cancellationToken))
                throw new InvalidOperationException("Person with this personal number already exists");

            if (await _userManager.FindByEmailAsync(email) != null)
                throw new InvalidOperationException("User with this email already exists");

            int doctorId;

            // ოთხი ნაბიჯი ერთ ტრანზაქციაში: ან ყველაფერი ჩაიწერება, ან არაფერი
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    Person = new Person
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        PersonalNumber = dto.PersonalNumber,
                        Phone = phone
                    }
                };

                // ეს ინახავს ანგარიშსაც და Person-საც
                var createResult = await _userManager.CreateAsync(user, dto.Password);
                if (!createResult.Succeeded)
                    throw new ArgumentException(string.Join(", ", createResult.Errors.Select(e => e.Description)));

                var roleResult = await _userManager.AddToRoleAsync(user, Roles.Doctor);
                if (!roleResult.Succeeded)
                    throw new InvalidOperationException(string.Join(", ", roleResult.Errors.Select(e => e.Description)));

                var doctor = new Doctor
                {
                    UserId = user.Id,
                    SpecialtyId = dto.SpecialtyId
                };

                await _doctorRepository.AddAsync(doctor, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                doctorId = doctor.Id;
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }

            var created = await _doctorRepository.GetWithDetailsAsync(doctorId, cancellationToken);
            return _mapper.Map<DoctorDTO>(created);
        }

        public async Task UpdateAsync(int id, UpdateDoctorDTO dto, CancellationToken cancellationToken)
        {
            var doctor = await _doctorRepository.GetWithDetailsAsync(id, cancellationToken);
            if (doctor == null)
                throw new KeyNotFoundException("Doctor not found");

            var firstName = ValidateName(dto.FirstName, "First name");
            var lastName = ValidateName(dto.LastName, "Last name");
            var phone = ValidatePhone(dto.Phone);

            var specialty = await _specialtyRepository.GetByIdAsync(dto.SpecialtyId, cancellationToken);
            if (specialty == null)
                throw new KeyNotFoundException("Specialty not found");

            // ეს ობიექტები უკვე თვალყურდევნებულია, ამიტომ Update() არ გვჭირდება
            var person = doctor.User.Person;
            person.FirstName = firstName;
            person.LastName = lastName;
            person.Phone = phone;
            doctor.Specialty = specialty;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken)
        {
            var doctor = await _doctorRepository.GetByIdAsync(id, cancellationToken);
            if (doctor == null)
                throw new KeyNotFoundException("Doctor not found");

            doctor.IsActive = isActive;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static string ValidateName(string? value, string field)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{field} is required");

            var trimmed = value.Trim();
            if (trimmed.Length > 50)
                throw new ArgumentException($"{field} must be at most 50 characters");

            return trimmed;
        }

        private static string? ValidatePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            var trimmed = phone.Trim();
            if (trimmed.Length > 20)
                throw new ArgumentException("Phone must be at most 20 characters");

            return trimmed;
        }

        private static void ValidatePersonalNumber(string? personalNumber)
        {
            if (personalNumber == null
                || personalNumber.Length != 11
                || !personalNumber.All(c => c >= '0' && c <= '9'))
                throw new ArgumentException("Personal number must be exactly 11 digits");
        }
    }
}