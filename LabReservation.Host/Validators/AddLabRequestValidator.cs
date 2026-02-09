using FluentValidation;
using LabReservation.Models.Requests;

namespace LabReservation.Host.Validators
{
    /// <summary>
    /// FluentValidator for AddLabRequest
    /// </summary>
    public class AddLabRequestValidator : AbstractValidator<AddLabRequest>
    {
        public AddLabRequestValidator()
        {
       RuleFor(x => x.Name)
         .NotEmpty().WithMessage("Lab name is required")
          .MinimumLength(2).WithMessage("Lab name must be at least 2 characters")
 .MaximumLength(100).WithMessage("Lab name cannot exceed 100 characters");

     RuleFor(x => x.Location)
  .NotEmpty().WithMessage("Location is required")
  .MaximumLength(200).WithMessage("Location cannot exceed 200 characters");

      RuleFor(x => x.Capacity)
      .GreaterThan(0).WithMessage("Capacity must be greater than 0")
           .LessThanOrEqualTo(500).WithMessage("Capacity cannot exceed 500");

 RuleFor(x => x.Equipment)
        .NotNull().WithMessage("Equipment list cannot be null");
        }
    }
}
