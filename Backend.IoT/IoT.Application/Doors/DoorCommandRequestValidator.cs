using FluentValidation;
using IoT.Domain.Entities.Devices;

namespace IoT.Application.Doors;

public class DoorCommandRequestValidator : AbstractValidator<DoorCommandRequest>
{
    public DoorCommandRequestValidator()
    {
        RuleFor(x => x.Command)
            .IsInEnum().WithMessage("Unknown door command.");

        // Only Vent takes a percentage, so the rule is conditional rather than a blanket
        // range check that would reject a null on Open.
        RuleFor(x => x.Percentage)
            .NotNull().WithMessage("Vent requires a percentage.")
            .InclusiveBetween(1, 99).WithMessage("Vent percentage must be between 1 and 99.")
            .When(x => x.Command == DeviceCommandType.Vent);
    }
}
