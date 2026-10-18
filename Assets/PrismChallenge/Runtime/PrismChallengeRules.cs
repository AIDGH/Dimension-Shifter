using System;

namespace DimensionShifter.PrismChallenge
{
    // Pure run state: no health, quest, checkpoint or combat state is written here.
    public sealed class PrismChallengeRules
    {
        public const float GateX = 35f;
        public const float GatePeriod = 4.6f;
        public const float QuietDuration = 2.8f;
        public const float WarningDuration = .8f;
        public const float AirShiftStart = 47f;
        public const float AirShiftEnd = 64f;
        public int Runes { get; private set; }
        public int AirShifts { get; private set; }
        public int Falls { get; private set; }
        public bool GatePassed { get; private set; }
        public bool Finished => Runes == 3;
        public float Elapsed { get; private set; }
        public string Medal => Elapsed <= 55f && Falls == 0 ? "GOLD" :
            Elapsed <= 90f && Falls <= 2 ? "SILVER" : "BRONZE";

        public void Tick(float delta)
        {
            if (!Finished && delta > 0 && !float.IsNaN(delta) && !float.IsInfinity(delta))
                Elapsed += delta;
        }

        public void Reset()
        {
            Runes = AirShifts = Falls = 0;
            Elapsed = 0;
            GatePassed = false;
        }

        public void RegisterAirShift(float localX)
        {
            if (Runes == 2 && localX >= AirShiftStart && localX <= AirShiftEnd)
                AirShifts++;
        }

        public void RegisterGatePass()
        {
            if (Runes == 1) GatePassed = true;
        }

        public bool TryCollectRune(int index, int world)
        {
            if (index != Runes || Finished) return false;
            if (index == 0 && world != 1) return false;
            if (index == 1 && (world != 0 || !GatePassed)) return false;
            if (index == 2 && AirShifts < 2) return false;
            Runes++;
            return true;
        }

        public void Fall()
        {
            if (Finished) return;
            Falls++;
            AirShifts = 0;
            if (Runes < 2) GatePassed = false;
        }

        public static float GatePhase(float elapsed) => Math.Max(0, elapsed) % GatePeriod;
        public static bool GateIsQuiet(float elapsed) => GatePhase(elapsed) < QuietDuration;
    }
}
