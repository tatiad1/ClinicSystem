using AutoMapper;
using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Interfaces;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Domain.Models.Entities;

namespace ClinicSystem.Application.Implementations
{
    public class SpecialtyService : ISpecialtyService
    {
        private readonly ISpecialtyRepository _specialtyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SpecialtyService(
            ISpecialtyRepository specialtyRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _specialtyRepository = specialtyRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ICollection<SpecialtyDTO>> GetAllAsync(CancellationToken cancellationToken)
        {
            var specialties = await _specialtyRepository.GetAllAsync(cancellationToken);
            return specialties.Select(_mapper.Map<SpecialtyDTO>).ToList();
        }

        public async Task<SpecialtyDTO?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var specialty = await _specialtyRepository.GetByIdAsync(id, cancellationToken);
            if (specialty == null)
                return null;

            return _mapper.Map<SpecialtyDTO>(specialty);
        }

        public async Task<SpecialtyDTO> AddAsync(CreateSpecialtyDTO dto, CancellationToken cancellationToken)
        {
            var name = ValidateName(dto.Name);
            ValidateReferralPolicy(dto.IsFamilyMedicine, dto.RequiresReferral);

            if (await _specialtyRepository.ExistsAsync(s => s.Name == name, cancellationToken))
                throw new InvalidOperationException("Specialty with this name already exists");

            var specialty = new Specialty
            {
                Name = name,
                IsFamilyMedicine = dto.IsFamilyMedicine,
                RequiresReferral = dto.RequiresReferral
            };

            await _specialtyRepository.AddAsync(specialty, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<SpecialtyDTO>(specialty);
        }

        public async Task UpdateAsync(int id, UpdateSpecialtyDTO dto, CancellationToken cancellationToken)
        {
            var specialty = await _specialtyRepository.GetByIdAsync(id, cancellationToken);
            if (specialty == null)
                throw new KeyNotFoundException("Specialty not found");

            var name = ValidateName(dto.Name);
            ValidateReferralPolicy(dto.IsFamilyMedicine, dto.RequiresReferral);

            if (await _specialtyRepository.ExistsAsync(s => s.Name == name && s.Id != id, cancellationToken))
                throw new InvalidOperationException("Specialty with this name already exists");

            specialty.Name = name;
            specialty.IsFamilyMedicine = dto.IsFamilyMedicine;
            specialty.RequiresReferral = dto.RequiresReferral;

            _specialtyRepository.Update(specialty);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken)
        {
            var specialty = await _specialtyRepository.GetByIdAsync(id, cancellationToken);
            if (specialty == null)
                throw new KeyNotFoundException("Specialty not found");

            if (await _specialtyRepository.HasDoctorsAsync(id, cancellationToken))
                throw new InvalidOperationException("Cannot delete specialty that has doctors");

            if (await _specialtyRepository.HasReferralsAsync(id, cancellationToken))
                throw new InvalidOperationException("Cannot delete specialty that is used in referrals");

            _specialtyRepository.Delete(specialty);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static string ValidateName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Specialty name is required");

            var trimmed = name.Trim();
            if (trimmed.Length > 100)
                throw new ArgumentException("Specialty name must be at most 100 characters");

            return trimmed;
        }

        private static void ValidateReferralPolicy(bool isFamilyMedicine, bool requiresReferral)
        {
            if (isFamilyMedicine && requiresReferral)
                throw new ArgumentException("Family medicine specialties cannot require a referral");
        }
    }
}
