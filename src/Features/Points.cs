namespace SharpTimer
{
    public partial class SharpTimer
    {

        // Step 1
        // This function calculates basic map completion points
        // Start at 25 for T1, then 50, then 100, etc..
        public int CalculateCompletion(bool forGlobal = false)
        {
            if (currentMapTier is not null)
            {
                switch (currentMapTier)
                {
                    case 1:
                        if (forGlobal)
                            return 25;
                        return baselineT1;
                    case 2:
                        if (forGlobal)
                            return 50;
                        return baselineT2;
                    case 3:
                        if (forGlobal)
                            return 100;
                        return baselineT3;
                    case 4:
                        if (forGlobal)
                            return 200;
                        return baselineT4;
                    case 5:
                        if (forGlobal)
                            return 400;
                        return baselineT5;
                    case 6:
                        if (forGlobal)
                            return 600;
                        return baselineT6;
                    case 7:
                        if (forGlobal)
                            return 800;
                        return baselineT7;
                    case 8:
                        if (forGlobal)
                            return 1000;
                        return baselineT8;
                    default:
                        return 25;
                }
            }
            return 25;
        }

        // Step 2
        // This function calculates tier ranking
        // This is the first step in calculating the players specific ranking on the map
        public async Task<double> CalculateTier(int completions, string mapname)
        {
            // Define max WR points for each tier (fallback to t1)
            int maxWR;
            int? tier;
            string _;

            if(disableRemoteData)
                (tier, _) = await FindMapInfoFromLocal(GetMapInfoSource(), mapname);
            else
                (tier, _) = await FindMapInfoFromHTTP(GetMapInfoSource(), mapname);
                
            if (tier is not null)
            {
                maxWR = maxRecordPointsBase * (int)tier;             // Get tier from remote_data by default
            }
            else if (currentMapTier is not null)
            {
                maxWR = maxRecordPointsBase * (int)currentMapTier;  // If remote_data tier doesnt exist, check local data
                tier = currentMapTier;
            }
            else
            {
                maxWR = maxRecordPointsBase;
                tier = 1;                                           // If nothing exists, tier = 1
            }

            switch (tier)
            {
                case 1:
                    return Math.Max(maxWR, 58.5 + (1.75 * completions) / 6);
                case 2:
                    return Math.Max(maxWR, 82.15 + (2.8 * completions) / 5);
                case 3:
                    return Math.Max(maxWR, 117 + (3.5 * completions) / 4);
                case 4:
                    return Math.Max(maxWR, 164.25 + (5.74 * completions) / 4);
                case 5:
                    return Math.Max(maxWR, 234 + (7 * completions) / 4);
                case 6:
                    return Math.Max(maxWR, 328 + (14 * completions) / 4);
                case 7:
                    return Math.Max(maxWR, 420 + (21 * completions) / 4);
                case 8:
                    return Math.Max(maxWR, 560 + (30 * completions) / 4);

                default:
                    return 0;
            }
        }

        // Step 3
        // This function takes the WR points from above and distributes them among the top 10
        public double CalculateTop10(double points, int position, bool forGlobal = false)
        {
            switch(position)
            {
                case 1:
                    if (forGlobal)
                        return points * 1;
                    return points * top10_1;
                case 2:
                    if (forGlobal)
                        return points * 0.8;
                    return points * top10_2;
                case 3:
                    if (forGlobal)
                        return points * 0.75;
                    return points * top10_3;
                case 4:
                    if (forGlobal)
                        return points * 0.7;
                    return points * top10_4;
                case 5:
                    if (forGlobal)
                        return points * 0.65;
                    return points * top10_5;
                case 6:
                    if (forGlobal)
                        return points * 0.6;
                    return points * top10_6;
                case 7:
                    if (forGlobal)
                        return points * 0.55;
                    return points * top10_7;
                case 8:
                    if (forGlobal)
                        return points * 0.5;
                    return points * top10_8;
                case 9:
                    if (forGlobal)
                        return points * 0.45;
                    return points * top10_9;
                case 10:
                    if (forGlobal)
                        return points * 0.4;
                    return points * top10_10;
                default:
                    return 0;
            }
        }

        // Step 4
        // This function sorts players below top10, but above 50th percentile, into groups
        // These groups get less points than top 10, but still get points!
        public double CalculateGroups(double points, double percentile, bool forGlobal = false)
        {
            if (forGlobal)
            {
                switch(percentile)
                {
                    case double p when p <= 3.125:
                        return points * 0.25; // Group 1
                    case double p when p <= 6.25:
                        return (points * 0.25) / 1.5; // Group 2
                    case double p when p <= 12.5:
                        return ((points * 0.25) / 1.5) / 1.5; // Group 3
                    case double p when p <= 25:
                        return (((points * 0.25) / 1.5) / 1.5) / 1.5; // Group 4
                    case double p when p <= 50:
                        return (((((points * 0.25) / 1.5) / 1.5) / 1.5) / 1.5); // Group 5
                    default:
                        return 0;
                }
            }
            switch(percentile)
            {
                case double p when p <= group1:
                    return points * 0.25; // Group 1
                case double p when p <= group2:
                    return (points * 0.25) / 1.5; // Group 2
                case double p when p <= group3:
                    return ((points * 0.25) / 1.5) / 1.5; // Group 3
                case double p when p <= group4:
                    return (((points * 0.25) / 1.5) / 1.5) / 1.5; // Group 4
                case double p when p <= group5:
                    return (((((points * 0.25) / 1.5) / 1.5) / 1.5) / 1.5); // Group 5
                default:
                    return 0;
            }
        }
    }
}