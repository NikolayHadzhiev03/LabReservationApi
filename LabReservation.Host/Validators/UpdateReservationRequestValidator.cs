using FluentValidation;
using LabReservation.Models.Entities;
using LabReservation.Models.Requests;

namespace LabReservation.Host.Validators
{
    /// <summary>
    /// FluentValidator for UpdateReservationRequest
/// </summary>
   public class UpdateReservationRequestValidator : AbstractValidator<UpdateReservationRequest>
    {
   public UpdateReservationRequestValidator()
  {
       RuleFor(x => x.StartTime)
   .NotEmpty().WithMessage("Start time is required");

   RuleFor(x => x.EndTime)
    .NotEmpty().WithMessage("End time is required")
   .GreaterThan(x => x.StartTime).WithMessage("End time must be after start time");

       RuleFor(x => x.Purpose)
.NotEmpty().WithMessage("Purpose is required")
            .MaximumLength(500).WithMessage("Purpose cannot exceed 500 characters");

  RuleFor(x => x.Status)
  .IsInEnum().WithMessage("Invalid reservation status");

         // Custom rule: reservation must be at least 30 minutes
       RuleFor(x => x)
           .Must(x => (x.EndTime - x.StartTime).TotalMinutes >= 30)
     .WithMessage("Reservation must be at least 30 minutes long");

        // Custom rule: reservation cannot exceed 8 hours
     RuleFor(x => x)
  .Must(x => (x.EndTime - x.StartTime).TotalHours <= 8)
 .WithMessage("Reservation cannot exceed 8 hours");
        }
}
}
