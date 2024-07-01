using CounterStrikeSharp.API.Core;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        public void setStyle(CCSPlayerController player, int style)
        {
            AddTimer(0.1f, () =>
            {
                SetNormalStyle(player);
                switch (style)
                {
                    case 0:
                        SetNormalStyle(player);
                        return;
                    case 1:
                        SetLowGravity(player);
                        return;
                    case 2:
                        SetSideways(player);
                        return;
                    case 3:
                        SetOnlyW(player);
                        return;
                    case 4:
                        Set400Vel(player);
                        return;
                    case 5:
                        SetHighGravity(player);
                        return;
                    case 6:
                        SetOnlyA(player);
                        return;
                    case 7:
                        SetOnlyD(player);
                        return;
                    case 8:
                        SetOnlyS(player);
                        return;
                    case 9:
                        SetHalfSideways(player);
                        return;
                    case 10:
                        SetFastForward(player);
                        return;
                    default:
                        return;
                }
            });
        }

        public void SetNormalStyle(CCSPlayerController player)
        {
            playerTimers[player.Slot].currentStyle = 0; // reset currentStyle
            playerTimers[player.Slot].changedStyle = true;
            player!.Pawn.Value!.GravityScale = 1f;
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
        public void SetHalfSideways(CCSPlayerController player)
        {
            playerTimers[player.Slot].currentStyle = 9; // 9 = halfsideways
            playerTimers[player.Slot].changedStyle = true;
        }
        public void SetFastForward(CCSPlayerController player)
        {
            playerTimers[player.Slot].currentStyle = 10; // 10 = fastforward
            playerTimers[player.Slot].changedStyle = true;
        }

        public void SetOnlyW(CCSPlayerController player)
        {
            playerTimers[player.Slot].currentStyle = 3; // 3 = only w
            playerTimers[player.Slot].changedStyle = true;
        }
        public void SetOnlyA(CCSPlayerController player)
        {
            playerTimers[player.Slot].currentStyle = 6; // 6 = only a
            playerTimers[player.Slot].changedStyle = true;
        }
        public void SetOnlyD(CCSPlayerController player)
        {
            playerTimers[player.Slot].currentStyle = 7; // 7 = only d
            playerTimers[player.Slot].changedStyle = true;
        }
        public void SetOnlyS(CCSPlayerController player)
        {
            playerTimers[player.Slot].currentStyle = 8; // 8 = only s
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
            //do not cap z velocity
        }

        public void IncreaseVelocity(CCSPlayerController player)
        {
            var currentSpeedXY = Math.Round(player!.Pawn.Value!.AbsVelocity.Length2D());
            var targetSpeed = currentSpeedXY + 5;

            AdjustPlayerVelocity2D(player, (float)targetSpeed);
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
                case 6:
                    return "OnlyA";
                case 7:
                    return "OnlyD";
                case 8:
                    return "OnlyS";
                case 9:
                    return "Half Sideways";
                case 10:
                    return "Fast Forward";
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
                    return lowgravPointModifier; //1.1x for lowgrav
                case 2:
                    return sidewaysPointModifier; // 1.3x for sideways
                case 3:
                    return onlywPointModifier; // 1.33x for onlyw
                case 4:
                    return velPointModifier; // 1.5x for 400vel
                case 5:
                    return highgravPointModifier; // 1.3x for highgrav
                case 6:
                    return onlyaPointModifier; // 1.33x for onlya
                case 7:
                    return onlydPointModifier; // 1.33x for onlyd
                case 8:
                    return onlysPointModifier; // 1.33x for onlys
                case 9:
                    return halfSidewaysPointModifier; // 1.3x for halfsideways
                case 10:
                    return fastForwardPointModifier; // 1.3x for halfsideways
                default:
                    return 1;
            }
        }
    }
}