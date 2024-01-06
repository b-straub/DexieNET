/*
DexieJSObject.cs

Copyright(c) 2022 Bernhard Straub

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

using Microsoft.JSInterop;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DexieNET
{
    public interface IDexieJSObject : IJSInProcessObjectReference
    {
        public IJSInProcessObjectReference Module { get; }
    }

    public class DexieJSObject(IJSInProcessObjectReference module, IJSInProcessObjectReference? reference) : IDexieJSObject
    {
        public IJSInProcessObjectReference Module { get; } = module;
        public IJSInProcessObjectReference? Reference { get; private set; } = reference;

        public void SetReference(IJSInProcessObjectReference? reference)
        {
            Reference = reference;
        }

        public virtual ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (Reference is null)
            {
                throw new InvalidDataException("Set reference first.");
            }

            return Reference.InvokeAsync<TValue>(identifier, args);
        }

        public ValueTask InvokeVoidAsync(string identifier, object?[]? args)
        {
            if (Reference is null)
            {
                throw new InvalidDataException("Set reference first.");
            }

            return Reference.InvokeVoidAsync(identifier, args);
        }

        public virtual ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (Reference is null)
            {
                throw new InvalidDataException("Set reference first.");
            }

            return Reference.InvokeAsync<TValue>(identifier, cancellationToken, args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "Mimic base API")]
        public ValueTask InvokeVoidAsync(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (Reference is null)
            {
                throw new InvalidDataException("Set reference first.");
            }

            return Reference.InvokeVoidAsync(identifier, cancellationToken, args);
        }

        public TValue Invoke<TValue>(string identifier, params object?[]? args)
        {
            if (Reference is null)
            {
                throw new InvalidDataException("Set reference first.");
            }

            return Reference.Invoke<TValue>(identifier, args);
        }

        public virtual void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);

            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Reference?.Dispose();
                Reference = null;
            }
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (Reference is not null)
            {
                await Reference.DisposeAsync().ConfigureAwait(false);
            }

            Reference = null;
        }
    }

    public class JSObservableKey<T>(DBBase db, string jsSubscribeFunction, string jsPostUnsubscribeFunction, params object?[] args) : JSObservable<T>(db, jsSubscribeFunction, jsPostUnsubscribeFunction, args)
    {
        protected override void PostUnsubscribe()
        {
            ArgumentNullException.ThrowIfNull(Value);
            ArgumentNullException.ThrowIfNull(JsPostUnsubscribeFunction);

            DB.DBBaseJS.Module.InvokeVoid(JsPostUnsubscribeFunction, Value);
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
#if DEBUG
            Console.WriteLine($"JSObservable subscribe: {_jsSubscribeFunction}");
#endif
            var disposable = _observable
                .Subscribe(observer);

            var args = new List<object?>() { DB.DBBaseJS.Reference, _dotnetRef };
            args.AddRange(_args);

            _jsSubscription ??= DB.DBBaseJS.Module.Invoke<IJSInProcessObjectReference>(_jsSubscribeFunction, [.. args]);
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
            if (JsPostUnsubscribeFunction is not null)
            {
                DB.DBBaseJS.Module.InvokeVoid(JsPostUnsubscribeFunction);
#if DEBUG
                Console.WriteLine($"JSObservable unsubscribe: {JsPostUnsubscribeFunction}");
#endif
            }
        }

        private void Unsubscribe()
        {
            if (_jsSubscription is not null)
            {
                DB.DBBaseJS.Module.InvokeVoid("UnSubscribeJSObservable", _jsSubscription);
                _jsSubscription = null;
                PostUnsubscribe();

#if DEBUG
                Console.WriteLine($"JSObservable unsubscribe");
#endif
            }
        }
    }

    internal static class JsonExtensions
    {
        public static JsonElement? FromObject(this object? element)
        {
            if (element is null)
            {
                return null;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.SerializeToElement(element, options);
            return json;
        }
    }
}
