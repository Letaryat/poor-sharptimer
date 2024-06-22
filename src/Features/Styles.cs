using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        public void SetNormalStyle(CCSPlayerController player)
        {
            playerTimers[player.Slot].currentStyle = 0; // reset currentStyle
            SetNormalGravity(player);
            playerTimers[player.Slot].changedStyle = true;
        }
        public void SetLowGravity(CCSPlayerController player)
        {
            playerTimers[player.Slot].currentStyle = 1; // 1 = low-gravity
            player!.Pawn.Value!.GravityScale = 0.5f;
            playerTimers[player.Slot].changedStyle = true;
        }

        public void SetHighGravity(CCSPlayerController player)
        {
            playerTimers[player.Slot].currentStyle = 5; // 5 = high-gravity
            player!.Pawn.Value!.GravityScale = 1.5f;
            playerTimers[player.Slot].changedStyle = true;
        }

        public void SetNormalGravity(CCSPlayerController player)
        {
            player!.Pawn.Value!.GravityScale = 1f;
        }

        public void SetSlowMo(CCSPlayerController player)
        {
            //playerTimers[player.Slot].currentStyle = ?; // ? = slowmo (its broken)
            //Schema.SetSchemaValue(player!.Pawn.Value!.Handle, "CBaseEntity", "m_flTimeScale", 0.5f);
            //Utilities.SetStateChanged(player!.Pawn.Value!, "CBaseEntity", "m_flTimeScale");
        }

        public void SetSideways(CCSPlayerController player)
        {
            playerTimers[player.Slot].currentStyle = 2; // 2 = sideways
            playerTimers[player.Slot].changedStyle = true;
        }

        public void SetOnlyW(CCSPlayerController player)
        {
            playerTimers[player.Slot].currentStyle = 3; // 3 = only w
            playerTimers[player.Slot].changedStyle = true;
        }

        public void Set400Vel(CCSPlayerController player)
        {
            playerTimers[player.Slot].currentStyle = 4; // 4 = 400vel
            playerTimers[player.Slot].changedStyle = true;
        }

        public void SetVelocity(CCSPlayerController player, Vector currentVel, int desiredVel)
        {
            if(currentVel.X > desiredVel) player!.PlayerPawn.Value!.AbsVelocity.X = desiredVel;
            if(currentVel.X < -desiredVel) player!.PlayerPawn.Value!.AbsVelocity.X = -desiredVel;
            if(currentVel.Y > desiredVel) player!.PlayerPawn.Value!.AbsVelocity.Y = desiredVel;
            if(currentVel.Y < -desiredVel) player!.PlayerPawn.Value!.AbsVelocity.Y = -desiredVel;
            if(currentVel.Z > desiredVel) player!.PlayerPawn.Value!.AbsVelocity.Z = desiredVel;
            if(currentVel.Z < -desiredVel) player!.PlayerPawn.Value!.AbsVelocity.Z = -desiredVel;
        }



        public string GetNamedStyle(int style)
        {
            switch(style)
            {
                case 0:
                    return "Normal";
                case 1:
                    return "Low Gravity";
                case 2:
                    return "Sideways";
                case 3:
                    return "OnlyW";
                case 4:
                    return "400vel";
                case 5:
                    return "High Gravity";
                default:
                    return "null";
            }
        }
        public double GetStyleMultiplier(int style)
        {
            switch(style)
            {
                case 0:
                    return 1; // 1.0x for normal
                case 1:
                    return 1.1; //1.1x for lowgrav
                case 2:
                    return 1.3; // 1.3x for sideways
                case 3:
                    return 1.33; // 1.33x for onlyw
                case 4:
                    return 1.5; // 1.5x for 400vel
                case 5:
                    return 1.3; // 1.3x for highgrav
                default:
                    return 1;
            }
        }
    }
}