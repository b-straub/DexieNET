/*
DexieCloudNETObservable.cs

Copyright(c) 2024 Bernhard Straub

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

'DexieNET' used with permission of David Fahlander 
*/

using System.Reactive.Linq;
using System.Reactive.Subjects;
using DexieNET;
using Microsoft.JSInterop;

namespace DexieCloudNET;

public class JSObservableKey<T>(DBBase db, string jsSubscribeFunction, string jsPostUnsubscribeFunction, params object?[] args) : JSObservable<T>(db, jsSubscribeFunction, jsPostUnsubscribeFunction, args)
{
    protected override void PostUnsubscribe()
    {
        if (!DB.HasCloud())
        {
            throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
        }

        ArgumentNullException.ThrowIfNull(Value);
        ArgumentNullException.ThrowIfNull(JsPostUnsubscribeFunction);

        DB.Cloud.Module.InvokeVoid(JsPostUnsubscribeFunction, Value);
    }
}

public class JSObservable<T> : IObservable<T>, IDisposable
{
    public T? Value => _subject.Value;
    protected DBBase DB { get; }
    protected string? JsPostUnsubscribeFunction { get; }

    private readonly DotNetObjectReference<JSObservable<T>> _dotnetRef;
    private readonly BehaviorSubject<T?> _subject;
    private readonly string _jsSubscribeFunction;

    private IJSInProcessObjectReference? _jsSubscription;
    private readonly IObservable<T> _observable;
    private readonly object?[] _args;

    public JSObservable(DBBase db, string jsSubscribeFunction, string? jsPostUnsubscribeFunction = null, params object?[] args)
    {
        DB = db;
        _dotnetRef = DotNetObjectReference.Create(this);
        _subject = new(default);
        _jsSubscribeFunction = jsSubscribeFunction;
        JsPostUnsubscribeFunction = jsPostUnsubscribeFunction;
        _args = args;

        _observable = _subject
            .Skip(1)
            .Where(x => x is not null)
            .Select(x => x!)
            .Finally(Unsubscribe)
            .Publish()
            .RefCount();
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        if (!DB.HasCloud())
        {
            throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
        }
#if DEBUG
        Console.WriteLine($"JSObservable subscribe: {_jsSubscribeFunction}");
#endif
        var disposable = _observable
            .Subscribe(observer);

        var args = new List<object?>() { DB.Cloud.Reference, _dotnetRef };
        args.AddRange(_args);

        _jsSubscription ??= DB.Cloud.Module.Invoke<IJSInProcessObjectReference>(_jsSubscribeFunction, [.. args]);
        return disposable;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Unsubscribe();
            _dotnetRef.Dispose();
        }
    }

    [JSInvokable]
    public void OnNext(T value)
    {
        _subject.OnNext(value);
    }

    [JSInvokable]
    public void OnCompleted()
    {
        _subject.OnCompleted();
    }

    [JSInvokable]
    public void OnError(string error)
    {
        _subject.OnError(new InvalidOperationException(error));
    }

    protected virtual void PostUnsubscribe()
    {
        if (!DB.HasCloud())
        {
            throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
        }

        if (JsPostUnsubscribeFunction is not null)
        {
            DB.Cloud.Module.InvokeVoid(JsPostUnsubscribeFunction);
#if DEBUG
            Console.WriteLine($"JSObservable unsubscribe: {JsPostUnsubscribeFunction}");
#endif
        }
    }

    private void Unsubscribe()
    {
        if (!DB.HasCloud())
        {
            throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
        }

        if (_jsSubscription is not null)
        {
            DB.Cloud.Module.InvokeVoid("UnSubscribeJSObservable", _jsSubscription);
            _jsSubscription = null;
            PostUnsubscribe();

#if DEBUG
            Console.WriteLine($"JSObservable unsubscribe");
#endif
        }
    }
}