using Quantum;

namespace HnSF
{
    public partial struct IncomingThreatEphemeral
    {
        public EntityRef sourceAttacker;

        public bool isThrow;
        public bool isProjectile;
        public int impactAtFrame;
        public int endAtFrame;
        public byte dangerLevel;
        public byte responseDifficulty;

        public bool IsValid()
        {
            return sourceAttacker != EntityRef.None;
        }
    }
}