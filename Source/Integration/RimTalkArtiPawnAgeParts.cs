using System;

namespace AdvancedRimTalk.Integration
{
    internal sealed class RimTalkArtiPawnAgeParts
    {
        internal const long TicksPerHour = 2500L;
        internal const long TicksPerDay = 60000L;
        internal const long TicksPerQuadrum = 900000L;
        internal const long TicksPerYear = 3600000L;

        private RimTalkArtiPawnAgeParts(long ticks)
        {
            Ticks = Math.Max(0L, ticks);

            Years = Ticks / TicksPerYear;
            long remainder = Ticks - Years * TicksPerYear;
            Quadrums = remainder / TicksPerQuadrum;
            remainder -= Quadrums * TicksPerQuadrum;
            Days = remainder / TicksPerDay;
            remainder -= Days * TicksPerDay;
            Hours = (double)remainder / TicksPerHour;
        }

        public long Ticks { get; }
        public long Years { get; }
        public long Quadrums { get; }
        public long Days { get; }
        public double Hours { get; }
        public double TotalYears { get { return (double)Ticks / TicksPerYear; } }
        public double TotalQuadrums { get { return (double)Ticks / TicksPerQuadrum; } }
        public double TotalDays { get { return (double)Ticks / TicksPerDay; } }
        public double TotalHours { get { return (double)Ticks / TicksPerHour; } }

        public static RimTalkArtiPawnAgeParts FromTicks(long ticks)
        {
            return new RimTalkArtiPawnAgeParts(ticks);
        }
    }
}
