# Faster & Easier Multi-Treading in batch processing with "Auto Fuel Concurrent Queue" 
Let's say we need to process 1,000,000,000 banking transactions and we have a limited time span to do so, in such a scenario parallelism is the only solution in order to boost the overall completion time.

If a cluster of computers is not available then multi-threading is the best solution currently available, but the only tricky question in multi-threading is the proper number of the threads, so little and the completion time will last forever, too many and the CPU's context switch will backfire, therefore a wise developer will always consider the system's resources before coming up with the magic number of the thread-count, like CPU's core-count, memory and so on ...

Anyhow, Let's suppose that the proper number has been determined to be 10,000 threads, which means we have to divide the 1,000,000,000 jobs into 10,000 chunks which it means that we will end up with 100,000 operations per thread. and Let's suppose that each operation will take maximum 5 seconds to be completed therefore 

```
(100,000 * 5 seconds) / (24 * 3600) = 5.7 Days  
```
It seams pretty straight forward right? except the 5 seconds completion time per operation is a wish, there is no guaranty, that each operation will take maximum 5 seconds to complete its task, the reason can be because of a lock in the database, or a network delay or so many other reasons, anyway in the reality the only thing that we can be sure about, is the minimum completion time, the maximum is always unknown, it can only be limited with a timeout, therefore 5.7 days is only an optimistic guess.

The underlying issue of the above solution is, despite of the fact that we have parallelized(ignoring the context switches) the operation into 10,000 threads, in each thread everything is taking place sequentially, This means that 100,000 operations will be processed one by one and after each other in every thread,therefore if the first operation in the thread-1 stuck for 1 hour , the completion time of the thread-1 will shift 1 hour forward, and it is extremely likely to encounter such a complications in 1,000,000,000 operations, especially when there are 10,000 threads running side by side.

This article will introduce you to the "Auto Fuel Concurrent Queue" which can help you to minimize the overall performance deficiency caused by the blocking operations which explained above, It can achieve this by not distributing the jobs into the threads beforehand, and instead, by creating 10,000 hungry threads which each one of them can process one job at a time, immediately after it finishes that job, it will ask for another one and this process will repeat itself, the "Auto Fuel Concurrent Queue" on the other hand, will act as a mother to these hungry threads, and as soon as it receives a request from any of these threads for a new job, it will try to retrieve it by using its Date provider interface, and in case there are any more new jobs left, it will provide the thread with it therefore the thread can continue to stay alive otherwise it has to kill itself.

Like this, if one thread stuck for an hour it doesn't matter anymore because the other threads are covering for it, and as soon as it finishes it can join the others immediately.        

Having said these, lets dive into the details and figure out how exactly this approach is useful and how does it work, to achieve this 1,000,000,000 jobs is a bit hard to imagine so, let's begin with 20 ones :)

Let's say we have 20 jobs which needs to be done as fast as possible and for the sake of the clarity we know the duration of each task as mentioned below 

```c#
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
```

In case you wonder why not using this 

```c#
var tasks = new List<Task>();

foreach (var duration in Jobs.TaskDurations)
{
    tasks.Add(Task.Delay((int) TimeSpan.FromSeconds(duration).TotalMilliseconds));
}

await Task.WhenAll(tasks);
```
is that, as the number of the tasks will increase lets say 1,000,000,000 records to process, the possibility to just use a simple for loop is fading away.  

## Typical Multi-Tasking by chunk approach

```c#
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
                await Task.Delay((int) TimeSpan.FromSeconds(delay)
                    .TotalMilliseconds);
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
```

##### Total operation's duration: 41 seconds 

![](https://i.ibb.co/5TRcZ0p/image.png)


|Iteration|Task 1|Task 2   |Task 3   |Task 4   |Task 5     |Max duration|
|---|---|---|---|---|---|---|
|1|1   |2   |3   |22  |4   |22   |
|2|5   |2   |1   |2   |3   |5   |
|3|4   |9   |10  |1   |2   |10  |
|4|3   |4   |5   |2   |1   |5  |
|#|    |    |    |    |Total  |42 (Seconds) |

So far so good right ? we have decreased the total duration time by 50% by applying multi-tasking and executing our tasks simultaneously but there are still some issues with approach:  
- You need to know the number of the whole operations upfront in order to divide them fairly between threads/tasks
- If one of the tasks stuck in any of the iterations therefore that iteration will be blocked which will result in blocking the whole operation 
- There is no easy way to inject dynamically new jobs into the operation after the operation has been started 

Having said all these, are you ready to meet the fastest and most efficient method to run these tasks simultaneously, so stick with me because from this point forward things will get a little tricky :)
 

## "Auto-Fuel Concurrent-Queue" approach

```c#
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
```

"AutoFuelConcurrentQueue" needs a DataProvider class to be defined in order to fetch its new jobs

```c#
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
```

##### Total operation's duration: 22 seconds 

![](https://i.ibb.co/DkJShdZ/image.png)

This is how it does its work behind the scene

![](https://i.ibb.co/q0ZcL9G/Auto-Fuel-Concurrent-queue-Auto-Fuel.png)

> Note: you can add tasks dynamically to the data source, and you do not need to worry about it , because each time a task is getting idle it will ask for a new job from the data provider and data provider will check for new data as the whole operation is ongoing 

You can give it try from [Nuget](https://www.nuget.org/packages/AutoFuelConcurrentQueue/)
```
Package Manager
Install-Package AutoFuelConcurrentQueue -Version 1.1.0

.Net CLI
dotnet add package AutoFuelConcurrentQueue --version 1.1.0
```

Happy codding :)
