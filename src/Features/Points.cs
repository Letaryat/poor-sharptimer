using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml;
using CounterStrikeSharp.API.Core;

namespace SharpTimer
{
    public partial class SharpTimer
    {

        // Step 1
        // This function calculates basic map completion points
        // Start at 25 for T1, then 50, then 100, etc..
        public int CalculateCompletion()
        {
            if (currentMapTier is not null)
            {
                switch (currentMapTier)
                {
                    case 1:
                        return 25;
                    case 2:
                        return 50;
                    case 3:
                        return 100;
                    case 4:
                        return 200;
                    case 5:
                        return 400;
                    case 6:
                        return 600;
                    case 7:
                        return 800;
                    case 8:
                        return 1000;

                    default:
                        return 0;
                }
            }else{
                return 25;
            }
        }

        // Step 2
        // This function calculates tier ranking
        // This is the first step in calculating the players specific ranking on the map
        public double CalculateTier(int completions)
        {
            // Define max WR points for each tier (fallback to t1)
            int maxWR;
            int tier;
            if (currentMapTier is not null)
            {
                maxWR = 250 * (int)currentMapTier;
                tier = (int)currentMapTier;
            }
            else
            {
                maxWR = 250;
                tier = 1;
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
        public double CalculateTop10(double points, int position)
        {
            switch(position)
            {
                case 1:
                    return points;
                case 2:
                    return points * 0.8;
                case 3:
                    return points * 0.75;
                case 4:
                    return points * 0.7;
                case 5:
                    return points * 0.65;
                case 6:
                    return points * 0.6;
                case 7:
                    return points * 0.55;
                case 8:
                    return points * 0.5;
                case 9:
                    return points * 0.45;
                case 10:
                    return points * 0.4;

                default:
                    return 0;
            }
        }

        // Step 4
        // This function sorts players below top10, but above 50th percentile, into groups
        // These groups get less points than top 10, but still get points!
        public double CalculateGroups(double points, double percentile)
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
    }
}