using System;
using Photon.Deterministic;
using Quantum;

namespace HnSF
{
    [System.Serializable]
    public struct AnimationFrame
    {
        public int Id;
        public FP Time;
        public FPVector3 Position;
        public FPQuaternion Rotation;

        /// <summary>
        /// Y Rotation in radians
        /// </summary>
        public FP RotationY;

        public AnimationFrame(FPQuaternion rotation)
        {
            Id = 0;
            Time = 0;
            Position = FPVector3.Zero;
            Rotation = rotation;
            RotationY = 0;
        }

        public static AnimationFrame operator +(AnimationFrame AnimationFrameA, AnimationFrame AnimationFrameB)
        {
            return new AnimationFrame()
            {
                Id = AnimationFrameA.Id + AnimationFrameB.Id,
                Time = AnimationFrameA.Time + AnimationFrameB.Time,
                Position = AnimationFrameA.Position + AnimationFrameB.Position,
                Rotation = FPQuaternion.Product(AnimationFrameB.Rotation, AnimationFrameA.Rotation),
                RotationY = AnimationFrameA.RotationY + AnimationFrameB.RotationY
            };
        }

        public static AnimationFrame operator -(AnimationFrame AnimationFrameA, AnimationFrame AnimationFrameB)
        {
            return new AnimationFrame()
            {
                Id = AnimationFrameB.Id - AnimationFrameA.Id,
                Time = AnimationFrameB.Time - AnimationFrameA.Time,
                Position = AnimationFrameB.Position - AnimationFrameA.Position,
                Rotation = FPQuaternion.Product(AnimationFrameA.Rotation.Inverted, AnimationFrameA.Rotation),
                RotationY = AnimationFrameB.RotationY - AnimationFrameA.RotationY
            };
        }

        public static AnimationFrame operator *(AnimationFrame AnimationFrameA, AnimationFrame AnimationFrameB)
        {
            return new AnimationFrame()
            {
                Id = AnimationFrameA.Id * AnimationFrameB.Id,
                Time = AnimationFrameA.Time * AnimationFrameB.Time,
                Position = FPVector3.Scale(AnimationFrameA.Position, AnimationFrameB.Position),
                Rotation = AnimationFrameA.Rotation * AnimationFrameB.Rotation,
                RotationY = AnimationFrameA.RotationY * AnimationFrameB.RotationY
            };
        }

        public static AnimationFrame operator *(AnimationFrame AnimationFrameA, FP value)
        {
            return new AnimationFrame()
            {
                Id = (AnimationFrameA.Id * value).AsInt,
                Time = AnimationFrameA.Time * value,
                Position = AnimationFrameA.Position * value,
                Rotation = AnimationFrameA.Rotation * value,
                RotationY = AnimationFrameA.RotationY * value
            };
        }

        public static AnimationFrame Lerp(AnimationFrame AnimationFrameA, AnimationFrame AnimationFrameB, FP value)
        {
            AnimationFrame output = new AnimationFrame();

            output.Id = AnimationFrameA.Id;
            output.Time = FPMath.Lerp(AnimationFrameA.Time, AnimationFrameB.Time, value);
            output.Position = FPVector3.Lerp(AnimationFrameA.Position, AnimationFrameB.Position, value);

            var rotationYA = AnimationFrameA.RotationY;
            var rotationYB = AnimationFrameB.RotationY;

            if (rotationYA < 0 && AnimationFrameB.RotationY > 0) rotationYA *= -1;
            if (rotationYA > 0 && AnimationFrameB.RotationY < 0) rotationYB *= -1;

            output.RotationY = FPMath.Lerp(rotationYA, rotationYB, value);

            try
            {
                output.Rotation = FPQuaternion.Slerp(AnimationFrameA.Rotation, AnimationFrameB.Rotation, value);
            }
            catch (Exception e)
            {
                Log.Info("quaternion slerp divByZero : " + value + " " + ToString(AnimationFrameA.Rotation) + " " +
                         ToString(AnimationFrameB.Rotation) +
                         " \n" + e);
                output.Rotation = AnimationFrameA.Rotation;
            }

            return output;
        }

        public override string ToString()
        {
            return string.Format("Animator Frame id: " + Id + " time: " + Time.AsFloat + " position " +
                                 Position.ToString() +
                                 " rotation " + Rotation.AsEuler.ToString() + " rotationY " + RotationY);
        }

        public static string ToString(FPQuaternion q)
        {
            return $"{q.AsEuler.Z.AsFloat}, {q.AsEuler.Y.AsFloat}, {q.AsEuler.Z.AsFloat}";
        }
    }
}