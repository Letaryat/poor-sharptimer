using System.Runtime.CompilerServices;
using CounterStrikeSharp.API.Modules.Utils;
using FixVectorLeak;

public class CUserCmd
{
    public CUserCmd(IntPtr pointer)
    {
        Handle = pointer;
    }

    // static: the table never changes, and a CUserCmd is created for every processed command.
    private static readonly Dictionary<long, string> buttonNames = new Dictionary<long, string>
    {
        {1, "Left Click"},
        {2, "Jump"},
        {4, "Crouch"},
        {8, "Forward"},
        {16, "Backward"},
        {32, "Use"},
        {128, "Turn Left"},
        {256, "Turn Right"},
        {512, "Left"},
        {1024, "Right"},
        {2048, "Right Click"},
        {8192, "Reload"},
        {65536, "Shift"},
        {8589934592, "Scoreboard"},
        {34359738368, "Inspect"}
    };

    public unsafe List<String> GetMovementButton()
    {
        if (Handle == IntPtr.Zero)
            return ["None"];

        nint inputs = Unsafe.Read<IntPtr>((void*)(Handle + 0x60));
        
        // System.Console.WriteLine(moveMent); // Use this to see the value of the button you are pressing

        
        var movementButtons = new List<String>();

        foreach (var button in buttonNames)
        {
            if ((inputs & button.Key) == button.Key)
            {
                movementButtons.Add(button.Value);
            }
        }
        

        return movementButtons;
    }

    public IntPtr Handle { get; set; }
    public CBaseUserCmd BaseUserCmd => GetBaseCmd();


    public unsafe CBaseUserCmd GetBaseCmd()
    {
        var baseCmd = Unsafe.Read<IntPtr>((void*)(Handle + 0x40));

        return new CBaseUserCmd(baseCmd);
    }
    public unsafe void DisableInput(IntPtr userCmd, nint value)
    {
        Unsafe.Write((void*)(userCmd + 0x60), Unsafe.Read<ulong>((void*)(userCmd + 0x60)) & ~(ulong)value);
    }

    public unsafe ulong GetInputMask()
    {
        if (Handle == IntPtr.Zero)
            return 0;

        return Unsafe.Read<ulong>((void*)(Handle + 0x60));
    }
    public unsafe QAngle_t? GetViewAngles()
    {
        if (Handle == IntPtr.Zero)
            return null;

        var baseCmd = Unsafe.Read<IntPtr>((void*)(Handle + 0x40));
        if (baseCmd == IntPtr.Zero)
            return null;

        var msgQAngle_t = Unsafe.Read<IntPtr>((void*)(baseCmd + 0x40));
        if (msgQAngle_t == IntPtr.Zero)
            return null;

        var viewAngles = new QAngle_t(msgQAngle_t + 0x18);
        
        return viewAngles;
    }
}