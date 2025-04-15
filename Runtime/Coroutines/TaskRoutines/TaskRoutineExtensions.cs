using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IDEK.Tools.Coroutines.TaskRoutines
{
    public static class TaskRoutineExtensions
    {
        /// <summary>
        /// Will attempt to run nextRoutine once all given routines have completed.
        /// </summary>
        /// <param name="routines"></param>
        /// <param name="nextRoutine"></param>
        /// <param name="stillRunOnAnyCancel">If true, then <see cref="nextRoutine"/> will run once all given routines have either resolved or cancelled. If false, the entire process will be aborted if any of the given routines are cancelled.</param>
        /// <typeparam name="T">Used to clarify that the output is guaranteed to be the same given type as the input for this method (which must fulfill <see cref="IEnumerable{TaskRoutine}"/> </typeparam>
        /// <returns></returns>
        public static T OnAllFinish<T>(this T routines, TaskRoutine.FinishedHandlerWithCancelCheck callback, bool stillRunOnAnyCancel = false) where T : IEnumerable<TaskRoutine>
        {
            return TaskRoutine.OnAllFinish(routines, callback, stillRunOnAnyCancel);
        }

        /// <summary>
        /// Will attempt to run nextRoutine once all given routines have completed.
        /// </summary>
        /// <param name="routines"></param>
        /// <param name="nextRoutine"></param>
        /// <param name="stillRunOnAnyCancel">If true, then <see cref="nextRoutine"/> will run once all given routines have either resolved or cancelled. If false, the entire process will be aborted if any of the given routines are cancelled.</param>
        /// <typeparam name="T">Used to clarify that the output is guaranteed to be the same given type as the input for this method (which must fulfill <see cref="IEnumerable{TaskRoutine}"/> </typeparam>
        /// <returns></returns>
        public static TaskRoutine OnAllFinish<T>(this T routines, Action callback, bool stillRunOnAnyCancel = false) where T : IEnumerable<TaskRoutine>
        {
            return TaskRoutine.OnAllFinish(routines, callback, stillRunOnAnyCancel);
        }

        /// <summary>
        /// Will attempt to run nextRoutine once all given routines have completed.
        /// </summary>
        /// <param name="routines"></param>
        /// <param name="nextRoutine"></param>
        /// <param name="stillRunOnAnyCancel">If true, then <see cref="nextRoutine"/> will run once all given routines have either resolved or cancelled. If false, the entire process will be aborted if any of the given routines are cancelled.</param>
        /// <returns></returns>
        public static TaskRoutine OnAllFinish(this IEnumerable<TaskRoutine> routines, TaskRoutine nextRoutine, bool stillRunOnAnyCancel = false)
        {
            return TaskRoutine.OnAllFinish(routines, nextRoutine, stillRunOnAnyCancel);
        }

        public static WaitUntil GetOnAllFinishedYield(this IEnumerable<TaskRoutine> routines, bool stillCountIfAnyCancel) => TaskRoutine.GetOnAllFinishedYield(routines, stillCountIfAnyCancel);
        public static WaitUntil AwaitAll(this IEnumerable<TaskRoutine> routines) => TaskRoutine.AwaitAll(routines);

        public static IEnumerable<TaskRoutine> CancelAll(this IEnumerable<TaskRoutine> routines)
        {
            foreach (TaskRoutine v in routines)
            {
                v?.Cancel();
            }

            return routines;
        }
    }
}