/*
DexieNETLiveQuery.cs

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

using Microsoft.JSInterop;
using R3;

namespace DexieNET
{
    public interface IUseLiveQuery<T>
    {
        public Observable<T> AsObservable { get; }
    }
    
    public interface ILiveQuery<T>
    {
        public Observable<T> AsObservable { get; }
        public IUseLiveQuery<T> UseLiveQuery(params Observable<Unit>[] observables);
    }
    
    internal sealed class LiveQuery<T> : ILiveQuery<T>, IUseLiveQuery<T>
    {
        public Observable<T> AsObservable { get; }
        public Observable<T> AsRawObservable { get; }
        
        private DotNetObjectReference<LiveQuery<T>>? _dotnetRef;
        private long? _id;

        private readonly DBBase _db;
        private readonly Func<ValueTask<T>> _query;
        private readonly Subject<T> _subject;
        
        public LiveQuery(DBBase db, Func<ValueTask<T>> query, params Observable<Unit>[] observables)
        {
            _db = db;
            _query = query;
            _subject = new();

            AsRawObservable = _subject
                .Do(onSubscribe: OnSubscribe, onDispose: OnUnsubscribe);
            
            AsObservable = AsRawObservable
                .DistinctUntilChanged()
                .Share();
        }

        public IUseLiveQuery<T> UseLiveQuery(params Observable<Unit>[] observables)
        {
            return new UseLiveQuery<T>(this, observables);
        }

        [JSInvokable]
        public async ValueTask<T> LiveQueryCallback()
        {
            return await _query();
        }

        [JSInvokable]
        public void OnNextJS(T value)
        {
            _subject.OnNext(value);
        }

        [JSInvokable]
        public void OnCompletedJS()
        {
            _subject.OnCompleted(Result.Success);
        }

        [JSInvokable]
        public void OnErrorJS(string error)
        {
            _subject.OnErrorResume(new InvalidOperationException(error));
        }
        
        private void OnSubscribe()
        {
            OnUnsubscribe();
            if (_dotnetRef is null && _id is null)
            {
                _dotnetRef = DotNetObjectReference.Create(this);
                _id = _db.DBBaseJS.Module.Invoke<long>("LiveQuerySubscribe", _dotnetRef);
#if DEBUG
                Console.WriteLine($"LiveQuery subscribe: {_id}");
#endif
            }
        }

        private void OnUnsubscribe()
        {
            if (_dotnetRef is not null && _id is not null)
            {
#if DEBUG
                Console.WriteLine($"LiveQuery unsubscribe: {_id}");
#endif
                _db.DBBaseJS.Module.InvokeVoid("LiveQueryUnsubscribe", _id);
                _id = null;

                _dotnetRef?.Dispose();
                _dotnetRef = null;
            }
        }
    }

    internal sealed class UseLiveQuery<T>(LiveQuery<T> liveQuery, params Observable<Unit>[] observables)
        : IUseLiveQuery<T>
    {
        public Observable<T> AsObservable { get; } = observables.Length == 0
            ? liveQuery.AsObservable
            : Observable.Merge(liveQuery.AsRawObservable,
                    observables
                        .Merge()
                        .SelectAwait(async (_, _) => await liveQuery.LiveQueryCallback(), AwaitOperation.Switch))
                .DistinctUntilChanged()
                .Share();
    }
}