/*
Copyright (C) 2024 Dea Brcka

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        private CCSGameRules? _gameRules;

        private void UpdateGameRestartFlag()
        {
            if (_gameRules == null)
            {
                var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
                _gameRules = gameRulesProxy?.GameRules;
            }
            else if (!_gameRules.WarmupPeriod)
            {
                // Skip during warmup. RestartRoundTime idles at 0, so this expression is otherwise
                // always true and pins m_bGameRestart, and the engine's warmup-end handling does not
                // arm mp_roundtime while that flag is set. A never-ending-round mode
                // (mp_maxrounds 1 + mp_ignore_round_win_conditions 1) then keeps the 999 s warmup
                // round time for the whole map. Live play is unaffected. See #184.
                _gameRules.GameRestart = _gameRules.RestartRoundTime < Server.CurrentTime;
            }
        }
    }
}
