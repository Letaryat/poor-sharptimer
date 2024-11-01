namespace SharpTimer
{
    public class Structs
    {
        public struct Vector(float? x = null, float? y = null, float? z = null)
        {
            public float? X { get; set; } = x;
            public float? Y { get; set; } = y;
            public float? Z { get; set; } = z;
            public float? Length2D()
            { 
                if (X.HasValue && Y.HasValue)
                {
                    return (float?)Math.Sqrt(Math.Pow(X.Value, 2) + Math.Pow(Y.Value, 2));
                }
                return null;
            }
            public float? Length()
            { 
                if (X.HasValue && Y.HasValue && Z.HasValue)
                {
                    return (float?)Math.Sqrt(Math.Pow(X.Value, 2) + Math.Pow(Y.Value, 2) + Math.Pow(Z.Value, 2));
                }
                return null;
            }
            public bool IsZero()
            {
                return (X ?? 0) == 0 && (Y ?? 0) == 0 && (Z ?? 0) == 0;
            }
        }
        public struct QAngle(float? x = null, float? y = null, float? z = null)
        {
            public float? X { get; set; } = x;
            public float? Y { get; set; } = y;
            public float? Z { get; set; } = z;
        }
    }
}