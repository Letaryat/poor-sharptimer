using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;

[StructLayout(LayoutKind.Sequential)]
internal struct CUtlMemory
{
    public unsafe nint* m_pMemory;
    public int m_nAllocationCount;
    public int m_nGrowSize;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CUtlVector
{
    public unsafe nint this[int index]
    {
        get => m_Memory.m_pMemory[index];
        set => m_Memory.m_pMemory[index] = value;
    }

    public int m_iSize;
    public CUtlMemory m_Memory;

    public nint Element(int index)
    {
        return this[index];
    }
}

internal class INetworkServerService : NativeObject
{
    private readonly VirtualFunctionWithReturn<nint, nint> GetIGameServerFunc;

    public INetworkServerService() : base(NativeAPI.GetValveInterface(0, "NetworkServerService_001"))
    {
        GetIGameServerFunc =
            new VirtualFunctionWithReturn<nint, nint>(Handle,
                GameData.GetOffset("INetworkServerService_GetIGameServer"));
    }

    public INetworkGameServer GetIGameServer()
    {
        return new INetworkGameServer(GetIGameServerFunc.Invoke(Handle));
    }
}

public class INetworkGameServer : NativeObject
{
    private static readonly int SlotsOffset = GameData.GetOffset("INetworkGameServer_Slots");

    private CUtlVector Slots;

    public INetworkGameServer(nint ptr) : base(ptr)
    {
        Slots = Marshal.PtrToStructure<CUtlVector>(Handle + SlotsOffset);
    }

    public CServerSideClient? GetClientBySlot(int playerSlot)
    {
        if (playerSlot >= 0 && playerSlot < Slots.m_iSize)
            return Slots[playerSlot] == IntPtr.Zero ? null : new CServerSideClient(Slots[playerSlot]);

        return null;
    }
}

public class CServerSideClient : NativeObject
{
    private static readonly int m_nForceWaitForTick = GameData.GetOffset("CServerSideClient_m_nForceWaitForTick");

    public CServerSideClient(nint ptr) : base(ptr)
    {
    }

    public unsafe int ForceWaitForTick
    {
        get => *(int*)(Handle + m_nForceWaitForTick);
        set => *(int*)(Handle + m_nForceWaitForTick) = value;
    }

    public void ForceFullUpdate()
    {
        ForceWaitForTick = -1;
    }
}