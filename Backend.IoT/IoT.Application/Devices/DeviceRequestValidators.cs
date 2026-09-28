using FluentValidation;
using IoT.Domain.Entities.Devices;

namespace IoT.Application.Devices;

public class DeviceEventRequestValidator : AbstractValidator<DeviceEventRequest>
{
    public DeviceEventRequestValidator()
    {
        // Parsed rather than range-checked: the firmware sends enum names, so the rule is
        // "is this a name we know", and the service reparses the same way.
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Event type is required.")
            .Must(type => Enum.TryParse<DeviceEventType>(type, ignoreCase: true, out _))
            .WithMessage(x => $"Invalid event type: {x.Type}");

        RuleFor(x => x.Source)
            .Must(source => Enum.TryParse<DeviceEventSource>(source, ignoreCase: true, out _))
            .WithMessage(x => $"Invalid event source: {x.Source}")
            .When(x => !string.IsNullOrWhiteSpace(x.Source));
    }
}

public class DeviceStatusRequestValidator : AbstractValidator<DeviceStatusRequest>
{
    public DeviceStatusRequestValidator()
    {
        RuleFor(x => x.DoorState)
            .IsInEnum().WithMessage("Unknown door state.");

        RuleFor(x => x.PositionPercent)
            .InclusiveBetween(0, 100).WithMessage("Position must be between 0 and 100.");
    }
}
