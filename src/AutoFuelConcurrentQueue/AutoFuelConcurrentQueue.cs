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

        /// <summary>
        /// Base class constructor
        /// </summary>
        private AutoFuelConcurrentQueue()
        {
        }

        /// <summary>
        /// Base class constructor
        /// </summary>
        private AutoFuelConcurrentQueue(IEnumerable<T> collection) : base(collection)
        {
        }

        /// <summary>
        /// Auto fuel constructor 
        /// </summary>
        /// <param name="poolSize">The size which the queue will attempt to get
        /// new data if the count of the queue is lesser than this value</param>
        /// <param name="dataProvider">A data provider object</param>
        /// <param name="initialFueling">if false the queue will begin empty</param>
        public AutoFuelConcurrentQueue(int poolSize, IDataProvider<T> dataProvider
            , bool initialFueling = true)
        {
            _poolSize = poolSize;
            _dataProvider = dataProvider;

            if (initialFueling) Fuel().Wait();
        }

        /// <summary>
        /// Will add an item T to the queue
        /// </summary>
        /// <param name="item"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public new void Enqueue(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            base.Enqueue(item);
            _signal.Release();
        }

        /// <summary>
        /// Add an item T to the queue without releasing the lock 
        /// </summary>
        /// <param name="item"></param>
        /// <exception cref="ArgumentNullException"></exception>
        private void _Enqueue(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            base.Enqueue(item);
        }


        /// <summary>
        /// try DeQueue an item T from the queue if the queue is empty it will try to fetch new data
        /// if there is no more data returning from the data provider it will throw <exception cref="EndOfQueueException"></exception>
        /// </summary>
        /// <returns>An item T from the queue</returns>
        /// <exception cref="EndOfQueueException"></exception>
        public async Task<T> DequeueAsync()
        {
            await _signal.WaitAsync();

            T result;
            if (Count > 0)
            {
                TryDequeue(out result);
                _signal.Release();
            }
            else
            {
                await Fuel();
                if (Count == 0) throw new EndOfQueueException();
                TryDequeue(out result);
            }

            return result;
        }

        /// <summary>
        /// try DeQueue an item T from the queue if the queue is empty it will try to fetch new data
        /// if there is no more data returning from the data provider it will throw <exception cref="EndOfQueueException"></exception>
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <returns>An item T from the queue</returns>
        /// <exception cref="EndOfQueueException"></exception>
        public async Task<T> DequeueAsync(TimeSpan timeSpan)
        {
            await _signal.WaitAsync(timeSpan);

            T result;
            if (Count > 0)
            {
                TryDequeue(out result);
                _signal.Release();
            }
            else
            {
                await Fuel();
                if (Count == 0) throw new EndOfQueueException();
                TryDequeue(out result);
            }

            return result;
        }

        /// <summary>
        /// try DeQueue an item T from the queue if the queue is empty it will try to fetch new data
        /// if there is no more data returning from the data provider it will throw <exception cref="EndOfQueueException"></exception>
        /// </summary>
        /// <param name="milliseconds"></param>
        /// <returns>An item T from the queue</returns>
        /// <exception cref="EndOfQueueException"></exception>
        public async Task<T> DequeueAsync(int milliseconds)
        {
            await _signal.WaitAsync(milliseconds);

            T result;
            if (Count > 0)
            {
                TryDequeue(out result);
                _signal.Release();
            }
            else
            {
                await Fuel();
                if (Count == 0) throw new EndOfQueueException();
                TryDequeue(out result);
            }

            return result;
        }

        /// <summary>
        /// try DeQueue an item T from the queue if the queue is empty it will try to fetch new data
        /// if there is no more data returning from the data provider it will throw <exception cref="EndOfQueueException"></exception>
        /// </summary>
        /// <param name="milliseconds"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>An item T from the queue</returns>
        /// <exception cref="EndOfQueueException"></exception>
        public async Task<T> DequeueAsync(int milliseconds, CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(milliseconds, cancellationToken);

            T result;
            if (Count > 0)
            {
                TryDequeue(out result);
                _signal.Release();
            }
            else
            {
                await Fuel();
                if (Count == 0) throw new EndOfQueueException();
                TryDequeue(out result);
            }

            return result;
        }

        /// <summary>
        /// try DeQueue an item T from the queue if the queue is empty it will try to fetch new data
        /// if there is no more data returning from the data provider it will throw <exception cref="EndOfQueueException"></exception>
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>An item T from the queue</returns>
        /// <exception cref="EndOfQueueException"></exception>
        public async Task<T> DequeueAsync(TimeSpan timeSpan, CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(timeSpan, cancellationToken);

            T result;
            if (Count > 0)
            {
                TryDequeue(out result);
                _signal.Release();
            }
            else
            {
                await Fuel();
                if (Count == 0) throw new EndOfQueueException();
                TryDequeue(out result);
            }

            return result;
        }

        /// <summary>
        /// try DeQueue an item T from the queue if the queue is empty it will try to fetch new data
        /// if there is no more data returning from the data provider it will throw <exception cref="EndOfQueueException"></exception>
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>An item T from the queue</returns>
        /// <exception cref="EndOfQueueException"></exception>
        public async Task<T> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);

            T result;
            if (Count > 0)
            {
                TryDequeue(out result);
                _signal.Release();
            }
            else
            {
                await Fuel();
                if (Count == 0) throw new EndOfQueueException();
                TryDequeue(out result);
            }

            return result;
        }


        /// <summary>
        /// This method will try to fetch "PoolSize-Count" number of data from the data provider 
        /// </summary>
        /// <returns></returns>
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