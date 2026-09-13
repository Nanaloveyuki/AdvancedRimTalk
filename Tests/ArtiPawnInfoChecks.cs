using System;
using AdvancedRimTalk.Integration;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class ArtiPawnInfoChecks
    {
        public static void Run()
        {
            SplitsGameTicksIntoAgeParts();
            ClampsNegativeTicks();
        }

        private static void SplitsGameTicksIntoAgeParts()
        {
            long ticks = RimTalkArtiPawnAgeParts.TicksPerYear
                + RimTalkArtiPawnAgeParts.TicksPerQuadrum * 2L
                + RimTalkArtiPawnAgeParts.TicksPerDay * 3L
                + RimTalkArtiPawnAgeParts.TicksPerHour * 4L
                + RimTalkArtiPawnAgeParts.TicksPerHour / 2L;
            RimTalkArtiPawnAgeParts age = RimTalkArtiPawnAgeParts.FromTicks(ticks);

            AssertEqual(1L, age.Years, "age years");
            AssertEqual(2L, age.Quadrums, "age quadrums");
            AssertEqual(3L, age.Days, "age days");
            AssertEqual(4.5, age.Hours, "age hours");
            AssertEqual(2236.5, age.TotalHours, "total age hours");
        }

        private static void ClampsNegativeTicks()
        {
            RimTalkArtiPawnAgeParts age = RimTalkArtiPawnAgeParts.FromTicks(-1L);
            AssertEqual(0L, age.Ticks, "negative ticks are empty");
            AssertEqual(0L, age.Years, "negative age years are empty");
            AssertEqual(0.0, age.Hours, "negative age hours are empty");
        }

        private static void AssertEqual<T>(T expected, T actual, string name)
        {
            if (!Equals(expected, actual))
            {
                throw new InvalidOperationException("Failed: " + name);
            }
        }
    }
}
