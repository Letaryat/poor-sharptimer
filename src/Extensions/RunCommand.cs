using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace SharpTimer;

public interface IRunCommand { 
    void Hook(Func<DynamicHook, HookResult> handler, HookMode mode);
    void Unhook(Func<DynamicHook, HookResult> handler, HookMode mode);
}

public class RunCommandWindows : IRunCommand
{
    public MemoryFunctionVoid<IntPtr, IntPtr, IntPtr, CCSPlayer_MovementServices> _RunCommand;
    public RunCommandWindows() {
        _RunCommand = new(GameData.GetSignature("RunCommand"));
    }

    public void Hook(Func<DynamicHook, HookResult> handler, HookMode mode) {
        _RunCommand.Hook(handler, mode);
    }

    public void Unhook(Func<DynamicHook, HookResult> handler, HookMode mode) {
        _RunCommand.Unhook(handler, mode);
    }
}

public class RunCommandLinux : IRunCommand
{
    public MemoryFunctionVoid<CCSPlayer_MovementServices, IntPtr> _RunCommand;
    public RunCommandLinux() {
        _RunCommand = new(GameData.GetSignature("RunCommand"));
    }

    public void Hook(Func<DynamicHook, HookResult> handler, HookMode mode) {
        _RunCommand.Hook(handler, mode);
    }

    public void Unhook(Func<DynamicHook, HookResult> handler, HookMode mode) {
        _RunCommand.Unhook(handler, mode);
    }
}