// Created By: "chomp" (https://forum.unity.com/members/chomp.29811/)
// Found At: https://forum.unity.com/threads/a-more-flexible-coroutine-interface.94220/
// Pasted/Edited by: Julian Noel

/// TaskRoutineManager.cs
///
/// This is a convenient coroutine API for Unity.
///
/// Example usage:
///   IEnumerator MyAwesomeTask()
///   {
///       while(true) {
///           // ...
///           yield return null;
////      }
///   }
///
///   IEnumerator TaskKiller(float delay, Task t)
///   {
///       yield return new WaitForSeconds(delay);
///       t.Stop();
///   }
///
///   // From anywhere
///   TaskRoutine my_task = new TaskRoutine(MyAwesomeTask());
///   new TaskRoutine(TaskKiller(5, my_task));
///
/// The code above will schedule MyAwesomeTask() and keep it running
/// concurrently until either it terminates on its own, or 5 seconds elapses
/// and triggers the TaskKiller Task that was created.
///
/// Note that to facilitate this API's behavior, a "TaskRoutineManager" GameObject is
/// created lazily on first use of the Task API and placed in the scene root
/// with the internal TaskManager component attached. All coroutine dispatch
/// for Tasks is done through this component.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
// using IDEK.Tools.Logging;
using UnityEngine;
using UnityEngine.Events;

namespace IDEK.Tools.Coroutines.TaskRoutines
{
    /// A Task object represents a coroutine.  Tasks can be started, paused, and stopped.
    /// It is an error to attempt to start a task that has been stopped or which has
    /// naturally terminated.
    public class TaskRoutine
    {
        protected const string IENUMERATOR_DEPRECATION_WARNING =
            "When possible, use the IEnumerable overload instead of IEnumerator, " +
            "as IEnumerable routines support restarts and looping.";

        internal TaskRoutineManager.TaskState innerTaskState;

        /// <summary>
        /// Returns true if and only if the coroutine is running.  Paused tasks
        /// are considered to be running.
        /// </summary>
        public virtual bool IsRunning => !Destroyed && innerTaskState.Running;
        
        ///<summary>
        ///Returns true if and only if the coroutine is currently paused.
        ///</summary>
        public virtual bool IsPaused => !Destroyed && innerTaskState.Paused;

        ///<summary>
        /// Returns true if and only if the coroutine has successfully resolved.
        ///</summary>
        public virtual bool IsResolved => !Destroyed && innerTaskState.Resolved;

        ///<summary>
        /// Returns true if and only if the coroutine's execution has been cancelled.
        ///</summary>
        public virtual bool IsCancelled => Destroyed || innerTaskState.Cancelled;

        public virtual bool HasNeverBeenStarted => innerTaskState.HasNeverBeenStarted;

        public virtual bool Destroyed { get; protected set; } = false;

        /// <summary>
        /// The TaskRoutine currently expected to run before this one
        /// </summary>
        public virtual TaskRoutine Previous { get; protected set; }

        /// <summary>
        /// The TaskRoutine currently expected to run after this one (not accurate on branching routine chains)
        /// </summary>
        [Obsolete("Deprecated. Inherently does NOT handle branching, be aware when using branching chains. Depend on Previous instead.")]
        public virtual TaskRoutine Next { get; protected set; }

        /// <summary>
        /// Not guaranteed to be updated by all routines, but all at least provide a means
        /// </summary>
        public virtual float Progress {  get; set; }

        /// Delegate for termination subscribers.  manual is true if and only if
        /// the coroutine was stopped with an explicit call to Stop().
        public delegate void FinishedHandlerWithCancelCheck(bool wasCancelled);
        public delegate TaskRoutine FinishedHandlerWithTaskResultAndCancelCheck(bool wasCancelled);

        [Obsolete(IENUMERATOR_DEPRECATION_WARNING)]
        public delegate IEnumerator TaskRoutineFunc();

        /// <summary>
        /// Represents a function that RETURNS a <see cref="TaskRoutine"/> object, as opposed to a
        /// regular TaskRoutine object.
        /// These functions require special proxying in order to be chained, as OnFinish()
        /// requires an immediate handle to later invoke, but a chained LatentTaskRoutineFunc
        /// cannot be invoked during assignment without immediately invoking the logic leading up
        /// to its internal return of a TaskRoutine object, instead of only executing the function after the previous one.
        /// The non-static TaskRoutine.OnFinish() overloads are all built to enable chaining,
        /// meaning the function has to return a reference to a taskroutine that resolves after
        /// the TaskRoutine instance it was called from. This cannot be directly done with LatentTaskRoutineFuncs
        ///
        /// The OnFinish overload for these must create a TaskRoutineProxy,
        /// a wrapper that will invoke a later given TaskRoutine ref, or await its arrival
        /// Or otherwise return a special taskroutine
        /// </summary>
        public delegate TaskRoutine LatentTaskRoutineFunc();

        [Obsolete(IENUMERATOR_DEPRECATION_WARNING)]
        public delegate IEnumerator TaskResultFunc<T>(TaskResult<T> carriedResultRef);

        public delegate IEnumerable TaskRoutineEnumerableFunc();

        public delegate IEnumerable TaskResultEnumerableFunc<T>(TaskResult<T> carriedResultRef);

        /// Termination event.  Triggered when the coroutine completes execution.
        public event FinishedHandlerWithCancelCheck FinishedEvent;


        public static TaskRoutine Resolved => new TaskRoutine().Resolve();
        public static TaskRoutine Cancelled => new TaskRoutine().Cancel();

        
        // /// <summary>
        // /// A <see cref="TaskRoutine"/>, if created from an <see cref="IEnumerable"/>, will store it here
        // /// (and use it to create fresh <see cref="IEnumerator"/> instances for repeated runs).
        // /// </summary>
        // [CanBeNull]
        // public IEnumerable Enumerable { get; protected set; }

        protected TaskRoutine()
        {
            //the default is IEnumerator since it does not unexpectedly fill in expected data with null.
            //user-facing methods should not use IEnumerator directly though.
            innerTaskState = new(null as IEnumerator); //what a silly solution, lol
        }

        /// <summary>
        /// Creates a new Task object for the given coroutine.
        /// <br/>
        /// If autoStart is true (default) the task is automatically started
        /// upon construction. 
        /// </summary>
        [Obsolete(IENUMERATOR_DEPRECATION_WARNING)]
        public TaskRoutine(IEnumerator c, bool autoStart = true)
        {
            innerTaskState = TaskRoutineManager.CreateTask(c);
            innerTaskState.FinishedEvent += (wasCancelled) => FinishedEvent?.Invoke(wasCancelled);
            if(autoStart)
            {
                Start();
            }
        }

        /// <summary>
        /// Creates a new Task object for the given coroutine.
        /// <br/>
        /// If autoStart is true (default) the task is automatically started
        /// upon construction. 
        /// </summary>
        public TaskRoutine(IEnumerable c, bool autoStart = true)
        {
            innerTaskState = TaskRoutineManager.CreateTask(c);
            innerTaskState.FinishedEvent += (wasCancelled) => FinishedEvent?.Invoke(wasCancelled);
            if (autoStart)
            {
                Start();
            }
        }

        public static implicit operator bool(TaskRoutine routine)
        {
            return routine is { Destroyed: false };
        }

        public static TaskRoutine StartLoop(Action loopingAction) => NewLoop(loopingAction, true);
        
        public static TaskRoutine NewLoop(Action loopingAction, bool autoStart = false)
        {
            IEnumerable Local_LoopRoutine() { while (true) {
                loopingAction();
                yield return null;
            }}
            
            return New(Local_LoopRoutine(), autoStart);
        }

        public static TaskRoutine StartLoop(Action loopingAction, float repeatDelay) => 
            NewLoop(loopingAction, repeatDelay, true);

        public static TaskRoutine NewLoop(Action loopingAction, float repeatDelay, bool autoStart = false)
        {
            IEnumerable Local_LoopRoutine() { while (true) {
                loopingAction();
                yield return new WaitForSeconds(repeatDelay);
            }}

            return New(Local_LoopRoutine(), autoStart);

        }

        public static TaskRoutine StartLoop(Action loopingAction, Func<object> yieldInstructionFactory) => 
            NewLoop(loopingAction, yieldInstructionFactory, true);

        public static TaskRoutine NewLoop(Action loopingAction, Func<object> yieldInstructionFactory, bool autoStart = false)
        {
            IEnumerable Local_LoopRoutine() { while (true) {
                loopingAction();
                yield return yieldInstructionFactory();
            }}

            return New(Local_LoopRoutine(), autoStart);
        }

        #region Factories(?)
        /// <summary>
        /// More readable way to immediately start a TaskRoutine. <br/>
        /// This will IMMEDIATELY execute all code up to the first yield within the IEnumerator function inside the TaskRoutine.
        /// <para/>Analogous to: <code>new TaskRoutine(yourIEnumeratorRoutine, true);</code> or
        /// <code>TaskRoutine.New(yourIEnumeratorRoutine, true);</code>
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static TaskRoutine Start(IEnumerable c) => New(c, true);

        [Obsolete(IENUMERATOR_DEPRECATION_WARNING)]
        public static TaskRoutine Start(IEnumerator c) => New(c, true);

        public static TaskRoutine Start(Action actionToLoop) => New(actionToLoop, true);

        /// <summary>
        /// More readable way to immediately start a TaskRoutine. <br/>
        /// This will IMMEDIATELY execute all code up to the first yield within the IEnumerator function inside the TaskRoutine.
        /// <para/>Analogous to: <code>new TaskRoutine(yourIEnumeratorRoutine, true);</code> or
        /// <code>TaskRoutine.New(yourIEnumeratorRoutine, true);</code>
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static TaskRoutine Start(TaskRoutineEnumerableFunc func) => New(func, true);
        [Obsolete(IENUMERATOR_DEPRECATION_WARNING)]
        public static TaskRoutine Start(TaskRoutineFunc func) => New(func, true);

        public static TaskResult<T> Start<T>(TaskResultEnumerableFunc<T> func) => New<T>(func, true);
        [Obsolete(IENUMERATOR_DEPRECATION_WARNING)]
        public static TaskResult<T> Start<T>(TaskResultFunc<T> func) => New<T>(func, true);

        public static TaskResult<T> Start<T>(IEnumerable c, TaskResult<T> output) => New<T>(c, output, true);
        [Obsolete(IENUMERATOR_DEPRECATION_WARNING)]
        public static TaskResult<T> Start<T>(IEnumerator c, TaskResult<T> output) => New<T>(c, output, true);

        
        /// <summary>
        /// Waits for delayTime seconds then starts the taskRoutine
        /// </summary>
        /// <param name="delayTime"></param>
        /// <param name="delayedRoutineFunc"></param>
        /// <returns>The routine that will start after the delay</returns>
        public static TaskRoutine DelayedStart(float delayTime, TaskRoutineFunc delayedRoutineFunc)
        {
            return DelayedStart(delayTime, New(delayedRoutineFunc));
        }

        /// <summary>
        /// Waits for delayTime seconds then starts the taskRoutine
        /// </summary>
        /// <param name="delayTime"></param>
        /// <param name="delayedRoutine"></param>
        /// <returns>The routine that will start after the delay</returns>
        public static TaskRoutine DelayedStart(float delayTime, IEnumerable delayedRoutine)
        {
            return DelayedStart(delayTime, New(delayedRoutine));
        }
        [Obsolete(IENUMERATOR_DEPRECATION_WARNING)]
        public static TaskRoutine DelayedStart(float delayTime, IEnumerator delayedRoutine)
        {
            return DelayedStart(delayTime, New(delayedRoutine));
        }

        /// <summary>
        /// Waits for delayTime seconds then starts the taskRoutine
        /// </summary>
        /// <param name="delayTime"></param>
        /// <param name="delayedTask"></param>
        /// <returns>The routine that will start after the delay</returns>
        public static TaskRoutine DelayedStart(float delayTime, TaskRoutine delayedTask)
        {
            return TaskRoutine.Delay(delayTime).OnFinish(delayedTask);
        }

        //Added for consistency with New<T>(Func<TaskResult<T>, IEnumerable> func, bool autoStart)
        public static TaskRoutine New(IEnumerable c, bool autoStart = false) => new TaskRoutine(c, autoStart);
        [Obsolete(IENUMERATOR_DEPRECATION_WARNING)]
        public static TaskRoutine New(IEnumerator c, bool autoStart = false) => new TaskRoutine(c, autoStart);
        
        //Added for consistency with New<T>(Func<TaskResult<T>, IEnumerable> func, bool autoStart)
        public static TaskRoutine New(TaskRoutineEnumerableFunc func, bool autoStart = false) =>
            new TaskRoutine(func?.Invoke(), autoStart);
        [Obsolete(IENUMERATOR_DEPRECATION_WARNING)]
        public static TaskRoutine New(TaskRoutineFunc func, bool autoStart = false) => new TaskRoutine(func?.Invoke(), autoStart);
        
        public static TaskRoutine New(Action action, bool autoStart = false)
        {
            return New(WrapperFunc, autoStart);
            IEnumerable WrapperFunc()
            {
                action?.Invoke();
                yield break;
            }
        }

        public static TaskResult<T> New<T>(TaskResultEnumerableFunc<T> func, bool autoStart = false)
        {
            TaskResult<T> result = new(); //generate result object
            result.routine =
                new TaskRoutine(func?.Invoke(result), autoStart); //set up and connect taskroutine that uses it
            return result;
        }
        [Obsolete(IENUMERATOR_DEPRECATION_WARNING)]
        public static TaskResult<T> New<T>(TaskResultFunc<T> func, bool autoStart = false)
        {
            TaskResult<T> result = new(); //generate result object
            result.routine =
                new TaskRoutine(func?.Invoke(result), autoStart); //set up and connect taskroutine that uses it
            return result;
        }

        public static TaskResult<T> New<T>(IEnumerable c, TaskResult<T> output, bool autoStart = false)
        {
            if (output.routine != null)
            {
                //can't use a TaskResult<T> that's already "married" to another routine.
                throw new ArgumentException($"Cannot pass in \"married\" {nameof(TaskResult<T>)} {nameof(output)} " +
                    $"(one that already has already set an associated {nameof(TaskRoutine)}). \n" +
                    $"Unless you really need to pass in an existing {nameof(TaskResult<T>)}, it's recommended you " +
                    $"instead use the overload {nameof(TaskResult<T>)} New<{nameof(T)}>{nameof(Func<TaskResult<T>, IEnumerable>)} " +
                    $"func, bool autoStart) or the respective TaskRountine.Start() overload instead.");
            }

            //set up and connect taskroutine that uses it
            output.routine = new TaskRoutine(c, autoStart);
            return output;
        }
        [Obsolete(IENUMERATOR_DEPRECATION_WARNING)]
        public static TaskResult<T> New<T>(IEnumerator c, TaskResult<T> output, bool autoStart = false)
        {
            if (output.routine != null)
            {
                //can't use a TaskResult<T> that's already "married" to another routine.
                throw new ArgumentException($"Cannot pass in \"married\" {nameof(TaskResult<T>)} {nameof(output)} " +
                    $"(one that already has already set an associated {nameof(TaskRoutine)}). \n" +
                    $"Unless you really need to pass in an existing {nameof(TaskResult<T>)}, it's recommended you " +
                    $"instead use the overload {nameof(TaskResult<T>)} New<{nameof(T)}>{nameof(Func<TaskResult<T>, IEnumerator>)} " +
                    $"func, bool autoStart) or the respective TaskRountine.Start() overload instead.");
            }

            //set up and connect taskroutine that uses it
            output.routine = new TaskRoutine(c, autoStart);
            return output;
        }

        
        #endregion

        /// <summary>
        /// Starts the TaskRoutine encapsulated within routineCaller().
        /// <br/>
        /// Should the encapsulated routine be cancelled, execution of routineCaller() is re-attempted up to a maximum of "maxAttemps" times.
        /// If/when it succeeds/is resolved, the returned taskRoutine immediately resolves.
        /// <br/>
        /// If it still is cancelled/fails after "maxAttempt" attempts, this function returns a cancel code. Otherwise, it is resolved.
        /// </summary>
        /// <param name="routineCaller">This wrapper allows you to freely pass in arguments or whatever you need to do. So long as it returns a TaskRoutine representing the task result.</param>
        /// <param name="maxAttempts">The maximum number of times routineCaller() will be invoked after failing. if the number of attempts made meets or exceeds this number, the whole Try routine is cancelled.</param>
        /// <param name="onAttemptFailureCallback">Invoked each time the operation fails. Good place for debug logging or some other reaction/feedback.</param>
        /// <param name="emergencyStopCondition">If this ever returns true, any remaining attempts are ignored and the operation is 
        /// canceled that frame. Useful for breaking ties with a lost cause.</param>
        /// <returns></returns>
        public static TaskRoutine Retry(
            Func<TaskRoutine> routineCaller, 
            int maxAttempts, 
            Action<int> onAttemptFailureCallback=null, 
            Func<bool> emergencyStopCondition = null)
        {
            return Start(TryRoutine_Internal(routineCaller, maxAttempts, onAttemptFailureCallback, emergencyStopCondition));
        }
        
        protected static IEnumerable TryRoutine_Internal(
            Func<TaskRoutine> routineCaller, 
            int maxAttempts, 
            Action<int> onAttemptFailureCallback = null, 
            Func<bool> earlyStopCondition = null)
        {
            TaskRoutine latestRoutine = null;
            bool successfullyCompleted = false;
            
            for (int remAttempts = maxAttempts; remAttempts > 0 && earlyStopCondition?.Invoke() != false; remAttempts--)
            {
                latestRoutine = routineCaller();
                
                yield return new WaitUntil(() => !latestRoutine.HasNeverBeenStarted && !latestRoutine.IsRunning);
                
                if (latestRoutine.IsResolved)
                {
                    //operation complete, break from the loop
                    successfullyCompleted = true;
                    break;
                }
                else
                {
                    onAttemptFailureCallback?.Invoke(remAttempts);
                }
            }

            //if still not resolved by this point, operation has failed.
            if (!successfullyCompleted && (latestRoutine == null || !latestRoutine.IsResolved))
            {
                yield return new CancelTaskRoutine();
            }
        }

        /// <summary>
        /// Runs relevant checks and attempts to fire Cancel() on the given routine. Can also send a message to the Unity console
        /// </summary>
        /// <param name="routineRef"></param>
        /// <param name="debugMessage"></param>
        /// <returns></returns>
        public static bool TryCancel(TaskRoutine routineRef, string debugMessage = "")
        {
            if (routineRef is not { IsRunning: true }) return false;
            
            if(debugMessage.Length > 0)
            {
                Debug.LogWarning("[TaskRoutine.TryCancel()] " + debugMessage);
            }

            routineRef.Cancel();
            return true;

        }

        /// Begins execution of the coroutine
        public TaskRoutine Start()
        {
            innerTaskState.Start();
            return this;
        }

        public void Restart()
        {
            if (innerTaskState.enumerableRoutine == null)
            {
                Debug.LogError("[TaskRoutine] A TaskRoutine need to be defined using an IEnumerable " +
                    "(as opposed to an IEnumerator) in order to repeatedly execute (like what Restart() does).");
                return;
            }
            
            // innerTaskState.Cancel();
            // innerTaskState.Start();
            innerTaskState.Restart();
        }

        /// <summary> Prematurely finishes execution of the coroutine at its next yield WITHOUT counting as a cancellation.</summary>
        public virtual TaskRoutine Resolve()
        {
            innerTaskState.Resolve();
            return this;
        }

        /// <summary> Prematurely discontinues execution of the coroutine at its next yield as a cancellation.</summary>
        public virtual TaskRoutine Cancel()
        {
            innerTaskState.Cancel(); 
            return this;
        }

        /// <summary>
        /// Cancels the routine and then unhooks interior references so that all the pieces can be GC'd
        /// Be sure to null out or otherwise disconnect the reference to this instance as well.
        /// A 
        /// </summary>
        public void Destroy()
        {
            Cancel();
            innerTaskState = null;
            Destroyed = true;
        }

        public void Pause() => innerTaskState.Pause();

        public void Unpause() => innerTaskState.Unpause();

        /// <summary>
        /// Runs the given action (not a routine) after the taskroutine finishes. Returns the taskRoutine that called this function.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="stillRunOnCancel"></param>
        /// <returns></returns>
        public TaskRoutine OnFinish(FinishedHandlerWithCancelCheck callback, bool stillRunOnCancel = false)
        {
            if(IsFinished(stillRunOnCancel))
            {
                callback?.Invoke(IsCancelled);
            }
            else
            {
                FinishedEvent += OnceCallback;

                void OnceCallback(bool wasCancelled)
                {
                    if(stillRunOnCancel || !wasCancelled)
                    {
                        callback?.Invoke(wasCancelled);
                    }
                    FinishedEvent -= OnceCallback;
                }
            }

            return this;
        }

        /// <summary>
        /// Runs the given action (not a routine) after the taskroutine finishes. Returns the taskRoutine that called this function. <br/>
        /// This version omits the wasCancelled boolean so that you don't always have to remember to include it
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="stillRunOnCancel"></param>
        /// <returns></returns>
        public TaskRoutine OnFinish(Action callback, bool stillRunOnCancel = false)
        {
            if (IsFinished(stillRunOnCancel))
            {
                callback?.Invoke();
            }
            else
            {
                FinishedEvent += OnceCallback;

                void OnceCallback(bool wasCancelled)
                {
                    if (stillRunOnCancel || !wasCancelled)
                    {
                        callback?.Invoke();
                    }
                    FinishedEvent -= OnceCallback;
                }
            }

            return this;
        }

        public bool IsFinished(bool stillCountIfCancelled=false) => IsResolved || (stillCountIfCancelled && IsCancelled);

        /// <summary>
        /// Use to chain <see cref="TaskRoutine"/>s together!
        /// </summary>
        /// <param name="nextRoutine"></param>
        /// <param name="stillRunOnCancel"></param>
        /// <returns>nextRoutine, the same <see cref="TaskRoutine"/> you inputted, for convenience</returns>
        /// <remarks>
        /// Note: while multiple OnFinish() calls made from the same TaskRoutine will technically
        /// execute in order, they will still be treated concurrently
        ///<code>
        /// var a = TaskRoutine.New(SomeProcess);
        /// a.OnFinish(b);
        /// a.OnFinish(c);
        /// b.OnFinish(d);
        /// c.OnFinish(e);
        /// </code>
        /// Will execute as a Starts -> b Starts -> c Starts
        /// With routines b and c ultimately executing as concurrent TaskRoutine chains.
        /// </remarks>
        public TaskRoutine OnFinish(TaskRoutine nextRoutine, bool stillRunOnCancel = false)
        {
            //rifle forward until you find the front of the given nextRoutine's existing thread, 
            //if it has one, so that you do that thread next instead of just part of it
            while(nextRoutine.Previous != null)
            {
                nextRoutine = nextRoutine.Previous;
            }

            nextRoutine.Previous = this;
            this.Next = nextRoutine;
            //TODO: Next doesn't handle branches well:
            //a.OnFinish(b)
            //b.OnFinish(c)
            //b.OnFinish(d)
            //seems fine at first - c and d will execute after b, in order, but b's flags now only think d comes after
            //query - do we use or need Next? just Previous is enough to handle causality

            //another note - d is NOT inserted between b and c

            this.OnFinish(wasCancelled =>
            {
                if(nextRoutine != null)
                {
                    if (!nextRoutine.HasNeverBeenStarted)
                    {
                        Debug.LogError("[TaskRoutine.OnFinish] The next routine has been started prematurely! " +
                             "routines given to OnFinish() should not be prematurely started.\n " +
                             "Otherwise they won't actually trigger when the first one finishes, which will be very confusing " +
                             "and almost certainly not what you intended!");
                    }
                    nextRoutine.Start();
                }
                else
                {
                    Debug.LogError("[TaskRoutine] Broken TaskRoutine Chain Error! The routine that was chained after "
                        + $"this one ({this}) now no longer exists. Failed routines should be flagged as " 
                        + "Cancelled, not Destroyed/Disposed.");
                }
            }, stillRunOnCancel);

            return nextRoutine;
        }

        /// <summary>
        /// Use to chain <see cref="TaskResult{T}"/>s together!
        /// </summary>
        /// <param name="nextRoutineWithResult"></param>
        /// <param name="stillRunOnCancel"></param>
        /// <returns>The same <see cref="TaskResult{T}"/> you inputted, for convenience</returns>
        public TaskResult<T> OnFinish<T>(TaskResult<T> nextRoutineWithResult, bool stillRunOnCancel = false)
        {
            nextRoutineWithResult.OnFinish(nextRoutineWithResult, stillRunOnCancel);
            return nextRoutineWithResult;
        }

        /// <summary>
        /// Use to chain <see cref="TaskRoutine"/>s together with less boilerplate.
        /// </summary>
        /// <param name="nextRoutine"></param>
        /// <param name="stillRunOnCancel"></param>
        /// <returns>The <see cref="TaskRoutine"/> wrapping the given function, for convenience</returns>
        public TaskRoutine OnFinish(TaskRoutineEnumerableFunc routineFunc, bool stillRunOnCancel = false)
        {
            return OnFinish(New(routineFunc, false));
        }

        /// <summary>
        /// Use to chain a function that returns a <see cref="TaskRoutine"/> but can't immediately execute (a mix of synchronous and async instructions).
        /// </summary>
        /// <param name="routineFunc">Function to wrap in a proxy and execute within. Returns a <see cref="TaskRoutine"/> that will be embedded into the proxy that is immediately returned.</param>
        /// <param name="stillRunOnCancel"></param>
        /// <returns>A proxy <see cref="TaskRoutine"/> that will resolve or cancel when the <see cref="TaskRoutine"/> returned by the latent function does.</returns>
        /// <remarks>
        /// Experimental, but it PROBABLY works.
        /// <br/>
        /// Use at your own risk.
        /// <br/>
        /// It may be safer to just wrap your logic ahead of time.
        /// </remarks>
        [Obsolete("Experimental! Let me know if it works or if you have issues.")]
        public TaskRoutine OnFinish(LatentTaskRoutineFunc routineFunc, bool stillRunOnCancel = false)
        {
            if (routineFunc == null) return this; //nothing to do then, return self and keep chaining.

            //Boilerplate required; the original proxy needs be referenced within the coroutine.
            TaskRoutine proxy = null;
            proxy = New(Local_ProxyRoutine());
            // Debug.Log("[TaskRoutine.OnFinish(latent)] - fooble - about to return proxy taskroutine");
            return OnFinish(proxy, stillRunOnCancel);

            IEnumerable Local_ProxyRoutine()
            {
                // Debug.Log("[TaskRoutine.OnFinish(latent)] - fooble - started proxy routine");
                //this guy only resolves once actual routine becomes both non-null and resolved
                //this guy only cancels once actual routine becomes both non-null and resolved

                //new problem - need cancellations to penetrate

                //run the latent logic, which will do multiple things before returning a final
                //task routine representing the end of its chain.
                TaskRoutine actualRoutine = routineFunc.Invoke();
                // Debug.Log("[TaskRoutine.OnFinish(latent)] - fooble - invoked latent function, receiving routine...");
                if (actualRoutine == null)
                {
                    // Debug.Log("[TaskRoutine.OnFinish(latent)] - fooble - latent func did not return a routine, resolving immediately");
                    yield return new ResolveTaskRoutine();
                    yield break;
                }
                // Debug.Log("[TaskRoutine.OnFinish(latent)] - fooble - received valid taskroutine from latent func");

                //start if not already started, for consistent behaviour.
                if (actualRoutine.HasNeverBeenStarted)
                {
                    actualRoutine.Start();
                    // Debug.Log("[TaskRoutine.OnFinish(latent)] - fooble - started real routine since it hasn't been started");
                }
                // else
                // {
                //     Debug.Log("[TaskRoutine.OnFinish(latent)] - fooble - the taskroutine from the latent func has already started");
                // }

                //your IDE may be concerned about proxy being null - if this function was called
                //before the proxy was assigned, it would be, but it isn't in this case.
                //that's why we used New() instead of Start().

                //embed the latent taskroutine into the proxy.
                //This updates the inner state and coroutine to the new latent one,
                //aggregates on-finished logic, and preserves the original
                //proxy's reference so that external linkages are unaffected
                //it also allows the proxy's original inner state and routine to be GC'd
                proxy.EmbedState(actualRoutine);

                // Debug.Log("[TaskRoutine.OnFinish(latent)] - fooble - embedded true routine into the proxy");
                //give it a cycle to allow the transfer to happen.
                //need to ensure it executes the embedding in time.
                yield return null;

                // Debug.Log("[TaskRoutine.OnFinish(latent)] - fooble - completing proxy routine");
            }

            //Register B -> Return B' -> Run A -> OnFinishedEvent Invoke -> Run B
            //B': IEnumerator: Await proxy val != null -> Await proxy val != running -> handle resolve/canceled

        }

        [Obsolete(IENUMERATOR_DEPRECATION_WARNING)]
        public TaskRoutine OnFinish(TaskRoutineFunc routineFunc, bool stillRunOnCancel = false)
        {
            return OnFinish(New(routineFunc, false));
        }

        /// <summary>
        /// Use to chain <see cref="TaskResult{T}"/>s together!
        /// </summary>
        /// <param name="func"></param>
        /// <param name="stillRunOnCancel"></param>
        /// <returns>The same <see cref="TaskResult{T}"/> you inputted, for convenience</returns>
        public TaskResult<T> OnFinish<T>(TaskResultEnumerableFunc<T> func, bool stillRunOnCancel = false)
        {
            return OnFinish(New(func), stillRunOnCancel);
        }
        [Obsolete(IENUMERATOR_DEPRECATION_WARNING)]
        public TaskResult<T> OnFinish<T>(TaskResultFunc<T> func, bool stillRunOnCancel = false)
        {
            return OnFinish(New(func), stillRunOnCancel);
        }

        public static TaskRoutine OnAllFinish(TaskRoutine nextRoutine, bool stillRunOnAnyCancel, params TaskRoutine[] routines)
        {
            return OnAllFinish(routines, nextRoutine, stillRunOnAnyCancel);
        }
        public static TaskRoutine OnAllFinish(Action nextAction, bool stillRunOnAnyCancel, params TaskRoutine[] routines)
        {
            return OnAllFinish(routines, New(nextAction), stillRunOnAnyCancel);
        }

        public static TaskRoutine OnAllFinish(IEnumerable<TaskRoutine> routines, Action nextAction, bool stillRunOnAnyCancel = false)
        {
            return OnAllFinish(routines, New(nextAction), stillRunOnAnyCancel);
        }

        /// <summary>
        /// Will attempt to run nextRoutine once all given routines have completed.
        /// /// <br/>
        /// This does NOT automatically start any of the routines.
        /// </summary>
        /// <param name="routines"></param>
        /// <param name="nextRoutine"></param>
        /// <param name="stillRunOnAnyCancel">If true, then <see cref="nextRoutine"/> will run once all given routines have either resolved or cancelled. If false, the entire process will be aborted if any of the given routines are cancelled.</param>
        /// <returns></returns>
        public static TaskRoutine OnAllFinish(IEnumerable<TaskRoutine> routines, TaskRoutine nextRoutine, bool stillRunOnAnyCancel = false)
        {
            int completedTasks = 0;

            if (!stillRunOnAnyCancel)
            {
                //first pass quickly checks to see if this whole operation is even valid
                foreach (TaskRoutine routine in routines)
                {
                    //aborts whole process to avoid massive pointless overhead and short-circuits.
                    //all these "on finish" functions return the given nextRoutine out of principle.
                    //Retaining for consistency.
                    if (routine.IsCancelled) return nextRoutine;
                }
            }

            //second pass performs the operation
            foreach (TaskRoutine routine in routines) 
            {
                if (routine == null)
                {
                    AttemptInvocation(false); //no routine to wait for, just execute immediately and be done with it.
                    continue;
                }

                routine.OnFinish(AttemptInvocation, stillRunOnAnyCancel);
            }

            return nextRoutine;

            void AttemptInvocation(bool wasCancelled)
            {
                completedTasks++;
                if (nextRoutine.HasNeverBeenStarted && completedTasks >= routines.Count())
                {
                    nextRoutine.Start();

                    //TODO: could then try and unsub all the other straggling callbacks,
                    //but that might be overengineering things at the current moment
                    //(and require a third loop iteration)
                }
            }
        }

        //TODO: Consolidate the functionality shared by these overloads to avoid another mix-up
        /// <summary>
        /// Will attempt to run nextRoutine once all given routines have completed.
        /// <br/>
        /// This does NOT automatically start any of the routines.
        /// </summary>
        /// <param name="routines"></param>
        /// <param name="nextRoutine"></param>
        /// <param name="stillRunOnAnyCancel">If true, then <see cref="nextRoutine"/> will run once all given routines have either resolved or cancelled. If false, the entire process will be aborted if any of the given routines are cancelled.</param>
        /// <typeparam name="T">Used to clarify that the output is GUARANTEED to be the same given type as the input for this method (which must fulfill <see cref="IEnumerable{TaskRoutine}"/>. The alternative is just an unspecified IEnumerable. </typeparam>
        /// <returns></returns>
        public static T OnAllFinish<T>(T routines, FinishedHandlerWithCancelCheck callback, bool stillRunOnAnyCancel = false) where T : IEnumerable<TaskRoutine>
        {
            int completedTasks = 0;
            bool callbackFired = false;
            bool anyWasCancelled = false;

            if (!stillRunOnAnyCancel)
            {
                //first pass quickly checks to see if this whole operation is even valid
                foreach (TaskRoutine routine in routines)
                {
                    if (routine == null) continue;

                    if (routine.IsCancelled && !stillRunOnAnyCancel)
                    {
                        //aborts whole process to avoid massive pointless overhead and short-circuits.
                        //all these "on finish" functions return the given nextRoutine out of principle.
                        //Retaining for consistency.
                        return routines;
                    }
                }
            }

            //second pass performs the operation
            foreach (TaskRoutine routine in routines)
            {
                if (routine == null)
                {
                    AttemptInvocation(false); //no routine to wait for, just execute immediately and be done with it.
                    continue;
                }

                routine.OnFinish(AttemptInvocation, stillRunOnAnyCancel);
            }
            return routines;

            void AttemptInvocation(bool wasCancelled)
            {
                completedTasks++;
                anyWasCancelled |= wasCancelled;

                if (!callbackFired && completedTasks >= routines.Count())
                {
                    callbackFired = true;
                    callback?.Invoke(anyWasCancelled);
                }
            }
        }

        public TaskRoutine OnFinishDelay(float delayTime, TaskRoutine nextRoutine, bool stillRunOnCancel = false)
        {
            return OnFinish(Delay(delayTime), stillRunOnCancel).OnFinish(nextRoutine);
        }

        public TaskRoutine OnFinishDelay(float delayTime, Action delayedAction, bool stillRunOnCancel = false)
        {
            return OnFinish(Delay(delayTime, delayedAction), stillRunOnCancel);
        }

        /// <summary>
        /// Defers/delays the given callback to execute sometime next frame. 
        /// <para/>Timing within next frame NOT guaranteed!
        /// </summary>
        /// <param name="defferedAction"></param>
        public static TaskRoutine Defer(Action defferedAction)
        {
            return TaskRoutine.Wait(() => null, defferedAction);
        }

        /// <summary>
        /// Waits for delayTime seconds then optionally runs a callback
        /// </summary>
        /// <param name="delayTime"></param>
        /// <param name="delayedAction"></param>
        /// <returns>The delay routine itself</returns>
        public static TaskRoutine Delay(float delayTime, Action delayedAction=null, bool realtime = false)
        {
            return TaskRoutine.Wait(
                () => realtime ?
                    new WaitForSecondsRealtime(delayTime) :
                    new WaitForSeconds(delayTime), 
                delayedAction);
        }

        /// <summary>
        /// Waits for delayTime seconds after the current task completes
        /// </summary>
        /// <param name="delayTime"></param>
        /// <param name="realtime"></param>
        /// <returns></returns>
        public TaskRoutine ThenDelay(float delayTime, bool realtime=false)
        {
            return OnFinish(TaskRoutine.Yield(
                () => realtime ? 
                    new WaitForSecondsRealtime(delayTime) : 
                    new WaitForSeconds(delayTime),
                autoStart:false));
        }

        /// <summary>
        /// Returns a taskroutine that waits until the event fires before resolving (and optionally executing a callback)
        /// </summary>
        /// <param name="eventTrigger">an event that we are waiting on.</param>
        /// <param name="yieldedAction">optional action to perform once query == true</param>
        /// <returns>A TaskRoutine that resolves once query == true and yieldedAction has been executed (if not null)</returns>
        public static TaskRoutine AwaitEvent(UnityEvent eventTrigger, Action yieldedAction=null)
        {
            if(eventTrigger == null) throw new ArgumentNullException("WaitUntil eventTrigger cannot be null");

            Debug.Log("[TaskRoutine.AwaitEvent()] - fooble - entered");

            bool eventFired = false;
            eventTrigger.AddListener(Local_WaitOnceForEvent);
            void Local_WaitOnceForEvent()
            {
                Debug.Log("[TaskRoutine.AwaitEvent()] - fooble - registered event fired");
                eventFired = true;
                eventTrigger?.RemoveListener(Local_WaitOnceForEvent);
            }

            //not excellent, but just about as performant as WaitUntil() in general
            return TaskRoutine.Wait(() => new WaitUntil(() => eventFired), yieldedAction);
        }

        /// <summary>
        /// Returns a taskroutine that waits until the event fires before resolving (and optionally executing a callback)
        /// </summary>
        /// <param name="eventTrigger">an event that we are waiting on.</param>
        /// <param name="yieldedAction">optional action to perform once query == true</param>
        /// <returns>A TaskRoutine that resolves once query == true and yieldedAction has been executed (if not null)</returns>
        public static TaskRoutine AwaitEvent(Action eventTrigger, Action yieldedAction=null)
        {
            if(eventTrigger == null) throw new ArgumentNullException("WaitUntil eventTrigger cannot be null");

            bool eventFired = false;
            eventTrigger += Local_WaitOnceForEvent;
            void Local_WaitOnceForEvent()
            {
                eventFired = true;
                eventTrigger -= Local_WaitOnceForEvent;
            }

            //not excellent, but just about as performant as WaitUntil() in general
            return TaskRoutine.Wait(() => new WaitUntil(() => eventFired), yieldedAction);
        }

        /// <summary>
        /// Returns a taskroutine that waits until the query == true before resolving (and optionally executing a callback)
        /// </summary>
        /// <param name="query">a function that returns a boolean (presumably after repeatedly evaluating an expression</param>
        /// <param name="yieldedAction">optional action to perform once query == true</param>
        /// <returns>A TaskRoutine that resolves once query == true and yieldedAction has been executed (if not null)</returns>
        public static TaskRoutine WaitUntil(Func<bool> query, Action yieldedAction=null)
        {
            if(query == null) throw new ArgumentNullException("WaitUntil query predicate cannot be null");
            if (query.Invoke())
            {
                //condition already met, return a taskroutine that will
                //execute it without an awkward frame delay and immediately resolve.
                return TaskRoutine.New(yieldedAction);
            }
            return TaskRoutine.Wait(() => new WaitUntil(query), yieldedAction);
        }

        /// <summary>
        /// Returns a taskroutine that waits while the query == true and resolves once the query == false (and optionally executing a callback)
        /// </summary>
        /// <param name="query">a function that returns a boolean (presumably after repeatedly evaluating an expression</param>
        /// <param name="yieldedAction">optional action to perform once query == false</param>
        /// <returns>A TaskRoutine that waits while query == true and resolves once query ==false and yieldedAction has been executed (if not null)</returns>
        public static TaskRoutine WaitWhile(Func<bool> query, Action yieldedAction=null)
        {
            if(query == null) throw new ArgumentNullException("WaitWhile query predicate cannot be null");
            if (!query.Invoke())
            {
                //condition already met, return a taskroutine that will
                //execute it without an awkward frame delay and immediately resolve.
                return TaskRoutine.New(yieldedAction);
            }
            return TaskRoutine.Wait(() => new WaitWhile(query), yieldedAction);
        }

        /// <summary>
        /// Internal container for logic shared by static Wait-based TaskRoutine functions that use various YieldInstructions.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="yieldInstructionFactory"></param>
        /// <param name="yieldedAction"></param>
        /// <returns></returns>
        public static TaskRoutine Yield(Func<object> yieldInstructionFactory, bool autoStart)
        {
            return New(_WaitForGameObjectInvokeRoutine(), autoStart);
            IEnumerable _WaitForGameObjectInvokeRoutine()
            {
                yield return yieldInstructionFactory();
            }
        }

        public static TaskRoutine Yield(object yieldInstruction, bool autoStart)
        {
            return Yield(() => yieldInstruction, autoStart);
        }
        
        /// <summary>
        /// Internal container for logic shared by static Wait-based TaskRoutine functions that use various YieldInstructions.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="yieldInstructionFactory"></param>
        /// <param name="yieldedAction"></param>
        /// <returns></returns>
        protected static TaskRoutine Wait(Func<object> yieldInstructionFactory, Action yieldedAction)
        {
            return TaskRoutine.Start(_WaitForGameObjectInvokeRoutine());

            IEnumerable _WaitForGameObjectInvokeRoutine()
            {
                yield return yieldInstructionFactory();
                yieldedAction?.Invoke();
            }
        }

        /// <summary>
        /// Returns a yield-able object that will yield the routine it is called in until the given routine 
        /// finishes or is destroyed.
        /// When used inside a routine, this functions similarly to an await call on an async task.
        /// </summary>
        /// <param name="stillCountIfCancelled"></param>
        /// <returns></returns>
        public WaitUntil GetOnFinishedYield(bool stillCountIfCancelled)
        {
            return new WaitUntil(() => this == null || IsFinished(stillCountIfCancelled));
        }

        public static WaitUntil GetOnAllFinishedYield(bool stillCountIfAnyCancel, params TaskRoutine[] tasks) => GetOnAllFinishedYield(tasks, stillCountIfAnyCancel);

        public static WaitUntil GetOnAllFinishedYield(IEnumerable<TaskRoutine> tasks, bool stillCountIfAnyCancel)
        {
            return new WaitUntil(() => tasks.All(tr => tr == null || tr.IsFinished(stillCountIfAnyCancel)));
        }

        /// <summary>
        /// Returns a WaitUntil YieldInstruction that will "await" the completion of this TaskRoutine.
        /// </summary>
        /// <returns></returns>
        public WaitUntil Await() => GetOnFinishedYield(true);

        public static WaitUntil AwaitAll(IEnumerable<TaskRoutine> tasks) => GetOnAllFinishedYield(tasks, true);
        public static WaitUntil AwaitAll(params TaskRoutine[] tasks) => GetOnAllFinishedYield(tasks, true);

        public static IEnumerable<TaskRoutine> StartAll(IEnumerable<TaskRoutine> routines)
        {
            foreach(TaskRoutine task in routines)
                task.Start();

            return routines;
        }
        public static IEnumerable<TaskRoutine> StartAll(params TaskRoutine[] routines) => StartAll(routines);

        /// <summary>
        /// Replaces this TaskRoutine's state object with another's, and aggregates their callbacks.
        /// Generally used internally to handle latent routines.
        /// </summary>
        /// <param name="routine"></param>
        public void EmbedState(TaskRoutine actualRoutine)
        {
            var incomingInnerState = actualRoutine.innerTaskState;

            //can't externally wipe the event's subscriptions
            //using this to ensure the original proxy routine's
            //temporary resolver logic doesn't awkwardly trigger somehow
            innerTaskState.ClearOnFinishedCallbacks();

            //ensure the incoming routine's state completion additionally triggers any
            //additional callbacks that were already added to the proxy instance.
            //Result - on incoming's completion,
            //both proxy's outer callbacks and incoming's outer callbacks will all fire.
            //Null check in case it was destroyed
            if(incomingInnerState != null)
                incomingInnerState.FinishedEvent += (wasCan) => this.FinishedEvent?.Invoke(wasCan);

            //if your new one was already destroyed, inherit that state
            this.Destroyed |= actualRoutine.Destroyed;
            //actual progress will be from the incoming, empty proxies cannot meaningfully progress.
            this.Progress = actualRoutine.Progress;

            //back trace to start of new chain,
            TaskRoutine previousFromIncoming = actualRoutine;
            while (previousFromIncoming.Previous != null)
            {
                previousFromIncoming = previousFromIncoming.Previous;
            }
            TaskRoutine startOfIncomingChain = previousFromIncoming;

            //set that start's previous to be proxy's previous,
            //connecting the latent incoming chain to the proxy's existing chain.
            startOfIncomingChain.Previous = this.Previous;

            //Similarly, pick up from where the end of the latent incoming chain left off.
            //Note: we are assigning the previous values instead of this instance because
            //the proxy is being supplanted, not appended.
            //This is being done because we cannot easily/safely all the TaskRoutines
            //that have set the proxy as their Previous, so we need to ensure that reference is maintained
            //and stinky unsafe pointer manipulation doesn't quite work with managed objects.
            this.Previous = actualRoutine.Previous;

            //we don't have to worry about the other end -
            //that's why we're embedding the new INTO the proxy instead of the other way around

            //we keep the proxy wrapper's next - this ref is the end of its chain, so wrapper's next
            //it's also deprecated anyway

            //these inner task states are only referenced inside this class,
            //so the old ref should get GC'd after this reassignment
            innerTaskState = incomingInnerState;
        }
    }

    // /// <summary>
    // /// Wraps another (often latent) taskroutine. This class enables the same interface to penetrate into the latent state.
    // /// </summary>
    // public class TaskRoutineProxy : TaskRoutine
    // {
    //     //routine wrapper
    //
    //     TaskRoutine wrappedRoutine;
    //
    //     public void Resolve(TaskRoutine routine)
    //     {
    //         innerTaskState = routine.innerTaskState;
    //     }
    // }
}
