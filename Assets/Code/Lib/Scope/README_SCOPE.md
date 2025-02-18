# What is Scope?

`Scope` is a dynamic cleanup mechanism that ensures automatic and predictable resource management. 

It provides a unified approach to managing side effects in code. It allows developers to attach "undo" actions to operations, ensuring resources are correctly cleaned up, even in cases of exceptions or crashes. 

This pattern replaces the traditional Dispose method, reducing dependencies and improving maintainability.
## Why use `Scope`?
Managing resource cleanup and side effects in Unity (and C# in general) can be complex and error-prone. The standard Dispose pattern requires manual tracking of dependencies, leading to tight coupling and brittle code.
`Scope` offers:
- Automatic Cleanup: Ensures that registered undo actions are executed in the correct order. 
- Decoupled Resource Management: Reduces dependencies on object references.
- Support for Nesting: Enables cascading cleanup actions. 
- Improved Readability: Makes control flow and resource management clearer. 
- Performance Optimization: Works faster than scene unloading and avoids domain reloads.

## Initialization
To use `Scope`, create an instance:
```csharp
Scope Scope = new Scope();
```
This creates a Scope instance that implements both `IDisposable` and `ISafeScope`, making it easy to integrate with existing cleanup mechanisms.

## Managing Scope Access and Scope

### **Ensuring Controlled Scope Usage**
When creating an instance of the `Scope` class, it implements both `ISafeScope` and `IDisposable`. However, if the Scope instance is passed to other methods or classes, it's recommended to **restrict access** to `ISafeScope`. This ensures that only designated parts of the code can finalize the Scope, while others can only **register cleanup actions**.

---

### **Why Restrict Access to `ISafeScope`?**
1. **Encapsulation:** Prevents unintended disposal of the Scope from within lower-level functions.
2. **Controlled Cleanup Flow:** Ensures that cleanup actions are properly accumulated without premature disposal.
3. **Predictability:** Makes it clear which part of the code has the authority to finalize the Scope.

---

### **Recommended Implementation**
Instead of passing `Scope` directly, cast it to `ISafeScope` when passing it to other methods.

```csharp
void SomeMethod(ISafeScope Scope) // Pass only ISafeScope, stating that the code below won't call Dispose()
{
    Scope.Add(() => Debug.Log("Cleanup action executed."));
}

// Usage
using(var Scope = new Scope())
{
    SomeMethod(Scope); 
} // Scope is disposed
```


### Adding Undo Actions
Attach cleanup actions dynamically:
```csharp
onClick += Callback;
Scope.Add(() => onClick -= Callback);
```
This ensures that Callback is unsubscribed when the Scope is disposed.

### Nested Scopes
Nested Scopes allow better hierarchical cleanup:
```csharp
Scope nestedScope = Scope.NestedScope();
Service1(nestedScope);
```
When the parent Scope is disposed, all nested Scopes are also cleaned up. This is useful for cases when you want some service to be disposed when the game stops and to be disposed on it's own.

## Example Comparisons
Here's an example comparing the `Dispose` pattern with the `Scope` approach:

### `Dispose` approach (Before)
```csharp
GameObject instantiatedItem;
GameObject prefab;
IPurchaseManager purchaseManager;

public SomeManager(IPurchaseManager purchaseManager)
{
    this.purchaseManager = purchaseManager;
    purchaseManager.PurchaseCompleted += OnPurchaseComplete;
}

void PurchaseCompleted()
{
    this.instantiatedItem = Object.Instantiate(prefab);
}

public void Dispose()
{
    // Here we don't know yet if the object was created or not, so we need to check all cases
    if (instantiatedItem != null)
    {
        Object.Destroy(instantiatedItem);
    }
    this.purchaseManager.PurchaseCompleted -= OnPurchaseComplete;
}
```
Issues with the `Dispose` approach

- State tracking complexity: Requires manually checking if instantiatedItem was created before disposing.
- Tight coupling: The `Dispose` method must handle all cleanup cases explicitly.
- Inconsistent resource cleanup: If an exception occurs before an object is instantiated, the cleanup logic still runs, potentially leading to errors.

### `Scope` approach (After)
```csharp
IScope Scope;
GameObject prefab;

public SomeManager(IPurchaseManager purchaseManager, IScope Scope)
{
    this.Scope = Scope;
    this.purchaseManager = purchaseManager;
    purchaseManager.PurchaseCompleted += OnPurchaseComplete;
    this.Scope.Add(() => purchaseManager.PurchaseCompleted -= OnPurchaseComplete);
}

void PurchaseCompleted()
{
    var instantiatedItem = Object.Instantiate(prefab);
    this.Scope.Add(() => Object.Destroy(instantiatedItem));
}
```
### Benefits of the Scope Pattern

- Easier Cleanup: No need to check whether `instantiatedItem` was created; if it's instantiated, the cleanup action is registered and will be called on Dispose.
- Reduced Coupling: Cleanup logic is localized to where the side effect occurs.
- Exception Safety: If an error occurs before instantiatedItem is created, no cleanup action is registered, avoiding redundant checks.
- Better Readability: The code follows a logical order, where every side effect immediately declares its corresponding cleanup.

Using the `Scope` pattern makes resource management more predictable, reduces manual checks, and improves maintainability by keeping cleanup logic close to where side effects occur. This approach is particularly useful in event-driven programming within Unity, where object lifetimes are not always predictable.

# Asynchronous Code Management with `Scope`

Using `Scope` in asynchronous code allows for structured cleanup actions, ensuring graceful resource management even in cases of exceptions. This approach is particularly useful in Unity when dealing with long-running tasks and event-driven logic.

## Handling Asynchronous Code with `Scope`

### Passing `Scope` as a Parameter
`Scope` can be passed as a parameter to asynchronous methods, allowing cleanup instructions to be accumulated dynamically.

```csharp
async Task Session(IScope gameScope)
{
    using(var sessionScope = gameScope.NestedScope())
    {
        while (true)
        {
            await Map(sessionScope);
            await Board(sessionScope);
        }
    }
}

async Task Board(IScope parentScope)
{
    using(Scope boardScope = parentScope.NestedScope())
    {
        var boardInstance = Instantiate(boardPrefab);
        boardScope.Add(() => Object.Destroy(boardInstance.gameObject));
        // Additional setup...
    }
}
```

Automatic Cleanup: When exiting the using block, the nested Scope will be disposed, ensuring that all registered cleanup actions are executed.
Graceful Degradation: In case of an exception, only the actions stored in the Scope will be executed, preventing partial or inconsistent states.

## Unity-Compatible Alternatives to `Task`


Using `System.Threading.Tasks.Task` is not recommended in Unity due to its threading model and the main thread constraint. Instead, Unity provides better alternatives:

- Async Routines Package – a specialized package for async operations with full control over the execution flow.
- UniTask – Officially recommended by Unity Technologies for improved async handling.

However, in cases where Task-based APIs must be used (e.g., third-party SDKs or library calls), `Scope` can be leveraged as a `CancellationToken` for proper task termination.

## Using `Scope` as a `CancellationToken`

When calling a library API that relies on Task, `Scope` can provide a cancellation token for controlled task cancellation:
```csharp
await Service.Login(Scope.GetCancellationToken());
```
Alternatively, an explicit `CancellationTokenSource` can be retrieved from the `Scope`:
```csharp
CancellationTokenSource cts = Scope.GetCancellationTokenSource();
await Service.Login(cts.Token);
```
## Handling Task Termination with Scope
Checking `Scope` Disposal Status

Within an asynchronous operation, `Scope` can be used to check whether it has been disposed and perform necessary cleanup:
```csharp
if (Scope.IsDisposed)
{
    // Cleanup logic
}
```
## Forcing early `System.Threading.Tasks.Task` Termination

If a `Scope` has been disposed and task execution should be stopped immediately, an `TaskCanceledException` can be thrown (proper way to halt the `Task` and bubble up):
```csharp
Scope.ThrowIfDisposed();
// Execute logic that should not run if Scope is disposed
```
## Exception Handling and Stability

`Scope`-based applications exhibit more stable behavior in the presence of exceptions:

- Only registered cleanup actions execute when a `Scope` is disposed, ensuring a predictable and controlled termination.
- Resources are released properly without requiring manual tracking.
- Nested Scopes maintain integrity, preventing inconsistent states during exceptions.

By using `Scope` in asynchronous workflows, you can achieve better resource management, cleaner code, and improved resilience in long-running operations.



## Conclusions
- Ensures cleanup actions are executed even in failure scenarios (graceful degradation).
- Eliminates the need to store references solely for cleanup.
- Deferred actions are only executed if added, ensuring stability.
- Clear control flow and feature hierarchy improve maintainability.
- Works seamlessly with `async`/`await` syntax, acting as `CancellationToken`.
- Manage object lifetimes within Unity's lifecycle constraints.
- Avoid domain reload for faster playmode switches.
- Faster than unloading the scene. 


Slightly outdated version can be found here:  
https://github.com/korchoon/rollback
