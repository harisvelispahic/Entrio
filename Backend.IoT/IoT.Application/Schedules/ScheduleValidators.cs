using FluentValidation;
using IoT.Domain.Entities.Devices;

namespace IoT.Application.Schedules;

public class CreateScheduleRequestValidator : AbstractValidator<CreateScheduleRequest>
{
    /// <summary>
    /// The only commands a user may schedule. A bare Close is not a user action, and Stop
    /// makes no sense on a door that is not moving.
    /// </summary>
    private static readonly DeviceCommandType[] Schedulable =
        [DeviceCommandType.Open, DeviceCommandType.Vent];

    public CreateScheduleRequestValidator()
    {
        RuleFor(x => x.CommandType)
            .Must(command => Schedulable.Contains(command))
            .WithMessage("Only Open and Vent can be scheduled.");

        RuleFor(x => x.TargetPercentage)
            .NotNull().WithMessage("Vent requires a target percentage.")
            .InclusiveBetween(1, 99).WithMessage("Vent percentage must be between 1 and 99.")
            .When(x => x.CommandType == DeviceCommandType.Vent);

        // Compared against UtcNow because the wire contract is UTC instants; the browser
        // has already converted the user's wall-clock choice by the time it arrives.
        RuleFor(x => x.OpensAtUtc)
            .Must(opensAt => opensAt > DateTime.UtcNow)
            .WithMessage("Opening time must be in the future.");

        RuleFor(x => x.ClosesAtUtc)
            .Must((request, closesAt) => closesAt > request.OpensAtUtc)
            .WithMessage("Closing time must be after the opening time.");
    }
}

public class AutoCloseSettingsRequestValidator : AbstractValidator<AutoCloseSettingsRequest>
{
    public AutoCloseSettingsRequestValidator()
    {
        RuleFor(x => x.AfterSeconds)
            .InclusiveBetween(AutoCloseService.MinAfterSeconds, AutoCloseService.MaxAfterSeconds)
            .WithMessage(
                $"Auto-close delay must be between {AutoCloseService.MinAfterSeconds} " +
                $"and {AutoCloseService.MaxAfterSeconds} seconds.");
    }
}
