using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using FixVectorLeak;

namespace SharpTimer;

public interface ISnapBaseAngles
{
    void Snap(CCSPlayerPawn pawn, QAngle_t angles);
}

public sealed class DisabledSnapBaseAngles : ISnapBaseAngles
{
    public void Snap(CCSPlayerPawn pawn, QAngle_t angles)
    {
    }
}

public class SnapBaseAngles : ISnapBaseAngles
{
    private readonly MemoryFunctionVoid<IntPtr, IntPtr> _snap;

    public SnapBaseAngles()
    {
        _snap = new(GameData.GetSignature("SnapBaseAngles"));
    }

    public unsafe void Snap(CCSPlayerPawn pawn, QAngle_t angles)
    {
        if (pawn == null || !pawn.IsValid)
            return;

        _snap.Invoke(pawn.Handle, (nint)(&angles));
    }
}
