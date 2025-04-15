Amaze your devs with the power of TaskRoutines, a powerful and flexible wrapper for working with Coroutines in a more robust way, marrying the power of "asynchronous" workflows within the constraints of Unity's Coroutine framework.

While not technically asynchronous, Coroutines enable a similar deferrence of game logic within the safety of the main thread. Base Coroutines, however, have some significant limitations, which this package aims to lift with minimal hassle.

## TaskRoutine.Start() vs TaskRoutine.New()
There is now a distinction between the creation and launching of routines. New() enables defining a routine without immediately executing it, and Start will immediately create and execute a given routine

Routines are passed around as IEnumerables (or as function delegates that return them)

## And A Whole Lot More
TODO: Finish the readme

## How?
Coroutines have to be run on a GameObject, so we auto-instantiate an isolated manager they can run on.

