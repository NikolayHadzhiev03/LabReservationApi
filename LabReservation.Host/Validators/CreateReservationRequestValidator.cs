using FluentValidation;
using LabReservation.Models.Requests;

namespace LabReservation.Host.Validators
{
    /// <summary>
  /// FluentValidator for CreateReservationRequest
    /// </summary>
    public class CreateReservationRequestValidator : AbstractValidator<CreateReservationRequest>
    {
        public CreateReservationRequestValidator()
     {
     RuleFor(x => x.LabId)
 .NotEmpty().WithMessage("Lab ID is required")
  .Length(24).WithMessage("Lab ID must be a valid MongoDB ObjectId (24 characters)");

    RuleFor(x => x.CustomerName)
     .NotEmpty().WithMessage("Customer name is required")
            .MinimumLength(2).WithMessage("Customer name must be at least 2 characters")
.MaximumLength(100).WithMessage("Customer name cannot exceed 100 characters");

            RuleFor(x => x.CustomerEmail)
     .NotEmpty().WithMessage("Customer email is required")
    .EmailAddress().WithMessage("Invalid email format");

      RuleFor(x => x.StartTime)
       .NotEmpty().WithMessage("Start time is required")
   .GreaterThan(DateTime.UtcNow).WithMessage("Start time must be in the future");

 RuleFor(x => x.EndTime)
          .NotEmpty().WithMessage("End time is required")
    .GreaterThan(x => x.StartTime).WithMessage("End time must be after start time");

RuleFor(x => x.Purpose)
           .NotEmpty().WithMessage("Purpose is required")
.MaximumLength(500).WithMessage("Purpose cannot exceed 500 characters");

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
