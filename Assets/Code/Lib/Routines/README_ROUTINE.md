# What is async Routine?

Custom `async` type. Very different from `Task`, allows to: 
- use `async` syntax without Threading
- Allows to stop at any time by calling Routine.Dispose()
- Excplicit scheduling: doesn't rely on implicit scheduler, executes only when you call `Routine.Tick()`

## Difference from Systems.Threading.Task:
- Doesn’t have scheduler. You need to call Routine.Tick() manually
- Cannot have multiple awaiters. Awaiter is Routine itself and is used for calling Routine.Tick(), Dispose(), etc,
- Routine can be disposed with it's sub-routine tree without CancellationToken-like mechanism, just calling Dispose()
- Any cleanup is better to implement via Scope.Add()  

# How to use it?

```csharp
bool flag1;

async Routine Flow(){
	Scope scope = await Routine.GetScope();
	scope.Defer(() => Debug("Flow() disposed"));
	Debug("Start");
	await Routine.Yield;
	Debug("Tick 1");
	await Routine.When(() => flag1);
	Debug("flag1 is true");
}

void Run_Normal(){
	Routine flow = Flow(); // output: Start
	flow.Tick();           // output: Tick 1
	flow.Tick();           // 
	flag1 = true;          // 
	flow.Tick();           // output: is true
	                       // Flow() disposed
}

void Run_Dispose(){
	Routine flow = Flow(); // output: Start
	flow.Dispose();        // output: Flow() disposed
	flow.Tick();           // does nothing
}
```

## 2 built-in operations:

```csharp
Scope scope = await Routine.GetScope();
scope.Defer(() => Debug.Log("Executed on routine dispose"))

Parallel parallel = await Routine.GetParallel();
parallel.Attach(AnimateTweens()); // 
```

## Extendable with awaiters:
You can also extend with your own awaiters similar to these:

```csharp
await Routine.When(Func<bool>); // yields until predicate is true

await Routine.Yield; // yields until next Tick()

await Routine.YieldTicks(3); // yields for 3 Ticks 

await Routine.Forever; // never pass this await

IList<Routine<T>> results = await Routine.WhenAny<T>(IList<Routine<T>>); 

await Routine.WhenAll<T>(IList<Routine<T>>);

await SomeRoutine().Configure(Func<bool> breakIf, Action onUpdate); // allows to wrap routine 
```


Slightly outdated version can be found here:  
https://github.com/korchoon/mk.routines