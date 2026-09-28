using FluentValidation;
using IoT.Domain.Entities.Devices;

namespace IoT.Application.Schedules;

public class CreateScheduleRequestValidator : AbstractValidator<CreateScheduleRequest>
{
    public CreateScheduleRequestValidator()
    {
        RuleFor(x => x.CommandType)
            .IsInEnum().WithMessage("Unknown schedule command.");

        RuleFor(x => x.TargetPercentage)
            .NotNull().WithMessage("Vent requires a target percentage.")
            .InclusiveBetween(1, 99).WithMessage("Vent percentage must be between 1 and 99.")
            .When(x => x.CommandType == DeviceCommandType.Vent);

        // Compared against UtcNow because the wire contract is UTC instants; the browser
        // has already converted the user's wall-clock choice by the time it arrives.
        RuleFor(x => x.ExecuteAtUtc)
            .Must(executeAt => executeAt > DateTime.UtcNow)
            .WithMessage("Scheduled time must be in the future.");
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
