using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;

namespace test.AutoFuelConcurrentQueue.Typical
{
    public class TypicalMultiTaskingTest
    {
        private readonly ConcurrentQueue<int> _queue = new ConcurrentQueue<int>();

        [SetUp]
        public void Setup()
        {
            //populate the queue 
            foreach (var data in Jobs.TaskDurations)
            {
                _queue.Enqueue(data);
            }
        }

        [Test]
        public async Task TestTypical()
        {
            var sw = new Stopwatch();
            sw.Start();

            for (var i = 1; i <= 4; i++)
            {
                var iSw= new Stopwatch();
                iSw.Start();
                
                var tasks = new Task[5];
                for (var j = 0; j < 5; j++)
                {
                    tasks[j] = Task.Run(async () =>
                    {
                        //do the operation 
                        _queue.TryDequeue(out var delay);
                        await Task.Delay((int) TimeSpan.FromSeconds(delay).TotalMilliseconds);
                        Console.Write($"{delay},");
                    });
                }

                await Task.WhenAll(tasks);
                
                iSw.Stop();
                Console.Write($"Iteration: {i} Time: {iSw.Elapsed}\n");
            }


            sw.Stop();
            Console.WriteLine($"Total Time consumed: {sw.Elapsed}");
        }
    }
}