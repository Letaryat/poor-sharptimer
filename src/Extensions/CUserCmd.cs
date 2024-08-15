using System.Runtime.CompilerServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

public class CUserCmd
{
    public CUserCmd(IntPtr pointer)
    {
        Handle = pointer;
    }

    private Dictionary<Int64, string> buttonNames = new Dictionary<Int64, string>
    {
        {1, "Left Click"},
        {2, "Jump"},
        {4, "Crouch"},
        {8, "Forward"},
        {16, "Backward"},
        {32, "Use"},
        // 64 ??
        {128, "Turn Left"},
        {256, "Turn Right"},
        {512, "Left"},
        {1024, "Right"},
        {2048, "Right Click"},
        {8192, "Reload"},
        // 16384 ??
        // 32768 ??
        {65536, "Shift"},
        /* 
        131072 ??
        262144 ??
        524288 ??
        1048576 ??
        2097152 ??
        4194304 ??
        8388608 ??
        16777216 ??
        33554432 ??
        67108864 ??
        134217728 ??
        268435456 ??
        536870912 ??
        1073741824 ??
        2147483648 ??
        4294967296 ?? 
        */
        {8589934592, "Scoreboard"},
        {34359738368, "Inspect"}
    };

    public unsafe List<String> GetMovementButton()
    {
        if (Handle == IntPtr.Zero)
            return ["None"];

        nint inputs = Unsafe.Read<IntPtr>((void*)(Handle + 0x60));
        
        // System.Console.WriteLine(moveMent); // Use this to see the value of the button you are pressing

        var binary = Convert.ToString(inputs, 2);
        binary = binary.PadLeft(64, '0');
        
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
        Unsafe.Write((void*)(userCmd + 0x50), Unsafe.Read<IntPtr>((void*)(userCmd + 0x50)) & ~(value));
    }
}