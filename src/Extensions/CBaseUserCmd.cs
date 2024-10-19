using System.Runtime.CompilerServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;

public class CBaseUserCmd
{
    public CBaseUserCmd(IntPtr pointer)
    {
        Handle = pointer;
    }

    public IntPtr Handle { get; set; }
    public float ForwardMove => GetForwardMove();
    public float SideMove => GetSideMove();
    public unsafe float GetForwardMove()
    {
        var ForwardMove = Unsafe.Read<float>((void*)(Handle + 0x50));
        return ForwardMove;
    }
    public unsafe float GetMouseX()
    {
        var MouseX = Unsafe.Read<float>((void*)(Handle + 0x68));
        return MouseX;
    }
    public unsafe float GetMouseY()
    {
        var MouseY = Unsafe.Read<float>((void*)(Handle + 0x6C));
        return MouseY;
    }
    public unsafe float GetSideMove()
    {
        var SideMove = Unsafe.Read<float>((void*)(Handle + 0x54));
        return SideMove;
    }
    public unsafe void DisableSideMove()
    {
        Unsafe.Write<float>((void*)(Handle + 0x54), 0);
    }
    public unsafe void DisableForwardMove()
    {
        Unsafe.Write<float>((void*)(Handle + 0x50), 0);
    }
}