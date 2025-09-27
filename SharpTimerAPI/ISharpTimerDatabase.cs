using CounterStrikeSharp.API.Core.Capabilities;

namespace SharpTimerAPI;

public interface ISharpTimerDatabase
{
    public static readonly PluginCapability<ISharpTimerDatabase> Capability = new("sharptimer:database");

    public class PlayerRecord
    {
        public int RecordID { get; set; }
        public string? PlayerName { get; set; }
        public string? SteamID { get; set; }
        public string? MapName { get; set; }
        public int TimerTicks { get; set; }
        public bool Replay { get; set; }
        public int Completions { get; set; }
    }
    
    public class PlayerPoints
    {
        public string? PlayerName { get; set; }
        public string? SteamID { get; set; }
        public int GlobalPoints { get; set; }
        public int Rank { get; set; }
    }
    public Task<Dictionary<int, PlayerRecord>> GetSortedRecordsFromDatabase(int limit = 0, int bonusX = 0, string mapName = "", int style = 0);
    public Task<List<PlayerRecord>> GetAllSortedRecordsFromDatabase(int limit = 0, int bonusX = 0, int style = 0);
    
    public Task<Dictionary<int, PlayerRecord>> GetTopLocalPlayersAsync(int limit = 0, int bonusX = 0, string mapName = "", int style = 0, string mode = "", string type = "");
    public Task<Dictionary<int, PlayerPoints>> GetTopLocalPointsAsync(int limit = 0);
}