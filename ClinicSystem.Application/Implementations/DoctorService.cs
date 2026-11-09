using AutoMapper;
using ClinicSystem.Application.Interfaces;
using ClinicSystem.Application.Validation;
using ClinicSystem.Domain.Enums;
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
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public DoctorService(
            IDoctorRepository doctorRepository,
            IPersonRepository personRepository,
            ISpecialtyRepository specialtyRepository,
            IAppointmentRepository appointmentRepository,
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _doctorRepository = doctorRepository;
            _personRepository = personRepository;
            _specialtyRepository = specialtyRepository;
            _appointmentRepository = appointmentRepository;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ICollection<DoctorDTO>> GetAllAsync(int? specialtyId, bool includeInactive, CancellationToken cancellationToken)
        {
            var doctors = await _doctorRepository.GetAllWithDetailsAsync(specialtyId, includeInactive, cancellationToken);
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
            var firstName = PersonValidation.ValidateName(dto.FirstName, "First name");
            var lastName = PersonValidation.ValidateName(dto.LastName, "Last name");
            var phone = PersonValidation.ValidatePhone(dto.Phone);
            PersonValidation.ValidatePersonalNumber(dto.PersonalNumber);

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

            var firstName = PersonValidation.ValidateName(dto.FirstName, "First name");
            var lastName = PersonValidation.ValidateName(dto.LastName, "Last name");
            var phone = PersonValidation.ValidatePhone(dto.Phone);

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

            // დეაქტივაცია იბლოკება, სანამ მომავალი აქტიური ვიზიტები არსებობს
            if (!isActive)
            {
                var now = DateTime.Now;

                if (await _appointmentRepository.ExistsAsync(a =>
                        a.DoctorId == id
                        && a.StartTime > now
                        && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved),
                        cancellationToken))
                    throw new InvalidOperationException(
                        "Doctor has upcoming appointments. Cancel or reassign them first");
            }

            var user = await _userManager.FindByIdAsync(doctor.UserId);

            // ექიმის ჩანაწერი და ანგარიში ერთად იცვლება
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                doctor.IsActive = isActive;

                if (user != null)
                {
                    if (isActive)
                    {
                        EnsureSucceeded(await _userManager.SetLockoutEndDateAsync(user, null));
                        EnsureSucceeded(await _userManager.ResetAccessFailedCountAsync(user));
                    }
                    else
                    {
                        EnsureSucceeded(await _userManager.SetLockoutEnabledAsync(user, true));
                        EnsureSucceeded(await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue));
                    }
                }

                await _unitOfWork.CommitAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static void EnsureSucceeded(IdentityResult result)
        {
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}