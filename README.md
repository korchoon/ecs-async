# Async Routine для EcsProto

## Важно
Из-за ограничений лицензии этот репозиторий не включает в себя пакеты `Leopotam.EcsProto`, `Leopotam.EcsProto.QoL` (они добавлены в `.gitignore`)

Прочитать о EcsProto и узнать, как получить его копию можно [на сайте автора](https://leopotam.ru/28/)

## Описание

`AsyncSystem` — это базовый класс, который позволяет записывать асинхронную логику в EcsProto в линейном стиле, используя синтаксис `async/await`. Это упрощает код, избавляя от callback hell и позволяя использовать такие синтаксические возможности, как:

- область видимости переменных,
- управление потоком (`flow control`),
- обработка исключений (`try/finally`).

## Решаемая проблема

В EcsProto (как и в других ECS фреймворках) не рекомендуется использовать `async` синтаксис (`Task`, `UniTask`, `Unity's Awaitable`) при работе с сущностями `ProtoEntity`, так как у этих async типов есть неявный шедулер, который работает вне цикла `SystemGroup`. Это может привести к непредсказуемым ошибкам при обращении к данным `ProtoEntity`.

`AsyncSystem` решает эту проблему за счет использования собственного типа `Routine`, который:

- не использует шедулер,
- оборачивается в систему, выполнение происходит строго в рамках очередности систем,
- перед каждым вызовом логики `await` проверяется, что сущность удовлетворяет требованиям к фильтру, что сущность жива, мир жив, и пр.


Таким образом, обращение к `ProtoEntity` остается безопасным в пределах всего `async` метода, без лишних упаковок-распаковок, так как все проверки проводятся до вызова `Routine.Tick()`.

## Когда использовать

### 1. Прототипирование

- Начинаем с `async` системы для быстрого тестирования логики.
- По мере стабилизации выносим важные компоненты и разрезаем на обычные ECS-системы.

### 2. Логика UI

- Взаимодействие с UI часто бывает сложным и неудобным для разбиения на отдельные системы.
- Поскольку UI редко требует массовой обработки, использование `async` упрощает код.
- При необходимости легко рефакторится в обычные системы.

## Дополнительные преимущества

- Сокращает количество вспомогательных компонентов-маркеров, которые нужны только для ожидания и не несут бизнес-логики.
- Позволяет выразить любой **Behaviour Tree** с помощью `Routine<bool>` и писать его в синтаксисе `async/await`, сохраняя контроль над выполнением.


## Добавление системы

Чтобы добавить AsyncSystem, необходимо создать класс, унаследованный от AsyncSystem<TSelf>, и реализовать два метода:
```csharp
class MySystem : AsyncSystem<MySystem> {
    protected override async Routine Run (ProtoEntity entity){
        await Routine.When(...)
        // ...
    }

    // фильтр как условие старта async логики
    protected override IProtoIt GetProtoIt () => new ProtoIt(It.Inc<SomeComponent>());
}
```

Под капотом используется кастомный Task-like тип `Routine`, подробности в  [README_ROUTINE.md](./Assets/Code/Lib/Routines/README_ROUTINE.md)


Для выполнения отложенной очистки используется тип `Scope` (похоже на `defer` в языке Go), подробности в [README_SCOPE.md](./Assets/Code/Lib/Scope/README_SCOPE.md)

## Примеры async систем

### Пример 1: SysUnit

```csharp
class SysUnit : AsyncSystem<SysUnit> {
    [DI] GameAspect _counterAspect = default;
    [DI] SceneContext _scene = default;

    protected override IProtoIt GetProtoIt () => new ProtoIt (It.Inc<CUnit> ());

    protected override async Routine Run (ProtoEntity entity) {
        var scope = await Routine.GetScope ();

        var view = Object.Instantiate (_scene.UnitPrefab, _scene.UnitSpawn);
        scope.Add (() => Object.Destroy (view.gameObject));

        var parallel = await Routine.GetParallel ();
        parallel.Attach (ObserveAmount ()); 

        // healthy state
        view.Image.color = Color.green;

        await Routine.When (() => _counterAspect.CUnit.Get (entity).Health < 50);
        // injured state
        view.Image.color = Color.yellow;

        await Routine.When (() => _counterAspect.CUnit.Get (entity).Health <= 0);
        // dead state
        view.Image.color = Color.red;

        var timer = 3f;
        await Routine.When (() => {
            timer -= Time.deltaTime;
            return timer < 0f;
        });

        _counterAspect.CUnit.Del (entity);
        return;

        async Routine ObserveAmount () {
            while (true) {
                var cache = _counterAspect.CUnit.Get (entity).Health;
                view.Slider.value = cache;
                view.Text.text = cache.ToString ();
                await Routine.When (() => cache != _counterAspect.CUnit.Get (entity).Health);
            }
        }
    }
}
```

### Пример 2: SysGameFlow

```csharp
class SysGameFlow : AsyncSystem<SysGameFlow> {
    [DI ()] GameAspect _gameAspect = default;
    [DI ()] SceneContext _sceneContext = default;

    override IProtoIt GetProtoIt () => new ProtoIt (It.Inc<CGame> ());

    override async Routine Run (ProtoEntity entity) {
        _sceneContext.MenuRoot.SetActive (true);
        while (true) {
            await _sceneContext.StartBtn.WaitForClick ();
            // game started

            _gameAspect.CUnit.NewEntity () = new () {
                Health = 100
            };

            await Routine.When (() => _gameAspect.UnitIt.Len () == 0);
            // game over
        
            await _sceneContext.GameOverBtn.WaitForClick ();
        }
    }

    void Shoot () {
        foreach (var unitE in _gameAspect.UnitIt) {
            _gameAspect.CUnit.Get (unitE).Health -= 10;
        }
    }
}
```