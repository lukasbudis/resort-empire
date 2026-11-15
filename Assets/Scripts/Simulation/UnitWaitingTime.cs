namespace ResortEmpire
{
    // Nemenny zaznam vznikne pri usadeni; dalsia obsluha uz cakanie nezmeni.
    public readonly struct UnitWaitingTime
    {
        public int UnitId { get; }
        public long ArrivalTick { get; }
        public long SeatedTick { get; }
        public long WaitingTicks => SeatedTick - ArrivalTick;
        public double WaitingSeconds => WaitingTicks * 0.1d;

        internal UnitWaitingTime(int unitId, long arrivalTick, long seatedTick)
        {
            UnitId = unitId;
            ArrivalTick = arrivalTick;
            SeatedTick = seatedTick;
        }
    }
}