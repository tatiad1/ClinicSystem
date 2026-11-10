using AutoMapper;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Domain.Models.Entities;

namespace ClinicSystem.Application.Profiles
{
    public class PatientProfile : Profile
    {
        public PatientProfile()
        {
            CreateMap<Patient, PatientDTO>()
                .ForMember(d => d.FirstName, o => o.MapFrom(s => s.Person.FirstName))
                .ForMember(d => d.LastName, o => o.MapFrom(s => s.Person.LastName))
                .ForMember(d => d.PersonalNumber, o => o.MapFrom(s => s.Person.PersonalNumber))
                .ForMember(d => d.Phone, o => o.MapFrom(s => s.Person.Phone))
                .ForMember(d => d.Email, o => o.MapFrom(s => s.Person.User == null ? null : s.Person.User.Email))
                .ForMember(d => d.ContactEmail, o => o.MapFrom(s =>
                    s.Person.ContactEmail ?? (s.Person.User == null ? null : s.Person.User.Email)))
                .ForMember(d => d.HasAccount, o => o.MapFrom(s => s.Person.User != null))
                .ForMember(d => d.FamilyDoctorName, o => o.MapFrom(s =>
                    s.FamilyDoctor == null
                        ? null
                        : s.FamilyDoctor.User.Person.FirstName + " " + s.FamilyDoctor.User.Person.LastName));
        }
    }
}
