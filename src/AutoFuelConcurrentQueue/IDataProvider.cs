using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoFuelConcurrentQueue
{
    public interface IDataProvider<T>
    {
        Task<IEnumerable<T>> GetNewData(int count);
    }
}