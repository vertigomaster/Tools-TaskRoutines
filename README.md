# TaskRoutines

Amaze your devs with the power of TaskRoutines, a powerful and flexible wrapper for working with Coroutines in a more robust way, marrying the power of "asynchronous" workflows within the constraints of Unity's Coroutine framework.

While not technically asynchronous, Coroutines enable concurrent workflows within the safety of the main thread. Base Coroutines, however, have some significant limitations, which this package aims to lift with minimal hassle.

---

> See the Docs/ directory for more detailed documentation.

## Features

### 1. **TaskRoutine**
- A wrapper for Unity Coroutines that allows for:
  - Starting, pausing, resuming, and stopping routines.
  - Chaining routines together with `.OnFinish()` for sequential execution.
    - OnFinish() callbacks can also get feedback on whether the TaskRoutine "chain" was cancelled before the callback executes, allowing you to automatically cancel the callback or to conditionally react to both cases within the callback.
  - Delayed starts using `TaskRoutine.DelayedStart()`. (Experimental)
  - Retry logic with `TaskRoutine.Retry()` for robust error handling.
  - Progress tracking and cancellation checks.
  - Integrating TaskRoutines back into traditional Coroutine workflows
- Supports `TaskRoutine.Start()` for immediate execution or `TaskRoutine.New()` for deferred creation.
- Easily delaying synchronous logic (void => void functions can be passed in in lieu of actual routines!)
- A whole cohort of flexible static helper functions
  - TaskRoutine.Yield()
  - TaskRoutine.WaitUntil()
  - TaskRoutine.WaitWhile()
  - TaskRoutine.Delay()
  - TaskRoutine.Defer()
  - and more! I keep adding stuff faster than I can update these docs, so feel free to browse! 
    - Github's code viewer has a Symbols tab that shows everything we've added
  

### 2. **TaskResult** (Experimental)
- Extends `TaskRoutine` to include return values.
- Allows routines to return results while maintaining coroutine behavior.
- Supports chaining with `.OnFinish()` and provides callbacks for result handling.

### 3. **TaskRoutineManager**
- Manages the lifecycle of all TaskRoutines.
- Automatically creates a singleton GameObject to host coroutines.
- Provides internal state tracking for running, paused, resolved, and canceled routines.

### 4. **TaskRoutine-specific YieldInstructions**
- Special yield instructions for advanced control:
  - `CancelTaskRoutine`: Explicitly cancels a routine.
  - `ResolveTaskRoutine`: Completes a routine early with success.

### 5. **TaskRoutineExtensions**
- Utility methods for working with collections of TaskRoutines:
  - `OnAllFinish()`: Executes a callback or routine after all routines complete.
  - `CancelAll()`: Cancels all routines in a collection.
  - `AwaitAll()`: Waits for all routines to finish.

---

## Usage Examples

### Basic TaskRoutine
```csharp
IEnumerator MyTask()
{
    while (true)
    {
        yield return null;
    }
}

TaskRoutine myTask = TaskRoutine.Start(MyTask());
myTask.OnFinish(() => Debug.Log("Task completed!"));
```

### Chaining Routines
```csharp
TaskRoutine firstTask = TaskRoutine.Start(MyTask());
TaskRoutine secondTask = TaskRoutine.Start(AnotherTask());

firstTask.OnFinish(secondTask);
```

Or, more concisely:

```csharp
TaskRoutine.Start(MyTask).OnFinish(AnotherTask);
```
> The parentheses are optional, as TaskRoutines, IEnumerators (and later IEnumerables), *and* function delegates wich return either of those, can all be passed into most TaskRoutine methods, enabling both flexible and potentially concise syntaxes, especially with routines that don't take in arguments!
> 
> The varying amount of configurations also give you a lot of freedom to adapt TaskRoutines to your team's existing coding practices.

### Using TaskResult
```csharp
TaskResult<int> result = TaskRoutine.Start<int>(asyncResult =>
{
    FakeHTTP httpResponse = GetFancyNumberFromWebRequest();

    while (!httpResponse.Completed) 
    {
        yield return new WaitForSeconds(0.1f);
    }

    asyncResult.Value = httpResponse.payload != null ? 
        (int)httpResponse.payload : 0;
});

result.OnFinish(value => Debug.Log($"Result: {value}"));
```

Or, alternatively:

```csharp
FakeHTTP httpResponse = GetFancyNumberFromWebRequest();
TaskRoutine
    .WaitUntil(() => httpResponse.Completed)
    .OnFinish(() => Debug.Log($"Result: {httpResponse.payload}"));

```

Lots of flexibility here.

### Delayed Start
An easy way to do something later
```csharp
TaskRoutine delayedTask = TaskRoutine.DelayedStart(5f, MyTask);
```

### Defer
An easy way to just do something next frame
```csharp
DoAThing();
TaskRoutine.Defer(() => CleanUpThatThing());
```

or

```csharp
DoAThing();
TaskRoutine.Defer(CleanUpThatThing);
```


---

## Installation
1. Install `shocktrooper-utils` as a dependency 
   - currently contains the logger (ConsoleLog class) that TaskRoutine utilizes. 
   - Will eventually peel this out into its own package in a later release.
2. Add the `Tools-TaskRoutines` package to your Unity project.
3. Use `TaskRoutine` and related classes in your scripts to enhance coroutine workflows.


---

## Why TaskRoutines?
TaskRoutines provide a more structured and feature-rich approach to Unity Coroutines, enabling developers to write cleaner, more maintainable, and more powerful asynchronous workflows while still working within the safety and comfort of the main Unity thread.



