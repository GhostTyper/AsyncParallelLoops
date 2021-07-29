# Introduction

I'll programmed this code due to the lack of an async parallel foreach in the [Task Parallel Library (TPL) by Microsoft](https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/task-parallel-library-tpl). However, I didn't do proper research and you will most likely find better software or already made nuget packages out there.

The aim of this software is to provide you a safe environment to execute methods (or anonymous delegates) from `IEnumerable` data sources.

# Minimal example

This is an example with minimal parameters. It will just use 4 threads to execute on all enumerated elements:

```csharp
await ParallelLoops.ForEach<int, object>(Enumerable.Range(0, 12), async (number, holder) => {
    Console.WriteLine($" * {number}");
    await Task.Delay(1000);
});
```

It will output the numbers 0 to 11 unordered with a delay of 1 second per number per thread. Due to the fact that is executed with 4 threads it takes approximately 3 seconds.

# Error handling

If an exception gets thrown the exception will be pushed out after the `ForEach` call.

```csharp
await ParallelLoops.ForEach<int, object>(Enumerable.Range(0, 12), async (number, holder) => {
    Console.WriteLine($" * {number}");

    if (number == 4)
        throw new InvalidOperationException("4 is not allowed.");

    await Task.Delay(1000);
});

// Exception get's thown here.
```

You can add an exception handler callback so that you get notified about each thrown exception. However, if an exception gets thrown in the exception handler callback, than the exception will be pulled out at the end of the `ForEach` call:

```csharp
await ParallelLoops.ForEach<int, object>(Enumerable.Range(0, 12), async (number, holder) => {
    Console.WriteLine($" * {number}");

    if (number == 4)
        throw new InvalidOperationException("4 is not allowed.");

    await Task.Delay(1000);
}, exception: async (number, holder, exception) => {
    Console.WriteLine($" => Oh no, an exception at {number}: {exception.Message}");
});
```

The console will output this - so the error is processed right after it happened:

```
 * 0
 * 2
 * 3
 * 1
 * 4
 * 7
 * 6
 * 5
 => Oh no, an exception at 4: 4 is not allowed.
 * 8
 * 10
 * 9
 * 11
```

# Maximal example

This is a maximum example with all parameters setup. It is used to execute an initializer method and a finalizer method for each thread created. In general methods are executed in this order per thread:

1. The `init` method will be called for each thread started, initializing the holder.
1. The `body` method will be called for each result of the enumeration. Whenever an `Exception` is thrown within the `body` method the `exception` method will be called.
1. The `finalize` method will be called for each thread created and should dispose whatever necessary in the given holder class.

```csharp
await ParallelLoops.ForEach<DatabaseTaskMetaInfos, SqlConnection>(list<DatabaseTaskMetaInfos>,
    async (dbTasks, connection) => {
    await dbTasks.DoIt(connection);
}, exception: async (number, connection, exception) => {
    Console.WriteLine($" => Oh no, error: {exception.Message}");
}, init: async () => {
    SqlConnection connection = new SqlConnection("connectionString");

    await connection.OpenAsync();

    return connection;
}, finalize: async (sqlConnection) => {
    await sqlConnection.DisposeAsync();
}, threads: 7);
```

This example initializes a database connection for each thread and cleans it up after the run. It executes tasks given by a list with 7 threads in parallel.