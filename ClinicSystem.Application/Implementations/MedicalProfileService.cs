using AutoMapper;
using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Interfaces;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Domain.Models.Entities;

namespace ClinicSystem.Application.Implementations
{
    public class MedicalProfileService : IMedicalProfileService
    {
        private static readonly HashSet<string> AllowedBloodTypes =
            new() { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };

        private readonly IMedicalProfileRepository _profileRepository;
        private readonly IPatientRepository _patientRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public MedicalProfileService(
            IMedicalProfileRepository profileRepository,
            IPatientRepository patientRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _profileRepository = profileRepository;
            _patientRepository = patientRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<MedicalProfileDTO?> GetByPatientIdAsync(int patientId, CancellationToken cancellationToken)
        {
            if (!await _patientRepository.ExistsAsync(p => p.Id == patientId, cancellationToken))
                throw new KeyNotFoundException("Patient not found");

            var profile = await _profileRepository.GetByPatientIdAsync(patientId, cancellationToken);
            if (profile == null)
                return null;

            return _mapper.Map<MedicalProfileDTO>(profile);
        }

        public async Task<MedicalProfileDTO> AddAsync(int patientId, CreateMedicalProfileDTO dto, CancellationToken cancellationToken)
        {
            if (!await _patientRepository.ExistsAsync(p => p.Id == patientId, cancellationToken))
                throw new KeyNotFoundException("Patient not found");

            if (await _profileRepository.ExistsAsync(m => m.PatientId == patientId, cancellationToken))
                throw new InvalidOperationException("Medical profile already exists for this patient");

            var profile = new MedicalProfile
            {
                PatientId = patientId,
                BloodType = NormalizeBloodType(dto.BloodType),
                Allergies = NormalizeText(dto.Allergies, "Allergies"),
                ChronicConditions = NormalizeText(dto.ChronicConditions, "Chronic conditions")
            };

            await _profileRepository.AddAsync(profile, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<MedicalProfileDTO>(profile);
        }

        public async Task UpdateAsync(int patientId, UpdateMedicalProfileDTO dto, CancellationToken cancellationToken)
        {
            if (!await _patientRepository.ExistsAsync(p => p.Id == patientId, cancellationToken))
                throw new KeyNotFoundException("Patient not found");

            var profile = await _profileRepository.GetByPatientIdAsync(patientId, cancellationToken);
            if (profile == null)
                throw new KeyNotFoundException("Medical profile not found");

            profile.BloodType = NormalizeBloodType(dto.BloodType);
            profile.Allergies = NormalizeText(dto.Allergies, "Allergies");
            profile.ChronicConditions = NormalizeText(dto.ChronicConditions, "Chronic conditions");

            _profileRepository.Update(profile);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static string? NormalizeBloodType(string? bloodType)
        {
            if (string.IsNullOrWhiteSpace(bloodType))
                return null;

            var normalized = bloodType.Trim().ToUpperInvariant();
            if (!AllowedBloodTypes.Contains(normalized))
                throw new ArgumentException("Blood type must be one of: A+, A-, B+, B-, AB+, AB-, O+, O-");

            return normalized;
        }

        private static string? NormalizeText(string? value, string field)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();
            if (trimmed.Length > 1000)
                throw new ArgumentException($"{field} must be at most 1000 characters");

            return trimmed;
        }
    }
}