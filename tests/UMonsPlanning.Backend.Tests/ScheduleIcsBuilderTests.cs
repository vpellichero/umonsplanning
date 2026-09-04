using AwesomeAssertions;
using UMonsPlanning.Backend.Calendar;
using UMonsPlanning.Backend.Contracts;
using UMonsPlanning.Pronote.Models;

namespace UMonsPlanning.Backend.Tests;

public class ScheduleIcsBuilderTests
{
    private static readonly TimeProvider FixedTimeProvider =
        new FakeTimeProvider(new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero));

    private static readonly ResourceDto Formation = new("bab3-traduction", ".BAB3 - Traduction et interprétation");

    [Fact]
    public void Build_PerCourse_SummaryExcludesTheSubjectCode()
    {
        DayDto day = BuildDay(isCancelled: false);

        string ics = ScheduleIcsBuilder.Build(Formation, section: null, [day], IcsLayout.PerCourse, FixedTimeProvider);

        ics.Should().Contain("SUMMARY:Langue ALLE");
        ics.Should().NotContain("SUMMARY:T-ALLE-401");
    }

    [Fact]
    public void Build_PerCourse_SingleCourse_ProducesOneConfirmedEvent()
    {
        DayDto day = BuildDay(isCancelled: false);

        string ics = ScheduleIcsBuilder.Build(Formation, section: null, [day], IcsLayout.PerCourse, FixedTimeProvider);

        ics.Should().Contain("BEGIN:VEVENT");
        ics.Should().Contain("STATUS:CONFIRMED");
        ics.Should().Contain($"UID:{day.Courses[0].Key}@umonsplanning");
        ics.Should().Contain("DTSTART;TZID=Europe/Brussels:20260921T091500");
        ics.Should().Contain("DTEND;TZID=Europe/Brussels:20260921T101500");
        ics.Should().Contain("LOCATION:NiDeVinci.313");
        ics.Should().Contain("X-WR-CALNAME:.BAB3 - Traduction et interprétation");
    }

    [Fact]
    public void Build_PerCourse_DescriptionListsCodeAndCourseType()
    {
        DayDto day = BuildDay(isCancelled: false);

        string ics = ScheduleIcsBuilder.Build(Formation, section: null, [day], IcsLayout.PerCourse, FixedTimeProvider);

        ics.Should().Contain("Code : T-ALLE-401");
        ics.Should().Contain("Type de cours : Cours");
    }

    [Fact]
    public void Build_PerCourse_CancelledCourse_ProducesCancelledStatus()
    {
        DayDto day = BuildDay(isCancelled: true);

        string ics = ScheduleIcsBuilder.Build(Formation, section: null, [day], IcsLayout.PerCourse, FixedTimeProvider);

        ics.Should().Contain("STATUS:CANCELLED");
    }

    [Fact]
    public void Build_PerCourse_WithSection_IncludesSectionInCalendarName()
    {
        var section = new ResourceDto("d3", "D3");
        DayDto day = BuildDay(isCancelled: false);

        string ics = ScheduleIcsBuilder.Build(Formation, section, [day], IcsLayout.PerCourse, FixedTimeProvider);

        ics.Should().Contain("X-WR-CALNAME:.BAB3 - Traduction et interprétation — D3");
    }

    [Fact]
    public void Build_PerCourse_NoCourses_ProducesEmptyCalendarWithoutError()
    {
        string ics = ScheduleIcsBuilder.Build(Formation, section: null, days: [], IcsLayout.PerCourse, FixedTimeProvider);

        ics.Should().Contain("BEGIN:VCALENDAR");
        ics.Should().NotContain("BEGIN:VEVENT");
    }

    [Fact]
    public void Build_PerDay_SpansFromFirstCourseStartToLastCourseEnd()
    {
        DayDto day = BuildDay(isCancelled: false);

        string ics = ScheduleIcsBuilder.Build(Formation, section: null, [day], IcsLayout.PerDay, FixedTimeProvider);

        ics.Should().Contain("DTSTART;TZID=Europe/Brussels:20260921T091500");
        ics.Should().Contain("DTEND;TZID=Europe/Brussels:20260921T101500");
        ics.Split("BEGIN:VEVENT").Should().HaveCount(2, "a single day with courses produces exactly one VEVENT");
    }

    [Fact]
    public void Build_PerDay_MultipleCourses_SpansFromEarliestStartToLatestEnd()
    {
        CourseDto morning = BuildCourse(isCancelled: false);
        CourseDto afternoon = morning with
        {
            Key = "b1a2c3d4e5f60718",
            SourceId = "10#c3",
            Start = new DateTime(2026, 9, 21, 14, 0, 0),
            End = new DateTime(2026, 9, 21, 16, 0, 0)
        };
        var day = new DayDto(morning.Date, morning.DayOfWeek, [afternoon, morning]);

        string ics = ScheduleIcsBuilder.Build(Formation, section: null, [day], IcsLayout.PerDay, FixedTimeProvider);

        ics.Should().Contain("DTSTART;TZID=Europe/Brussels:20260921T091500");
        ics.Should().Contain("DTEND;TZID=Europe/Brussels:20260921T160000");
    }

    [Fact]
    public void Build_PerDay_TitleCombinesFormationAndSectionWithASlash()
    {
        var section = new ResourceDto("d3", "D3");
        DayDto day = BuildDay(isCancelled: false);

        string ics = ScheduleIcsBuilder.Build(Formation, section, [day], IcsLayout.PerDay, FixedTimeProvider);

        ics.Should().Contain("SUMMARY:.BAB3 - Traduction et interprétation / D3");
    }

    [Fact]
    public void Build_PerDay_TitleIsFormationAloneWhenNoSectionIsChosen()
    {
        DayDto day = BuildDay(isCancelled: false);

        string ics = ScheduleIcsBuilder.Build(Formation, section: null, [day], IcsLayout.PerDay, FixedTimeProvider);

        ics.Should().Contain("SUMMARY:.BAB3 - Traduction et interprétation");
        ics.Should().NotContain("SUMMARY:.BAB3 - Traduction et interprétation /");
    }

    [Fact]
    public void Build_PerDay_DescriptionFormatsEachCourseAsOneLine()
    {
        DayDto day = BuildDay(isCancelled: false);

        string ics = ScheduleIcsBuilder.Build(Formation, section: null, [day], IcsLayout.PerDay, FixedTimeProvider);

        // Parsed back rather than matched against the raw text: a description this long is folded
        // across multiple lines per RFC 5545, which Ical.Net.Calendar.Load correctly unfolds.
        string? description = SingleEventDescription(ics);
        description.Should().Be("09h15 - 10h15 -> [NiDeVinci.313] Langue ALLE (Cours) [T-ALLE-401]");
    }

    [Fact]
    public void Build_PerDay_CancelledCourseLineIsPrefixed()
    {
        DayDto day = BuildDay(isCancelled: true);

        string ics = ScheduleIcsBuilder.Build(Formation, section: null, [day], IcsLayout.PerDay, FixedTimeProvider);

        SingleEventDescription(ics).Should().StartWith("(Annulé) 09h15 - 10h15 ->");
    }

    private static string? SingleEventDescription(string ics)
    {
        Ical.Net.Calendar calendar = Ical.Net.Calendar.Load(ics) ?? throw new InvalidOperationException("Failed to parse the generated .ics back.");
        return calendar.Events.Single().Description;
    }

    private static DayDto BuildDay(bool isCancelled)
    {
        CourseDto course = BuildCourse(isCancelled);
        return new DayDto(course.Date, course.DayOfWeek, [course]);
    }

    private static CourseDto BuildCourse(bool isCancelled) => new()
    {
        Key = "9f2c41ab7d0e5533",
        SourceId = "10#c2",
        Date = new DateOnly(2026, 9, 21),
        DayOfWeek = DayOfWeek.Monday,
        Start = new DateTime(2026, 9, 21, 9, 15, 0),
        End = new DateTime(2026, 9, 21, 10, 15, 0),
        DurationMinutes = 60,
        Subject = new SubjectDto("T-ALLE-401 - Langue ALLE", "T-ALLE-401", "Langue ALLE"),
        Rooms = ["NiDeVinci.313"],
        Groups = ["<.BAB3 - Traduction et interprétation>D3"],
        Category = "Cours",
        IsCancelled = isCancelled
    };

    /// <summary>Minimal fixed clock: avoids pulling in Microsoft.Extensions.TimeProvider.Testing for a single test.</summary>
    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
