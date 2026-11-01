using AutoMapper;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Domain.Models.Entities;

namespace ClinicSystem.Application.Profiles
{
    public class ReferralProfile : Profile
    {
        public ReferralProfile()
        {
            CreateMap<Referral, ReferralDTO>()
                .ForMember(d => d.PatientName, o => o.MapFrom(s =>
                    s.Patient.Person.FirstName + " " + s.Patient.Person.LastName))
                .ForMember(d => d.FromDoctorName, o => o.MapFrom(s =>
                    s.FromDoctor.User.Person.FirstName + " " + s.FromDoctor.User.Person.LastName))
                .ForMember(d => d.ToSpecialtyName, o => o.MapFrom(s => s.ToSpecialty.Name))
                .ForMember(d => d.IsActive, o => o.MapFrom(s => !s.IsUsed && s.ExpiresAt > DateTime.UtcNow));
        }
    }
}