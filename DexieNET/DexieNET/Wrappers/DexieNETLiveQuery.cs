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
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace DexieNET
{
    public sealed class LiveQuery<T> : IObservable<T>, IDisposable
    {
        internal int ID { get; }
        internal DotNetObjectReference<LiveQuery<T>> DotnetRef { get; }

        private readonly DBBase _db;
        private readonly Func<ValueTask<T>> _query;
        private readonly BehaviorSubject<T?> _changedSubject;
        private readonly IObservable<T> _queryObservable;

        internal LiveQuery(DBBase db, Func<ValueTask<T>> query)
        {
            _db = db;
            _query = query;
            _changedSubject = new(default);

            _queryObservable = _changedSubject
                .Where(t => t is not null)
                .Select(t => t!)
                .DistinctUntilChanged()
                .Finally(LiveQueryUnsubscribe);

            DotnetRef = DotNetObjectReference.Create(this);
            ID = GetHashCode();
        }

        public UseLiveQuery<T> UseLiveQuery(params IObservable<Unit>[] observables)
        {
            return new UseLiveQuery<T>(this, observables);
        }

        [JSInvokable]
        public async ValueTask LiveQueryCallback()
        {
            _db.LiveQueryRunning = true;

            try
            {
                var result = await _query();
                _changedSubject.OnNext(result);
            }
            catch (Exception ex)
            {
                _changedSubject.OnError(ex);
            }

            _db.LiveQueryRunning = false;
        }

        public async Task<T> ExecuteQuery()
        {
            return await _query();
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            bool liveQuerySubscribe = !_changedSubject.HasObservers;

            if (liveQuerySubscribe)
            {
#if DEBUG
                Console.WriteLine($"LiveQuery subscribe: {ID}");
#endif
                _db.DBBaseJS.Module.InvokeVoid("LiveQuerySubscribe", ID);
                return _changedSubject.Value is null ?
                    _queryObservable.Subscribe(observer) :
                    _queryObservable.Skip(1).Subscribe(observer); // LiveQuerySubscribe invokes initial query
            }

            return _queryObservable.Subscribe(observer); ;
        }

        private void LiveQueryUnsubscribe()
        {
            if (!_changedSubject.HasObservers)
            {
#if DEBUG
                Console.WriteLine($"LiveQuery unsubscribe: {ID}");
#endif
                _db.DBBaseJS.Module.InvokeVoid("LiveQueryUnsubscribe", ID);
            }
        }

        public void Dispose()
        {
            DotnetRef.Dispose();
        }
    }

    public sealed class UseLiveQuery<T> : IObservable<T>
    {
        private readonly IObservable<Unit> _lqTrigger;
        private readonly LiveQuery<T> _liveQuery;
        private IDisposable? _lqDisposable;

        internal UseLiveQuery(LiveQuery<T> liveQuery, params IObservable<Unit>[] observables)
        {
            _liveQuery = liveQuery;

            _lqTrigger = observables.Length == 0 ? 
            Observable.Return(Unit.Default) :
            Observable
                .Merge(observables)
                .StartWith(Unit.Default)
                .Finally(Dispose);
        }

        private void Dispose()
        {
            _lqDisposable?.Dispose();
            _liveQuery.Dispose();
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return _lqTrigger
                .Subscribe(_ =>
                {
                    _lqDisposable?.Dispose();
                    _lqDisposable = _liveQuery.Subscribe(observer);
                });
        }
    }
}
