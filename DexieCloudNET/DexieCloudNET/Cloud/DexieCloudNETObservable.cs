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

using R3;
using DexieNET;
using Microsoft.JSInterop;

// ReSharper disable once CheckNamespace
namespace DexieCloudNET;

public record TimeOut<T>(TimeSpan TimeSpan, T Value);

internal class JSObservable<T>
{
    public Observable<T> AsObservable { get; }

    public T? Value => _subject.Value;

    private readonly DBBase _db;
    private DotNetObjectReference<JSObservable<T>>? _dotnetRef;
    private readonly BehaviorSubject<T?> _subject;
    private readonly string _jsSubscribeFunction;
    private readonly string? _jsUnSubscribeFunction;
   
    private IJSInProcessObjectReference? _jsSubscription;
    private readonly object?[] _args;

    public static JSObservable<T> Create(DBBase db, string jsSubscribeFunction)
    {
        return new JSObservable<T>(db, jsSubscribeFunction);
    }

    public static JSObservable<T> Create(DBBase db, string jsSubscribeFunction, TimeOut<T>? timeout)
    {
        return new JSObservable<T>(db, jsSubscribeFunction, null, null, timeout);
    }

    public static JSObservable<T> Create(DBBase db, string jsSubscribeFunction, string? jsUnsubscribeFunction)
    {
        return new JSObservable<T>(db, jsSubscribeFunction, jsUnsubscribeFunction);
    }

    public static JSObservable<T> Create(DBBase db, string jsSubscribeFunction, string? jsUnsubscribeFunction, params object?[] args)
    {
        return new JSObservable<T>(db, jsSubscribeFunction, jsUnsubscribeFunction, null, args);
    }

    protected JSObservable(DBBase db, string jsSubscribeFunction, string? jsUnsubscribeFunction = null, TimeOut<T>? timeout = null, params object?[] args)
    {
        _db = db;
        _subject = new(default);
        _jsSubscribeFunction = jsSubscribeFunction;
        _jsUnSubscribeFunction = jsUnsubscribeFunction;
        _args = args;

        AsObservable = _subject
            .Do(onSubscribe: OnSubscribe, onDispose: OnUnsubscribe)
            .Where(x => x is not null)
            .Select(x => x!)
            .Share();

        if (timeout is not null)
        {
            AsObservable = AsObservable.Timeout(timeout.TimeSpan).Select(_ => timeout.Value);
        }
    }

    [JSInvokable]
    public void OnNextJS(T value)
    {
        _subject.OnNext(value);
    }

    [JSInvokable]
    public void OnCompletedJS()
    {
        _subject.OnCompleted();
    }

    [JSInvokable]
    public void OnErrorJS(string error)
    {
        _subject.OnErrorResume(new InvalidOperationException(error));
    }

    private void OnSubscribe()
    {
        OnUnsubscribe();

        if (!_db.HasCloud())
        {
            throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
        }

        if (_dotnetRef is null && _jsSubscription is null)
        {
#if DEBUG
            Console.WriteLine($"JSObservable subscribe: {_jsSubscribeFunction}");
#endif
            _dotnetRef = DotNetObjectReference.Create(this);
            var args = new List<object?>() { _db.Cloud.Reference, _dotnetRef };
            args.AddRange(_args);
            _jsSubscription = _db.Cloud.Module.Invoke<IJSInProcessObjectReference>(_jsSubscribeFunction, [.. args]);
        }
    }

    private void OnUnsubscribe()
    {
        if (!_db.HasCloud())
        {
            throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
        }

        if (_dotnetRef is not null && _jsSubscription is not null)
        {
#if DEBUG
            Console.WriteLine($"JSObservable unsubscribe");
#endif
            _db.Cloud.Module.InvokeVoid("UnSubscribeJSObservable", _jsSubscription);
            _jsSubscription.Dispose();
            _jsSubscription = null;
            _dotnetRef?.Dispose();
            _dotnetRef = null;

            if (_jsUnSubscribeFunction is not null)
            {
#if DEBUG
                Console.WriteLine($"JSObservable unsubscribe: {_jsUnSubscribeFunction} with {Value}");
#endif
                _db.Cloud.Module.InvokeVoid(_jsUnSubscribeFunction, Value);
            }
        }
    }
}