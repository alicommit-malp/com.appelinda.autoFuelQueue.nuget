using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFuelConcurrentQueue;
using Faker;

namespace test.AutoFuelConcurrentQueue
{
    public class MyDataProvider : IDataProvider<string>
    {
        private readonly List<string> _data;
        private int _consumedCount = 0;
        public const int DataCount = 100;

        public MyDataProvider()
        {
            //populating the list with fake data 
            _data = new List<string>();
            for (var i = 0; i <= DataCount; i++)
            {
                _data.Add(Name.FullName());
            }
        }

        public async Task<IEnumerable<string>> GetNewData(int count)
        {
            await Task.Delay(0);

            //get limit number of new data
            var result = _data.AsQueryable().Skip(_consumedCount).Take(count);
            _consumedCount += count;
            return result;
        }
    }
}