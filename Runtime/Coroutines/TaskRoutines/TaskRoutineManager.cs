// Originally Created By: "chomp" (https://forum.unity.com/members/chomp.29811/)
// Found At: https://forum.unity.com/threads/a-more-flexible-coroutine-interface.94220/
// Edited and Updated by: Julian Noel (https://github.com/vertigomaster)

/* Begin Legacy Description */

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
/// Note that to facilitate this API's behavior, a "TaskRoutine" GameObject is
/// created lazily on first use of the Task API and placed in the scene root
/// with the internal TaskManager component attached. All coroutine dispatch
/// for Tasks is done through this component.

/* End Legacy Description */

using System.Collections;
using JetBrains.Annotations;
using UnityEngine;

// using IDEK.Tools.Logging;

namespace IDEK.Tools.Coroutines.TaskRoutines
{
    class TaskRoutineManager : MonoBehaviour
    {
        /// <summary>
        /// Internal class doing the bulk of the work
        /// </summary>
        internal class TaskState
        {
            public bool Running { get; private set; }
            public bool Paused { get; private set; }
            public bool Resolved { get; private set; }
            public bool Cancelled { get; private set; }
            public bool HasNeverBeenStarted { get; private set; } = true;

            private Coroutine activeCallWrapper;

            public delegate void FinishedHandler(bool wasCancelled);
            public event FinishedHandler FinishedEvent;

            [CanBeNull]
            public IEnumerable enumerableRoutine;
            public IEnumerator coroutine;

            public TaskState(IEnumerator c)
            {
                coroutine = c;
            }

            public TaskState(IEnumerable c)
            {
                //these are non-generic IEnumerables that do not implement IDisposable
                // ReSharper disable once GenericEnumeratorNotDisposed
                enumerableRoutine = c;
                coroutine = c.GetEnumerator();
            }

            public void Pause() => Paused = true;

            public void Unpause() => Paused = false;

            public void Start()
            {
                Running = true;
                HasNeverBeenStarted = false;
                activeCallWrapper = singleton.StartCoroutine(CallWrapper());
            }

            public void Resolve()
            {
                Resolved = true;
                Running = false;
                HasNeverBeenStarted = false;
            }

            public void Restart()
            {
                singleton.StopCoroutine(activeCallWrapper);

                Cancelled = false;
                Resolved = false;
                Paused = false;

                Start();
            }

            public void Cancel()
            {
                Cancelled = true;
                Running = false;
                HasNeverBeenStarted = false;
            }
            
            private IEnumerator CallWrapper()
            {
                yield return null;
                // ReSharper disable once GenericEnumeratorNotDisposed
                IEnumerator e = enumerableRoutine != null ? enumerableRoutine.GetEnumerator() : coroutine;
                while(Running)
                {
                    if(Paused)
                    {
                        yield return null;
                    }
                    else
                    {
                        if(e == null || !e.MoveNext() || e.Current is ResolveTaskRoutine)
                        {
                            Resolve();
                        }
                        else if(e.Current is CancelTaskRoutine)
                        {
                            Cancel();
                        }
                        else if (e.Current is TaskRoutine yieldedTaskRoutine) //TODO: add some way to track who/where the taskroutine was built from
                        {
                            Debug.LogError("[TaskRoutine] Do not directly yield return TaskRoutines! It is too dangerous. Use .Await() " +
                                "on the routine beforehand to return a reliable yield instruction for it instead.");
                            ////TODO: wait for yielded taskroutine to complete ?
                            yield return e.Current;
                        }
                        else
                        {
                            yield return e.Current;
                        }
                    }
                }
                
                FinishedEvent?.Invoke(Cancelled);
            }

            /// <summary>
            /// Shuts down any existing connections to outer task routines.
            /// Generally used when embedding a newly produced routine into a declared proxy.
            /// </summary>
            public void ClearOnFinishedCallbacks()
            {
                FinishedEvent = null;
            }
        }

        static TaskRoutineManager singleton;

        public static TaskState CreateTask(IEnumerator coroutine)
        {
            if(singleton == null)
            {
                GameObject go = new("[TaskRoutineManager]");
                singleton = go.AddComponent<TaskRoutineManager>();
                DontDestroyOnLoad(go);
            }
            return new TaskState(coroutine);
        }

        public static TaskState CreateTask(IEnumerable coroutine)
        {
            if (singleton == null)
            {
                GameObject go = new("[TaskRoutineManager]");
                singleton = go.AddComponent<TaskRoutineManager>();
                DontDestroyOnLoad(go);
            }

            return new TaskState(coroutine);
        }
    }

    public abstract class TaskRoutineInstruction : YieldInstruction { }

    /// <summary>
    /// Enables a task routine to explicitly cancel itself 
    /// during execution using <c>yield return new <see cref="CancelTaskRoutine"/>()</c>.
    /// </summary>
    /// <remarks>
    /// Avoids having to jump through awkward hoops to try and pass the <see cref="TaskRoutine"/> reference into the callback
    /// </remarks>
    public class CancelTaskRoutine : TaskRoutineInstruction {
        /// <summary>
        /// Explicitly cancels this <see cref="TaskRoutine"/>.
        /// </summary>
        /// /// <remarks>
        /// Avoids having to jump through awkward hoops to try and 
        /// pass the <see cref="TaskRoutine"/> reference into the callback
        /// </remarks>
        public CancelTaskRoutine() { }
    }

    /// <summary>
    /// Enables a task routine to explicitly complete early with a success code
    /// during execution using <c>yield return new <see cref="ResolveTaskRoutine"/>();</c>.
    /// Analogous to the <c>break</c> keyword in a loop.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="CancelTaskRoutine"/>, this does not trigger the <c>wasCancelled</c> flag 
    /// and counts as a successful resolution of the <see cref="TaskRoutine"/>.
    /// <br/>
    /// Also avoids having to jump through awkward hoops to try and pass the <see cref="TaskRoutine"/> reference into the callback
    /// </remarks>
    public class ResolveTaskRoutine : TaskRoutineInstruction
    {
        /// <summary>
        /// Explicitly breaks out of this TaskRoutine early. 
        /// Analogous to the <c>break</c> keyword in a loop.
        /// </summary>
        /// /// <remarks>
        /// Avoids having to jump through awkward hoops to try and 
        /// pass the <see cref="TaskRoutine"/> reference into the callback
        /// </remarks>
        public ResolveTaskRoutine() { }
    }

    //TODO: add an TaskRoutineError instruction?
}
