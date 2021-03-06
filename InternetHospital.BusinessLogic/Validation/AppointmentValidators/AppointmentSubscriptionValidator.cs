﻿using System;
using System.Linq;
using FluentValidation;
using InternetHospital.BusinessLogic.Models.Appointment;
using InternetHospital.DataAccess;
using InternetHospital.DataAccess.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternetHospital.BusinessLogic.Validation.AppointmentValidators
{
    public class AppointmentSubscriptionValidator : AbstractValidator<AppointmentSubscribeModel>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationContext _context;
        private Appointment _appointment;

        public AppointmentSubscriptionValidator(ApplicationContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            CascadeMode = CascadeMode.StopOnFirstFailure;

            RuleFor(a => a)
                .Must(a => IfExist(a.Id))
                .OverridePropertyName("message")
                .WithMessage("Appointment is not exist")
                .DependentRules(() =>
                {
                    RuleFor(a => a)
                         .Must(a => NotReserved())
                         .OverridePropertyName("message")
                         .WithMessage("This appointment is already reserved")
                         .DependentRules(() =>
                         {
                             RuleFor(a => a)
                                 .Must(a => NotOverdue())
                                 .OverridePropertyName("message")
                                 .WithMessage("This appointment is no longer available")
                                 .DependentRules(() =>
                                 {
                                     RuleFor(a => a)
                                         .Must(a => NotOverlay())
                                         .OverridePropertyName("message")
                                         .WithMessage("You already have an appointment for this time")
                                         .DependentRules(() =>
                                         {
                                             RuleFor(a => a)
                                                 .Must(a => NotAlreadySubscribed())
                                                 .OverridePropertyName("message")
                                                 .WithMessage("You can only have one appointment with each doctor per day")
                                                 .DependentRules(() =>
                                                 {
                                                     RuleFor(a => a)
                                                         .Must(a => AddedToBlackList())
                                                         .OverridePropertyName("message")
                                                         .WithMessage("The doctor added you to black list")
                                                         .DependentRules(() =>
                                                         {
                                                             RuleFor(a => a)
                                                                 .Must(a => CheckPatientWasApproved())
                                                                 .OverridePropertyName("message")
                                                                 .WithMessage("Please enter all necessary data and wait for the moderator's approval");
                                                         });
                                                 });
                                         });
                                 });
                         });
                });
        }

        private bool IfExist(int appointmentId)
        {
            _appointment = _context.Appointments
                .Include(a => a.Status)
                .FirstOrDefault(x => x.Id == appointmentId);

            return _appointment != null;
        }

        private bool NotReserved()
        {
            return _appointment.Status.Id == (int)AppointmentStatuses.DEFAULT_STATUS;
        }

        private bool NotOverdue()
        {
            return _appointment.StartTime > DateTime.Now;
        }

        private bool NotOverlay()
        {
            var patientId = int.Parse(_httpContextAccessor.HttpContext.User.Identity.Name);
            return !_context.Appointments
                .Any(a => a.UserId == patientId && a.StatusId == (int) AppointmentStatuses.RESERVED_STATUS
                          && ((a.StartTime >= _appointment.StartTime && a.StartTime < _appointment.EndTime)
                              || (a.EndTime > _appointment.StartTime && a.EndTime <= _appointment.EndTime)));
        }

        private bool NotAlreadySubscribed()
        {
            var patientId = int.Parse(_httpContextAccessor.HttpContext.User.Identity.Name);
            return !_context.Appointments.Any(a => a.UserId == patientId
                                           && a.DoctorId == _appointment.DoctorId
                                           && a.StartTime.Date == _appointment.StartTime.Date
                                           && a.StatusId != (int) AppointmentStatuses.CANCELED_STATUS);

        }

        private bool AddedToBlackList()
        {
            var patientId = int.Parse(_httpContextAccessor.HttpContext.User.Identity.Name);
            return !_context.DoctorBlackLists.Any(b => b.UserId == patientId
                                            && b.DoctorId == _appointment.DoctorId);
        }

        private bool CheckPatientWasApproved()
        {
            var patientId = int.Parse(_httpContextAccessor.HttpContext.User.Identity.Name);
            var STATUS_OF_APPROVED_PATIENTS = 2;
            return !_context.Users.Any(p => p.Id == patientId
                                            && p.StatusId == STATUS_OF_APPROVED_PATIENTS);
        }

    }
}
