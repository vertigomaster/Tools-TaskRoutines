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
using static IDEK.Tools.Coroutines.TaskRoutines.TaskRoutine;
namespace IDEK.Tools.Coroutines.TaskRoutines
{
    public class TaskResult
    {
        public TaskRoutine routine;
    }

    /// <summary>
    /// A wrapper for <see cref="TaskRoutine"/> that enables embedded return values, 
    /// as well as the ability to immediately return them.
    /// </summary>
    /// <remarks>
    /// The latter isn't doable with regular <see cref="TaskRoutine"/>s, even when using 
    /// <see cref="CancelTaskRoutine"/>, as <see cref="TaskRoutine"/>, being Coroutines under
    /// the hood, inherently take at least 1 frame to execute and trigger its finish event. 
    /// Using <see cref="TaskRoutine"/> as a function return value also requires that function 
    /// to manually return a new <see cref="TaskRoutine"/> object, which can be annoying 
    /// and confusing to look at on code paths that don't immediately chain into a nested 
    /// <see cref="TaskRoutine"/>. <see cref="TaskResult{TResultType}"/> can implicitly cast its 
    /// embeded <see cref="TResultType"/> to a <see cref="TaskResult{TResultType}"/>, enabling 
    /// you to return the original value like normal.
    /// </remarks>
    /// <typeparam name="TResultType"></typeparam>
    public class TaskResult<TResultType> : TaskResult
    {
        private const string CANT_GET_UNDEFINED_VALUE = "Cannot retrieve undefined value from TaskResult. Ensure sResolved == true or that the value has been otherwise set before trying to access it.";
        private TResultType _value;
        public bool ValueIsDefined { get; protected set; } = false;
        public TResultType Value
        {
            get
            {
                return ValueIsDefined ? _value : throw new InvalidOperationException(CANT_GET_UNDEFINED_VALUE);
            }
            set
            {
                _value = value;
                ValueIsDefined = true;
            }
        }

        public static TaskResult<TResultType> ResolvedWith(TResultType wrappedValue) => new() { 
            routine = TaskRoutine.Resolved, 
            Value = wrappedValue 
        };

        public static TaskResult<TResultType> CanceledWith(TResultType wrappedValue) => new()
        {
            routine = TaskRoutine.Cancelled,
            Value = wrappedValue
        };

        /// <summary>
        /// Use to chain <see cref="TaskRoutine"/>s together!
        /// </summary>
        /// <param name="nextRoutine"></param>
        /// <param name="stillRunOnCancel"></param>
        /// <returns>The same <see cref="TaskRoutine"/> you inputted, for convenience</returns>
        public TaskRoutine OnFinish(TaskRoutine nextRoutine, bool stillRunOnCancel = false)
        {
            //If the result obj isn't married to a routine (passed immediate result)
            //Immediately execute the nextRoutine
            //Cancellation is not determinable without a TaskRoutine to convey it
            if(routine == null)
                nextRoutine.Start();
            else
                routine.OnFinish(nextRoutine, stillRunOnCancel);

            return nextRoutine;
        }

        /// <summary>
        /// Use to chain <see cref="TaskRoutine"/>s together!
        /// </summary>
        /// <param name="func"></param>
        /// <param name="stillRunOnCancel"></param>
        /// <returns>The same <see cref="TaskRoutine"/> you inputted, for convenience</returns>
        public TaskRoutine OnFinish(TaskRoutineFunc func, bool stillRunOnCancel = false)
        {
            return OnFinish(New(func), stillRunOnCancel);
        }

        /// <summary>
        /// Callback overload that executes regardless of whether the task was cancelled or not
        /// </summary>
        /// <param name="resultValueCallback"></param>
        public void OnFinish(Action<TResultType, bool> resultValueCallback)
        {
            if(routine.IsResolved || (routine == null && ValueIsDefined))
            {
                bool wasCancelled = routine?.IsCancelled ?? false;
                resultValueCallback?.Invoke(Value, wasCancelled);
            }
            else
            {
                routine.FinishedEvent += OnceCallback;

                void OnceCallback(bool wasCancelled)
                {
                    resultValueCallback?.Invoke(Value, wasCancelled);
                    routine.FinishedEvent -= OnceCallback;
                }
            }
        }

        /// <summary>
        /// Callback overload that does not execute if the task was cancelled
        /// </summary>
        /// <param name="resultValue"></param>
        public void OnFinish(Action<TResultType> resultValue)
        {
            if(routine.IsResolved || (routine == null && ValueIsDefined))
            {
                resultValue?.Invoke(Value);
            }
            else
            {
                routine.FinishedEvent += OnceCallback;

                void OnceCallback(bool wasCancelled)
                {
                    if(!wasCancelled)
                    {
                        resultValue?.Invoke(Value);
                    }
                    routine.FinishedEvent -= OnceCallback;
                }
            }
        }

        /// <summary>
        /// Use to chain <see cref="TaskResult{T}"/>s together!
        /// </summary>
        /// <param name="nextRoutineWithResult"></param>
        /// <param name="stillRunOnCancel"></param>
        /// <returns>The same <see cref="TaskResult{T}"/> you inputted, for convenience</returns>
        public TaskResult<TOtherType> OnFinish<TOtherType>(TaskResult<TOtherType> nextRoutineWithResult, bool stillRunOnCancel = false)
        {
            if(routine == null)
                nextRoutineWithResult.routine.Start();
            else
                routine.OnFinish(nextRoutineWithResult.routine, stillRunOnCancel);

            return nextRoutineWithResult;
        }

        /// <summary>
        /// Use to chain <see cref="TaskResult{T}"/>s together!
        /// </summary>
        /// <param name="nextRoutineWithResult"></param>
        /// <param name="stillRunOnCancel"></param>
        /// <returns>The same <see cref="TaskResult{T}"/> you inputted, for convenience</returns>
        public TaskResult<TOtherType> OnFinish<TOtherType>(TaskResultFunc<TOtherType> func, bool stillRunOnCancel = false)
        {
            return OnFinish(New(func), stillRunOnCancel);
        }



        public override int GetHashCode() => (Value?.GetHashCode() ?? 0) + (31 * base.GetHashCode());

        //misleading; can potentially imply the value is ready when it isn't yet
        //Should always check the IsResolved flag before trying to work with the value
        //public static implicit operator TResultType(TaskResult<TResultType> taskResult) => taskResult.Value;

        public static implicit operator TaskResult<TResultType>(TResultType result) => ResolvedWith(result);
    }

}