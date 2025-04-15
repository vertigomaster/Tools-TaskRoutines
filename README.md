# TaskRoutines

Amaze your devs with the power of TaskRoutines, a powerful and flexible wrapper for working with Coroutines in a more robust way, marrying the power of "asynchronous" workflows within the constraints of Unity's Coroutine framework.

While not technically asynchronous, Coroutines enable a similar deferral of game logic within the safety of the main thread. Base Coroutines, however, have some significant limitations, which this package aims to lift with minimal hassle.

---

## Features

### 1. **TaskRoutine**
- A wrapper for Unity Coroutines that allows for:
  - Starting, pausing, resuming, and stopping routines.
  - Chaining routines together with `.OnFinish()` for sequential execution.
  - Delayed starts using `TaskRoutine.DelayedStart()`. (Experimental)
  - Retry logic with `TaskRoutine.Retry()` for robust error handling.
  - Progress tracking and cancellation checks.
- Supports `TaskRoutine.Start()` for immediate execution or `TaskRoutine.New()` for deferred creation.

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

### Using TaskResult
```csharp
TaskResult<int> result = TaskRoutine.Start<int>(asyncResult =>
{
    asyncResult.Value = 42;
    yield return null;
});

result.OnFinish(value => Debug.Log($"Result: {value}"));
```

### Delayed Start
```csharp
TaskRoutine delayedTask = TaskRoutine.DelayedStart(5f, MyTask);
```

---

## Installation
1. Add the package to your Unity project.
2. Use `TaskRoutine` and related classes in your scripts to enhance coroutine workflows.

---

## Why TaskRoutines?
TaskRoutines provide a more structured and feature-rich approach to Unity Coroutines, enabling developers to write cleaner, more maintainable, and more powerful asynchronous workflows.



