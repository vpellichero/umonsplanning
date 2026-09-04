using FluentValidation.TestHelper;
using UMonsPlanning.Backend.Contracts;

namespace UMonsPlanning.Backend.Tests;

public class ScheduleIcsQueryValidatorTests
{
    private readonly ScheduleIcsQueryValidator _validator = new();

    [Fact]
    public void Validate_FormationOnly_IsValid()
        => _validator.TestValidate(new ScheduleIcsQuery { Formation = "bab3" }).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Validate_WithWeek_IsValid()
        => _validator.TestValidate(new ScheduleIcsQuery { Formation = "bab3", Week = 3 }).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Validate_WithDate_IsValid()
        => _validator.TestValidate(new ScheduleIcsQuery { Formation = "bab3", Date = new DateOnly(2026, 9, 21) })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Validate_WithStartAndEnd_IsValid()
        => _validator.TestValidate(new ScheduleIcsQuery
        {
            Formation = "bab3",
            Start = new DateOnly(2026, 9, 21),
            End = new DateOnly(2026, 10, 5)
        }).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Validate_EmptyFormation_HasError()
        => _validator.TestValidate(new ScheduleIcsQuery { Formation = "" })
            .ShouldHaveValidationErrorFor(x => x.Formation);

    [Fact]
    public void Validate_WeekBelowOne_HasError()
        => _validator.TestValidate(new ScheduleIcsQuery { Formation = "bab3", Week = 0 })
            .ShouldHaveValidationErrorFor(x => x.Week);

    [Fact]
    public void Validate_StartWithoutEnd_IsValid()
        => _validator.TestValidate(new ScheduleIcsQuery { Formation = "bab3", Start = new DateOnly(2026, 9, 21) })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Validate_EndWithoutStart_IsValid()
        => _validator.TestValidate(new ScheduleIcsQuery { Formation = "bab3", End = new DateOnly(2026, 9, 21) })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Validate_StartAfterEnd_HasError()
        => _validator.TestValidate(new ScheduleIcsQuery
        {
            Formation = "bab3",
            Start = new DateOnly(2026, 10, 5),
            End = new DateOnly(2026, 9, 21)
        }).ShouldHaveValidationErrorFor(x => x.Start);

    [Fact]
    public void Validate_WeekAndDateTogether_HasError()
        => _validator.TestValidate(new ScheduleIcsQuery
        {
            Formation = "bab3",
            Week = 3,
            Date = new DateOnly(2026, 9, 21)
        }).ShouldHaveValidationErrorFor(x => x.Week);

    [Fact]
    public void Validate_WeekAndPeriodTogether_HasError()
        => _validator.TestValidate(new ScheduleIcsQuery
        {
            Formation = "bab3",
            Week = 3,
            Start = new DateOnly(2026, 9, 21),
            End = new DateOnly(2026, 10, 5)
        }).ShouldHaveValidationErrorFor(x => x.Week);

    [Fact]
    public void Validate_WeekAndStartOnlyTogether_HasError()
        => _validator.TestValidate(new ScheduleIcsQuery
        {
            Formation = "bab3",
            Week = 3,
            Start = new DateOnly(2026, 9, 21),
        }).ShouldHaveValidationErrorFor(x => x.Week);

    [Fact]
    public void Validate_DateAndEndOnlyTogether_HasError()
        => _validator.TestValidate(new ScheduleIcsQuery
        {
            Formation = "bab3",
            Date = new DateOnly(2026, 9, 21),
            End = new DateOnly(2026, 10, 5),
        }).ShouldHaveValidationErrorFor(x => x.Week);

    [Theory]
    [InlineData(IcsLayout.PerCourse)]
    [InlineData(IcsLayout.PerDay)]
    public void Validate_WithDefinedLayout_IsValid(IcsLayout layout)
        => _validator.TestValidate(new ScheduleIcsQuery { Formation = "bab3", Layout = layout })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Validate_UndefinedLayoutValue_HasError()
        => _validator.TestValidate(new ScheduleIcsQuery { Formation = "bab3", Layout = (IcsLayout)99 })
            .ShouldHaveValidationErrorFor(x => x.Layout);

    [Fact]
    public void Validate_TitleWithPerDayLayout_IsValid()
        => _validator.TestValidate(new ScheduleIcsQuery { Formation = "bab3", Layout = IcsLayout.PerDay, Title = "Cours BAB3" })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Validate_TitleWithoutPerDayLayout_HasError()
        => _validator.TestValidate(new ScheduleIcsQuery { Formation = "bab3", Title = "Cours BAB3" })
            .ShouldHaveValidationErrorFor(x => x.Title);

    [Fact]
    public void Validate_TitleWithPerCourseLayout_HasError()
        => _validator.TestValidate(new ScheduleIcsQuery { Formation = "bab3", Layout = IcsLayout.PerCourse, Title = "Cours BAB3" })
            .ShouldHaveValidationErrorFor(x => x.Title);

    [Fact]
    public void Validate_TitleTooLong_HasError()
        => _validator.TestValidate(new ScheduleIcsQuery { Formation = "bab3", Layout = IcsLayout.PerDay, Title = new string('a', 201) })
            .ShouldHaveValidationErrorFor(x => x.Title);
}
