# Documentation for `TaskRoutineManager.cs`

This file provides a convenient coroutine API for Unity via the `TaskRoutine` class. It allows you to start, pause, stop, and chain coroutines in a more flexible way than the built-in Unity `Coroutine` system. The underlying system lazily creates a `TaskRoutineManager` GameObject (with an internal `TaskManager` component) on first use of the API.

---

## Table of Contents
- [Documentation for `TaskRoutineManager.cs`](#documentation-for-taskroutinemanagercs)
  - [Table of Contents](#table-of-contents)
  - [Overview](#overview)
  - [Class: `TaskRoutine`](#class-taskroutine)
    - [Properties](#properties)
    - [Delegates](#delegates)
    - [Events](#events)

---

## Overview

A **TaskRoutine** represents a coroutine that can be:
- **Started** (begins execution immediately or on demand).
- **Paused** and then **Unpaused**.
- **Cancelled** (stopped prematurely).
- **Resolved** (manually completed without marking it as cancelled).
- **Chained** together (to sequentially run multiple coroutines).

The `TaskRoutineManager` is automatically created in the scene the first time you use any of these `TaskRoutine` static methods. It routes all coroutine logic through a single central manager object.

---

## Class: `TaskRoutine`

**Namespace**: `IDEK.Tools.Coroutines.TaskRoutines`

A `TaskRoutine` object encapsulates a coroutine (`IEnumerator`). Tasks can be manually started, paused, unpaused, stopped, or chained with other tasks.

### Properties

| Property               | Description                                                                                                            |
|------------------------|------------------------------------------------------------------------------------------------------------------------|
| **IsRunning** (`bool`) | Returns `true` if and only if the coroutine is running or paused (i.e., has not been cancelled or finished).           |
| **IsPaused** (`bool`)  | Returns `true` if the coroutine is currently paused.                                                                   |
| **IsResolved** (`bool`)| Returns `true` if the coroutine has completed successfully (without being cancelled).                                  |
| **IsCancelled** (`bool`)| Returns `true` if the coroutine's execution has been cancelled or if it has been destroyed.                           |
| **HasNeverBeenStarted** (`bool`) | Indicates whether the coroutine has never been started.                                                      |
| **Destroyed** (`bool`) | Indicates whether this `TaskRoutine` has been irreversibly disposed of (so it cannot be restarted).                    |
| **Previous** (`TaskRoutine`) | The routine that is expected to run immediately before this one in a chain.                                      |
| **Next** (`TaskRoutine`)     | The routine that is expected to run immediately after this one in a chain.                                       |
| **Progress** (`float`)       | A floating-point value intended to track progress. Not automatically updated by all tasks, but available for use. |

### Delegates

| Delegate                                        | Description                                                                                                                      |
|-------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------|
| **FinishedHandlerWithCancelCheck** (`bool wasCancelled`) | Signature for a handler invoked when a `TaskRoutine` finishes execution. The boolean indicates if the finish was due to cancellation. |
| **FinishedHandlerWithTaskResultAndCancelCheck** (`bool wasCancelled`) | Similar to the above, but returning a `TaskRoutine`. (Currently commented out in this code.)                                      |
| **TaskRoutineFunc**                             | Represents a function that returns an `IEnumerator`. Used when creating new routines via a callback.                             |
| **TaskResultFunc<T>**                           | Represents a function that takes a `TaskResult<T>` and returns an `IEnumerator`.                                                 |

### Events

| Event            | Description                                                       |
|------------------|-------------------------------------------------------------------|
| **FinishedEvent** | Invoked when the coroutine completes execution. The single parameter is a boolean `wasCancelled` that indicates if the stop was explicit. |

TODO: Finish

