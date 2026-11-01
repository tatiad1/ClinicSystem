using AutoMapper;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Domain.Models.Entities;

namespace ClinicSystem.Application.Profiles
{
    public class ConsultationProfile : Profile
    {
        public ConsultationProfile()
        {
            CreateMap<Consultation, ConsultationDTO>()
                .ForMember(d => d.AppointmentTime, o => o.MapFrom(s => s.Appointment.StartTime))
                .ForMember(d => d.PatientId, o => o.MapFrom(s => s.Appointment.PatientId))
                .ForMember(d => d.PatientName, o => o.MapFrom(s =>
                    s.Appointment.Patient.Person.FirstName + " " + s.Appointment.Patient.Person.LastName))
                .ForMember(d => d.DoctorId, o => o.MapFrom(s => s.Appointment.DoctorId))
                .ForMember(d => d.DoctorName, o => o.MapFrom(s =>
                    s.Appointment.Doctor.User.Person.FirstName + " " + s.Appointment.Doctor.User.Person.LastName));
        }
    }
}