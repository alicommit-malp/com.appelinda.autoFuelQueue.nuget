using System;
<<<<<<< HEAD:test/test.AutoFuelConcurrentQueue/AutoFuelTest.cs
=======
using System.Diagnostics;
>>>>>>> e09193f32e00b33644967d23217e1debfb5a9b1f:test/test.AutoFuelConcurrentQueue/AutoFuel/AutoFuelTest.cs
using System.Threading;
using System.Threading.Tasks;
using AutoFuelConcurrentQueue;
using NUnit.Framework;

namespace test.AutoFuelConcurrentQueue.AutoFuel
{
    public class AutoFuelTest
    {
        private AutoFuelConcurrentQueue<int> _autoFuelConcurrentQueue;
        private IDataProvider<int> _dataProvider;
        private const int TaskCount = 5;

        [SetUp]
        public void Setup()
        {
            _dataProvider = new MyDataProvider();
            _autoFuelConcurrentQueue = new AutoFuelConcurrentQueue<int>(10, _dataProvider);
        }


        [Test]
        public async Task Test()
        {
            var sw = new Stopwatch();
            sw.Start();
            
            var dataCounter = 0;
            var tasks = new Task[TaskCount];
            for (var i = 0; i < TaskCount; i++)
            {
                var i1 = i;
                tasks[i] = Task.Run(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            var delay = await _autoFuelConcurrentQueue.DequeueAsync();
                            Interlocked.Increment(ref dataCounter);
                            await Task.Delay((int) TimeSpan.FromSeconds(delay).TotalMilliseconds);
                            Console.WriteLine($"{DateTime.Now:mm:ss.fff};Task_{i1};{delay}");
                        }
                        catch (EndOfQueueException)
                        {
                            Console.WriteLine($"{DateTime.Now:mm:ss:fff};Task_{i1};end_of_queue");
                            break;
                        }
                    }

                    return Task.CompletedTask;
                });
            }

            await Task.WhenAll(tasks);
            sw.Stop();
            Console.WriteLine($"Total Time consumed: {sw.Elapsed}");
        }
    }
}