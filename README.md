# Faster & Easier Multi-Treading with "Auto Fuel Concurrent Queue"

Let's say we have 20 jobs which needs to be done as fastest as possible and for the sake of the clarity we 
know the duration of each task as mentioned below 

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
## Sequential approach
Is to run them sequentially like this
```c#
foreach (var duration in TaskDurations)
{
    await Task.Delay(duration);
}
``` 
##### Total operation's duration:  86 seconds

![](https://i.ibb.co/MfK9hpp/image.png)

This is not acceptable at all so the better solution will be to use the
multi-threading which in case you are dealing with c# and dotnet the better approach will 
be the multi-tasking, which behind the scene is doing some resource efficient and smart multi-threading,
so let's do that.

## Typical Multi-Tasking approach

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

So far so good right ? we have decreased the total duration time by 50% by applying multi-tasking and 
executing our tasks simultaneously but still there are some issues with approach like  
- You need to know the number of the whole operations upfront in order to divide them 
fairly between threads/tasks
- If one the tasks stuck in any of the iterations therefore that iteration will be blocked 
which will result in blocking the whole operation 
- There is no easy way to inject dynamically new jobs into the operation after the operation has been 
started 

Having said all these, are you ready to meet the fastest and most efficient method to run these tasks simultaneously, so 
stick with me because from this point forward things will get a little tricky :)
 

## "Auto-Fuel Concurrent-Queue" approach

```
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

```
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

> Note: you can add tasks dynamically to the data source, and you do not need to worry 
about it , because each time a task is getting idle it will ask for a new job from the 
data provider and data provider will check for new data as the whole operation is ongoing 

You can give it try from [Nuget](https://www.nuget.org/packages/AutoFuelConcurrentQueue/)
```
Package Manager
Install-Package AutoFuelConcurrentQueue -Version 1.1.0

.Net CLI
dotnet add package AutoFuelConcurrentQueue --version 1.1.0
```

Happy codding :)


