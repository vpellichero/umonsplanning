using FluentValidation;

namespace UMonsPlanning.Backend.Contracts;

public sealed class ScheduleIcsQueryValidator : AbstractValidator<ScheduleIcsQuery>
{
    public ScheduleIcsQueryValidator()
    {
        RuleFor(x => x.Formation).NotEmpty();

        RuleFor(x => x.Week)
            .GreaterThanOrEqualTo(1)
            .When(x => x.Week is not null)
            .WithMessage("Week must be 1 or greater.");

        RuleFor(x => x.Layout).IsInEnum();

        RuleFor(x => x.Title)
            .MaximumLength(200)
            .WithMessage("Title must be 200 characters or fewer.");

        RuleFor(x => x.Title)
            .Empty()
            .When(x => x.Layout != IcsLayout.PerDay)
            .WithMessage("Title is only supported with the PerDay layout.");

        // Start alone means "from Start to the last available week" ; End alone means "from the
        // first available week to End" — see ScheduleEndpoints.ResolveWeeksAsync.
        RuleFor(x => x)
            .Must(x => x.Start is null || x.End is null || x.Start <= x.End)
            .WithMessage("Start must not be after End.")
            .OverridePropertyName(nameof(ScheduleIcsQuery.Start));

        RuleFor(x => x)
            .Must(HaveAtMostOneSelectionMode)
            .WithMessage("Specify at most one of: week, date, or a start/end period.")
            .OverridePropertyName(nameof(ScheduleIcsQuery.Week));
    }

    private static bool HaveAtMostOneSelectionMode(ScheduleIcsQuery query)
    {
        int modes = 0;
        if (query.Week is not null)
        {
            modes++;
        }

        if (query.Date is not null)
        {
            modes++;
        }

        if (query.Start is not null || query.End is not null)
        {
            modes++;
        }

        return modes <= 1;
    }
}
