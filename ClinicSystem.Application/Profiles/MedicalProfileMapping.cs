using AutoMapper;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Domain.Models.Entities;

namespace ClinicSystem.Application.Profiles
{
    public class MedicalProfileMapping : Profile
    {
        public MedicalProfileMapping()
        {
            CreateMap<MedicalProfile, MedicalProfileDTO>();
        }
    }
}