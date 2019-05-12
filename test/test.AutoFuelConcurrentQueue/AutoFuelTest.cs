using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using AutoFuelConcurrentQueue;
using NUnit.Framework;

namespace test.AutoFuelConcurrentQueue
{
    public class AutoFuelTest
    {
        private AutoFuelConcurrentQueue<string> _autoFuelConcurrentQueue;
        private IDataProvider<string> _dataProvider;
        private const int ThreadCount = 5;

        [SetUp]
        public void Setup()
        {
            _dataProvider = new MyDataProvider();
            _autoFuelConcurrentQueue = new AutoFuelConcurrentQueue<string>(10, _dataProvider);
        }


        [Test]
        public async Task Test()
        {
            var dataCounter = 0;
            var tasks = new Task[ThreadCount];
            for (var i = 0; i < ThreadCount; i++)
            {
                var i1 = i;
                tasks[i] = Task.Run(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            var fullname = await _autoFuelConcurrentQueue
                                .DequeueAsync(new CancellationTokenSource(20000).Token);
                            Interlocked.Increment(ref dataCounter);
                            Console.WriteLine($"{DateTime.Now:mm:ss.fff};Task_{i1};{fullname}");
                        }
                        catch (EndOfQueueException e)
                        {
                            break;
                        }
                    }

                    Console.WriteLine($"{DateTime.Now:mm:ss:fff};Task_{i1};end_of_queue");
                    return Task.CompletedTask;
                });
            }

            await Task.WhenAll(tasks);

            if(MyDataProvider.DataCount!=dataCounter-1)
                Assert.Fail("Not all the data has been processed");
        }
    }
}