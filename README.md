#Easy Multi-Tasking with "Auto Fuel ConcurrentQueue"

##Typical Multi-Tasking 
![](https://i.ibb.co/wLhJLgN/Auto-Fuel-Concurrent-queue-Typical.png)
As it can be seen in a typical multi-tasking scenario the amount of the time 
which is require for the whole operation to be complete can be calculated like this

![](https://i.ibb.co/5TRcZ0p/image.png)

> Each iteration's duration is equal to the longest task's execution time.
####Example: 
Let's say there are total of 40 jobs and have decided to dedicate 10 tasks to be executed 
simultaneously therefore following outcome is possible 


|T1|T2   |T3   |T4   |T5   |T6   |T7   |T8   |T9   |T10   |Max time|
|---|---|---|---|---|---|---|---|---|---|---|
|1.23   |1.25   |0.66   |1.0   |2.5   |1.78   |5.0   |1.54   |9.5   |0.23   |9.5|
|2.23   |1.25   |1.5   |1.0   |2.5   |1.78   |5.0   |1.54   |4.3   |0.23   |4.3|
|1.53   |1.25   |0.78   |1.0   |2.5   |10.78   |5.0   |1.54   |9.5   |0.23   |10.78|
|6.23   |1.25   |0.66   |1.0   |2.5   |1.78   |5.0   |1.45   |9.5   |0.23   |9.5|
| | | | | | | | | | Total |25.17 |

As it can be seen in each iteration all the tasks needs to wait for the one which took 
the longest

#Issues

- We have to know the number of the whole operation upfront 
- If one task stuck all the operation has to wait for it 
- There is no easy way to inject dynamically new jobs into the operation 

#Solution
###Multi-Tasking with "Auto Fuel Concurrent Queue"
![](https://i.ibb.co/1Zt5vvJ/Auto-Fuel-Concurrent-queue-Auto-Fuel.png)

Test it for yourself

```c#
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

        if (MyDataProvider.DataCount != dataCounter - 1)
            Assert.Fail("Not all the data has been processed");
    }
```

"AutoFuelConcurrentQueue" needs this in order to fetch its new jobs 

```c#
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
```

and you will get a result like this

![](https://i.ibb.co/k8GypB4/image.png)

You can give it try from [here](https://www.nuget.org/packages/AutoFuelConcurrentQueue/)
```
Package Manager
Install-Package AutoFuelConcurrentQueue -Version 1.1.0

.Net CLI
dotnet add package AutoFuelConcurrentQueue --version 1.1.0
```

