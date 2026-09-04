using System.Text.Json;
using AwesomeAssertions;
using UMonsPlanning.Pronote.Internal;
using UMonsPlanning.Pronote.Models;
using UMonsPlanning.Pronote.Protocol;

namespace UMonsPlanning.Pronote.Tests;

public class PronoteCryptoTests
{
    // Vectors captured on hplanning2026.umons.ac.be by observing the official JS client.

    [Fact]
    public void EncryptOrder_FirstCall_UsesZeroIv()
        => PronoteCrypto.EncryptOrder(1, null).Should().Be("3fa959b13967e0ef176069e01e23c8d7");

    [Theory]
    [InlineData(3, "2e7d7189c311c9558fbeb0d7e85f9db5", "c113b20483a0effd595026777c63ac6d")]
    [InlineData(81, "2e100c4f5860970812d908b4a417d2e1", "84a6f82f9fa8b08f61e175239cd79c52")]
    public void EncryptOrder_SubsequentCalls_UseMd5OfSessionIv(int order, string ivHex, string expected)
        => PronoteCrypto.EncryptOrder(order, Convert.FromHexString(ivHex)).Should().Be(expected);

    [Fact]
    public void SerializeIvForServer_SessionIv_IsSerializedAsRawBase64()
    {
        byte[] iv = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");
        PronoteCrypto.SerializeIvForServer(iv).Should().Be(Convert.ToBase64String(iv));
    }
}

public class PronoteSetTests
{
    [Theory]
    [InlineData("[2..7,9,11..12]", new[] { 2, 3, 4, 5, 6, 7, 9, 11, 12 })]
    [InlineData("[]", new int[0])]
    [InlineData(null, new int[0])]
    public void Parse_ParsesRanges(string? value, int[] expected)
        => PronoteSet.Parse(value).Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
}

public class SlugTests
{
    [Fact]
    public void From_RemovesDiacriticsAndPunctuation()
        => Slug.From(".BAB3 - Traduction et interprétation").Should().Be("bab3-traduction-et-interpretation");
}

public class ScheduleMapperTests
{
    private static readonly GeneralParameters Parameters = GeneralParameters.Default;

    private static IReadOnlyList<DayDto> Load()
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText("fixture-semaine3.json"));
        return ScheduleMapper.Map(document.RootElement, Parameters, week: 3);
    }

    [Fact]
    public void Map_PlacesCoursesOnTheRightDayAndTime()
    {
        IReadOnlyList<DayDto> days = Load();

        CourseDto first = days.SelectMany(d => d.Courses)
            .Single(c => c.Subject!.Code == "T-ALLE-401");

        // Week 3 => Monday 2026-09-21 ; slot 5 => 08:00 + 5 x 15 min.
        first.Date.Should().Be(new DateOnly(2026, 9, 21));
        first.Start.Should().Be(new DateTime(2026, 9, 21, 9, 15, 0));
        first.End.Should().Be(new DateTime(2026, 9, 21, 10, 15, 0));
        first.DurationMinutes.Should().Be(60);
        first.Rooms.Should().ContainSingle().Which.Should().Be("NiDeVinci.313");
    }

    [Fact]
    public void Map_ComputesDayIndexFromPlace()
    {
        CourseDto friday = Load().SelectMany(d => d.Courses).Single(c => c.Subject!.Code == "T-NEER-400");

        // slot 294 => day 4 (Friday), slot 22 within the day => 13:30.
        friday.DayOfWeek.Should().Be(DayOfWeek.Friday);
        friday.Start.Should().Be(new DateTime(2026, 9, 25, 13, 30, 0));
        friday.Status.Should().Be("Semaine B | A");
    }

    [Fact]
    public void Map_KeysAreStableAcrossCalls()
    {
        string[] first = Load().SelectMany(d => d.Courses).Select(c => c.Key).ToArray();
        string[] second = Load().SelectMany(d => d.Courses).Select(c => c.Key).ToArray();
        second.Should().Equal(first);
    }
}
