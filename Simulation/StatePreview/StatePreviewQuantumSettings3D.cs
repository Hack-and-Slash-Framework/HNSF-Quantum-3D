using Photon.Deterministic;
using UnityEngine;

namespace Quantum
{
    public class StatePreviewQuantumSettings3D : StatePreviewQuantumSettingsBase
    {
#if QUANTUM_UNITY
        [Header("Preview Actor")]
#endif
        public FPVector3 attackerGroundStartPosition;
        public FPVector3 attackerAerialStartPosition;
        public FPVector3 attackerGroundStartingVelocity;
        public FPVector3 attackerAerialStartingVelocity;
        public FPVector3 attackerGroundStartingRotation;
        public FPVector3 attackerAerialStartingRotation;
        public ActorInputButtonType attackerInputButtons;
        public FPVector2 attackerInputMovement;
        
#if QUANTUM_UNITY
        [Header("Helping Actor")]
#endif
        public FPVector3 defenderGroundStartPosition;
        public FPVector3 defenderAerialStartPosition;
        public FPVector3 defenderGroundStartingVelocity;
        public FPVector3 defenderAerialStartingVelocity;
        public FPVector3 defenderGroundStartingRotation;
        public FPVector3 defenderAerialStartingRotation;
        public ActorInputButtonType defenderInputButtons;
        public FPVector2 defenderInputMovement;
        
        public virtual FPVector3 AttackerGetStartPosition(bool isAerial = false)
        {
            return isAerial ? attackerAerialStartPosition : attackerGroundStartPosition;
        }

        public virtual FPVector3 DefenderGetStartPosition(bool isAerial = false)
        {
            return isAerial ? defenderAerialStartPosition : defenderGroundStartPosition;
        }

        public virtual FPVector3 AttackerGetVelocity(bool isAerial = false)
        {
            return isAerial ? attackerAerialStartingVelocity : attackerGroundStartingVelocity;
        }

        public virtual FPVector3 DefenderGetVelocity(bool isAerial = false)
        {
            return isAerial ? defenderAerialStartingVelocity : defenderGroundStartingVelocity;
        }

        public virtual FPVector3 AttackerGetRotation(bool isAerial = false)
        {
            return isAerial ? attackerAerialStartingRotation : attackerGroundStartingRotation;
        }

        public virtual FPVector3 DefenderGetRotation(bool isAerial = false)
        {
            return isAerial ? defenderAerialStartingRotation : defenderGroundStartingRotation;
        }
    }
}