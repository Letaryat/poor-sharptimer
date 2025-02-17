namespace SharpTimer
{
    public partial class SharpTimer
    {

        // Step 1
        // This function calculates basic map completion points
        // Start at 25 for T1, then 50, then 100, etc..
        public int CalculateCompletion(bool forGlobal = false)
        {
            // If currentMapTier is null, default to 25.
            int tier = currentMapTier ?? 0;

            return tier switch
            {
                1 => forGlobal ? 25 : baselineT1,
                2 => forGlobal ? 50 : baselineT2,
                3 => forGlobal ? 100 : baselineT3,
                4 => forGlobal ? 200 : baselineT4,
                5 => forGlobal ? 400 : baselineT5,
                6 => forGlobal ? 600 : baselineT6,
                7 => forGlobal ? 800 : baselineT7,
                8 => forGlobal ? 1000 : baselineT8,
                _ => 25,
            };
        }

        // Step 2
        // This function calculates tier ranking
        // This is the first step in calculating the players specific ranking on the map
        public async Task<double> CalculateTier(int completions, string mapname)
        {
            // Define max WR points for each tier (fallback to t1)
            int maxWR;
            int? tier;
            string? _;
            
            if(disableRemoteData)
                (tier, _) = await FindMapInfoFromLocal(GetMapInfoSource(), mapname);
                
            else
                (tier, _) = await FindMapInfoFromHTTP(GetMapInfoSource(), mapname);
                
            if (tier != null)
            {
                maxWR = maxRecordPointsBase * (int)tier;             // Get tier from remote_data by default
            }
            else if (currentMapTier != null)
            {
                maxWR = maxRecordPointsBase * (int)currentMapTier;  // If remote_data tier doesnt exist, check local data
                tier = currentMapTier;
            }
            else
            {
                maxWR = maxRecordPointsBase;
                tier = 1;                                           // If nothing exists, tier = 1
            }

            return tier switch
            {
                1 => Math.Max(maxWR, 58.5 + (1.75 * completions) / 6),
                2 => Math.Max(maxWR, 82.15 + (2.8 * completions) / 5),
                3 => Math.Max(maxWR, 117 + (3.5 * completions) / 4),
                4 => Math.Max(maxWR, 164.25 + (5.74 * completions) / 4),
                5 => Math.Max(maxWR, 234 + (7 * completions) / 4),
                6 => Math.Max(maxWR, 328 + (14 * completions) / 4),
                7 => Math.Max(maxWR, 420 + (21 * completions) / 4),
                8 => Math.Max(maxWR, 560 + (30 * completions) / 4),
                _ => 0,
            };
        }

        // Step 3
        // This function takes the WR points from above and distributes them among the top 10
        public double CalculateTop10(double points, int position, bool forGlobal = false)
        {
            return position switch
            {
                1  => points * (forGlobal ? 1.0   : top10_1),
                2  => points * (forGlobal ? 0.8   : top10_2),
                3  => points * (forGlobal ? 0.75  : top10_3),
                4  => points * (forGlobal ? 0.7   : top10_4),
                5  => points * (forGlobal ? 0.65  : top10_5),
                6  => points * (forGlobal ? 0.6   : top10_6),
                7  => points * (forGlobal ? 0.55  : top10_7),
                8  => points * (forGlobal ? 0.5   : top10_8),
                9  => points * (forGlobal ? 0.45  : top10_9),
                10 => points * (forGlobal ? 0.4   : top10_10),
                _ => 0,
            };
        }

        // Step 4
        // This function sorts players below top10, but above 50th percentile, into groups
        // These groups get less points than top 10, but still get points!
        public double CalculateGroups(double points, double percentile, bool forGlobal = false)
        {
            double baseMultiplier = points * 0.25;
            double divisor = 1.5;
            
            double threshold1, threshold2, threshold3, threshold4, threshold5;
            if (forGlobal)
            {
                threshold1 = 3.125;
                threshold2 = 6.25;
                threshold3 = 12.5;
                threshold4 = 25;
                threshold5 = 50;
            }
            else
            {
                threshold1 = group1;
                threshold2 = group2;
                threshold3 = group3;
                threshold4 = group4;
                threshold5 = group5;
            }

            return percentile switch
            {
                double p when p <= threshold1 => baseMultiplier,
                double p when p <= threshold2 => baseMultiplier / divisor,
                double p when p <= threshold3 => baseMultiplier / (divisor * divisor),
                double p when p <= threshold4 => baseMultiplier / (divisor * divisor * divisor),
                double p when p <= threshold5 => baseMultiplier / (divisor * divisor * divisor * divisor),
                _ => 0,
            };
        }
    }
}
