using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoFuelConcurrentQueue
{
    public class AutoFuelConcurrentQueue<T> : ConcurrentQueue<T>
    {
        private readonly int _poolSize;
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);
        private readonly IDataProvider<T> _dataProvider;

        private AutoFuelConcurrentQueue()
        {
        }

        private AutoFuelConcurrentQueue(IEnumerable<T> collection) : base(collection)
        {
        }

        public AutoFuelConcurrentQueue(int poolSize, IDataProvider<T> dataProvider
            ,bool initialFueling = true)
        {
            _poolSize = poolSize;
            _dataProvider = dataProvider;

            if (initialFueling) Fuel().Wait();
        }

        public new void Enqueue(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            base.Enqueue(item);
            _signal.Release();
        }

        private void _Enqueue(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            base.Enqueue(item);
        }

        public async Task<T> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);

            T result;
            if (Count >0)
            {
                TryDequeue(out result);
                _signal.Release();
            }
            else
            {
                await Fuel();
                if (Count == 0) throw new AutoFuelConcurrentQueueTerminationException();
                TryDequeue(out result);
            }

            return result;
        }


        private async Task Fuel()
        {
            var newData = await _dataProvider.GetNewData(_poolSize - Count);
            var enumerable = newData.ToList();
            foreach (var item in enumerable)
            {
                _Enqueue(item);
            }

            _signal.Release();
        }
    }
}