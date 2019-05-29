using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFuelConcurrentQueue;

namespace test.AutoFuelConcurrentQueue.AutoFuel
{
    public class MyDataProvider : IDataProvider<int>
    {
        private int _consumedCount = 0;
        public async Task<IEnumerable<int>> GetNewData(int count)
        {
            await Task.Delay(0);

            //get limit number of new data
            var result = Jobs.TaskDurations.AsQueryable().Skip(_consumedCount).Take(count);
            _consumedCount += count;
            return result;
        }
    }
}