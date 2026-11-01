using AutoMapper;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Domain.Models.Entities;

namespace ClinicSystem.Application.Profiles
{
    public class AppointmentProfile : Profile
    {
        public AppointmentProfile()
        {
            CreateMap<Appointment, AppointmentDTO>()
                .ForMember(d => d.PatientName, o => o.MapFrom(s =>
                    s.Patient.Person.FirstName + " " + s.Patient.Person.LastName))
                .ForMember(d => d.DoctorName, o => o.MapFrom(s =>
                    s.Doctor.User.Person.FirstName + " " + s.Doctor.User.Person.LastName))
                .ForMember(d => d.SpecialtyName, o => o.MapFrom(s => s.Doctor.Specialty.Name));
        }
    }
}