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

        // CounterStrikeSharp swallows a missing signature while it builds the function
        // (empty catch in BaseMemoryFunction.CreateValveFunctionBySignature), which leaves a
        // null pointer in an object that constructed fine. Throw, so the caller keeps the
        // DisabledSnapBaseAngles fallback instead of every teleport failing with
        // "Invalid function pointer".
        if (_snap.Handle == IntPtr.Zero)
            throw new InvalidOperationException("SnapBaseAngles signature did not resolve");
    }

    public unsafe void Snap(CCSPlayerPawn pawn, QAngle_t angles)
    {
        if (pawn == null || !pawn.IsValid)
            return;

        _snap.Invoke(pawn.Handle, (nint)(&angles));
    }
}
