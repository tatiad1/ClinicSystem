using AutoMapper;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Domain.Models.Entities;

namespace ClinicSystem.Application.Profiles
{
    public class DoctorProfile : Profile
    {
        public DoctorProfile()
        {
            CreateMap<Doctor, DoctorDTO>()
                .ForMember(d => d.FirstName, o => o.MapFrom(s => s.User.Person.FirstName))
                .ForMember(d => d.LastName, o => o.MapFrom(s => s.User.Person.LastName))
                .ForMember(d => d.Phone, o => o.MapFrom(s => s.User.Person.Phone))
                .ForMember(d => d.Email, o => o.MapFrom(s => s.User.Email))
                .ForMember(d => d.SpecialtyName, o => o.MapFrom(s => s.Specialty.Name));
        }
    }
}