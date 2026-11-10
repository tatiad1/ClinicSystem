using AutoMapper;
using ClinicSystem.Application.Interfaces;
using ClinicSystem.Application.Validation;
using ClinicSystem.Domain.Interfaces;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Domain.Models.Entities;
using ClinicSystem.Domain.Models.Identity;
using Microsoft.AspNetCore.Identity;

namespace ClinicSystem.Application.Implementations
{
    public class PatientService : IPatientService
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IPersonRepository _personRepository;
        private readonly IDoctorRepository _doctorRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IAccountWelcomeEmailService _welcomeEmailService;

        public PatientService(
            IPatientRepository patientRepository,
            IPersonRepository personRepository,
            IDoctorRepository doctorRepository,
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IAccountWelcomeEmailService welcomeEmailService)
        {
            _patientRepository = patientRepository;
            _personRepository = personRepository;
            _doctorRepository = doctorRepository;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _welcomeEmailService = welcomeEmailService;
        }

        public async Task<ICollection<PatientDTO>> GetAllAsync(int? familyDoctorId, CancellationToken cancellationToken)
        {
            var patients = await _patientRepository.GetAllWithDetailsAsync(familyDoctorId, cancellationToken);
            return patients.Select(_mapper.Map<PatientDTO>).ToList();
        }

        public async Task<ICollection<PatientDTO>> GetAccessibleToDoctorAsync(
            int doctorId,
            CancellationToken cancellationToken)
        {
            var patients = await _patientRepository.GetAccessibleToDoctorAsync(doctorId, cancellationToken);
            return patients.Select(_mapper.Map<PatientDTO>).ToList();
        }

        public async Task<PatientDTO?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var patient = await _patientRepository.GetWithDetailsAsync(id, cancellationToken);
            if (patient == null)
                return null;

            return _mapper.Map<PatientDTO>(patient);
        }

        public async Task<PatientDTO> AddAsync(CreatePatientDTO dto, CancellationToken cancellationToken)
        {
            var firstName = PersonValidation.ValidateName(dto.FirstName, "First name");
            var lastName = PersonValidation.ValidateName(dto.LastName, "Last name");
            var phone = PersonValidation.ValidatePhone(dto.Phone);
            PersonValidation.ValidatePersonalNumber(dto.PersonalNumber);
            ValidateBirthDate(dto.BirthDate);

            // ანგარიში: ან email და პაროლი ერთად, ან არც ერთი
            string? email = null;
            var hasEmail = !string.IsNullOrWhiteSpace(dto.Email);
            var hasPassword = !string.IsNullOrWhiteSpace(dto.Password);

            if (hasEmail != hasPassword)
                throw new ArgumentException("Email and password must be provided together");

            if (hasEmail)
            {
                email = dto.Email!.Trim();
                if (await _userManager.FindByEmailAsync(email) != null)
                    throw new InvalidOperationException("User with this email already exists");
            }

            var contactEmail = string.IsNullOrWhiteSpace(dto.ContactEmail)
                ? email
                : dto.ContactEmail.Trim();

            int? familyDoctorId = null;
            if (dto.FamilyDoctorId.HasValue)
            {
                var doctor = await GetActiveDoctorAsync(dto.FamilyDoctorId.Value, cancellationToken);
                familyDoctorId = doctor.Id;
            }

            if (await _personRepository.ExistsAsync(p => p.PersonalNumber == dto.PersonalNumber, cancellationToken))
                throw new InvalidOperationException("Person with this personal number already exists");

            int patientId;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var person = new Person
                {
                    FirstName = firstName,
                    LastName = lastName,
                    PersonalNumber = dto.PersonalNumber,
                    Phone = phone,
                    ContactEmail = contactEmail
                };

                if (email != null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        Person = person
                    };

                    var createResult = await _userManager.CreateAsync(user, dto.Password!);
                    if (!createResult.Succeeded)
                        throw new ArgumentException(string.Join(", ", createResult.Errors.Select(e => e.Description)));

                    var roleResult = await _userManager.AddToRoleAsync(user, Roles.Patient);
                    if (!roleResult.Succeeded)
                        throw new InvalidOperationException(string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }

                var patient = new Patient
                {
                    Person = person,
                    BirthDate = dto.BirthDate,
                    FamilyDoctorId = familyDoctorId
                };

                await _patientRepository.AddAsync(patient, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                patientId = patient.Id;
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }

            if (email != null)
                await _welcomeEmailService.SendWelcomeEmailAsync(email, firstName, Roles.Patient);

            var created = await _patientRepository.GetWithDetailsAsync(patientId, cancellationToken);
            return _mapper.Map<PatientDTO>(created);
        }

        public async Task UpdateAsync(int id, UpdatePatientDTO dto, CancellationToken cancellationToken)
        {
            var patient = await _patientRepository.GetWithDetailsAsync(id, cancellationToken);
            if (patient == null)
                throw new KeyNotFoundException("Patient not found");

            var firstName = PersonValidation.ValidateName(dto.FirstName, "First name");
            var lastName = PersonValidation.ValidateName(dto.LastName, "Last name");
            var phone = PersonValidation.ValidatePhone(dto.Phone);
            ValidateBirthDate(dto.BirthDate);

            // ეს ობიექტები უკვე თვალყურდევნებულია, ამიტომ Update() არ გვჭირდება
            patient.Person.FirstName = firstName;
            patient.Person.LastName = lastName;
            patient.Person.Phone = phone;
            if (dto.ContactEmail != null)
            {
                patient.Person.ContactEmail = string.IsNullOrWhiteSpace(dto.ContactEmail)
                    ? null
                    : dto.ContactEmail.Trim();
            }
            patient.BirthDate = dto.BirthDate;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task AssignFamilyDoctorAsync(int id, int? doctorId, CancellationToken cancellationToken)
        {
            var patient = await _patientRepository.GetByIdAsync(id, cancellationToken);
            if (patient == null)
                throw new KeyNotFoundException("Patient not found");

            if (doctorId.HasValue)
            {
                var doctor = await GetActiveDoctorAsync(doctorId.Value, cancellationToken);
                patient.FamilyDoctorId = doctor.Id;
            }
            else
            {
                patient.FamilyDoctorId = null;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task<Doctor> GetActiveDoctorAsync(int doctorId, CancellationToken cancellationToken)
        {
            var doctor = await _doctorRepository.GetWithDetailsAsync(doctorId, cancellationToken);
            if (doctor == null)
                throw new KeyNotFoundException("Family doctor not found");

            if (!doctor.IsActive)
                throw new InvalidOperationException("Family doctor is not active");

            if (!doctor.Specialty.IsFamilyMedicine)
                throw new InvalidOperationException(
                    "Family doctor must belong to the Family Medicine specialty");

            return doctor;
        }


        public async Task<PatientDTO> CreateAccountAsync(int id, CreatePatientAccountDTO dto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new ArgumentException("Email is required");
            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new ArgumentException("Password is required");

            var patient = await _patientRepository.GetWithDetailsAsync(id, cancellationToken);
            if (patient == null)
                throw new KeyNotFoundException("Patient not found");

            if (patient.Person.User != null)
                throw new InvalidOperationException("Patient already has an account");

            var email = dto.Email.Trim();

            if (await _userManager.FindByEmailAsync(email) != null)
                throw new InvalidOperationException("User with this email already exists");

            // ანგარიში და როლი ერთ ტრანზაქციაში
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    PersonId = patient.PersonId
                };

                patient.Person.ContactEmail ??= email;

                var createResult = await _userManager.CreateAsync(user, dto.Password);
                if (!createResult.Succeeded)
                    throw new ArgumentException(string.Join(", ", createResult.Errors.Select(e => e.Description)));

                var roleResult = await _userManager.AddToRoleAsync(user, Roles.Patient);
                if (!roleResult.Succeeded)
                    throw new InvalidOperationException(string.Join(", ", roleResult.Errors.Select(e => e.Description)));

                await _unitOfWork.CommitAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }

            await _welcomeEmailService.SendWelcomeEmailAsync(email, patient.Person.FirstName, Roles.Patient);

            var updated = await _patientRepository.GetWithDetailsAsync(id, cancellationToken);
            return _mapper.Map<PatientDTO>(updated);
        }
        private static void ValidateBirthDate(DateOnly birthDate)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            if (birthDate > today)
                throw new ArgumentException("Birth date cannot be in the future");

            if (birthDate < today.AddYears(-120))
                throw new ArgumentException("Birth date is not valid");
        }


    }
}
