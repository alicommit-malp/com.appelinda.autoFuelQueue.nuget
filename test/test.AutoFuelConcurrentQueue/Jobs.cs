using System.Collections.Generic;

namespace test.AutoFuelConcurrentQueue
{
    public static class Jobs
    {
        public static readonly List<int> TaskDurations = new List<int>()
        {
            1, 2, 3, 22, 4,
            5, 2, 1, 2, 3,
            4, 9, 10, 1, 2,
            3, 4, 5, 2, 1
        };
    }
}