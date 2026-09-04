using AwesomeAssertions;
using UMonsPlanning.Pronote.Internal;

namespace UMonsPlanning.Pronote.Tests;

public class GeneralParametersTests
{
    private static readonly GeneralParameters Parameters = GeneralParameters.Default;

    [Fact]
    public void WeekNumberFor_DateOfFirstMonday_ReturnsWeekOne()
        => Parameters.WeekNumberFor(Parameters.FirstMonday).Should().Be(1);

    [Fact]
    public void WeekNumberFor_OneWeekAfterFirstMonday_ReturnsWeekTwo()
        => Parameters.WeekNumberFor(Parameters.FirstMonday.AddDays(7)).Should().Be(2);

    [Fact]
    public void WeekNumberFor_DateBeforeAcademicYearStarts_ClampsToWeekOne()
        => Parameters.WeekNumberFor(Parameters.FirstMonday.AddDays(-5)).Should().Be(1);

    [Fact]
    public void WeekNumberFor_DateAfterAcademicYearEnds_ClampsToLastWeek()
        => Parameters.WeekNumberFor(Parameters.LastDate.AddDays(30)).Should().Be(Parameters.WeekCount);

    [Fact]
    public void WeekNumberFor_AnyDate_NeverReturnsLessThanOne()
        => Parameters.WeekNumberFor(new DateOnly(2000, 1, 1)).Should().BeGreaterThanOrEqualTo(1);
}
