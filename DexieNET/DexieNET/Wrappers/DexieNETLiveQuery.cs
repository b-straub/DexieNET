/*
DexieNETLiveQuery.cs

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
        private readonly IObservable<Unit>[] _observables;
        private readonly IObservable<T> _queryObservable;

        public LiveQuery(DBBase db, Func<ValueTask<T>> query, params IObservable<Unit>[] observables)
        {
            _db = db;
            _query = query;
            _observables = observables;
            _changedSubject = new(default);

            _queryObservable = _changedSubject
                .Where(t => t is not null)
                .Select(t => t!)
                .Merge(
                    Observable.Merge(_observables)
                        .Select(_ => Observable.FromAsync(ExecuteQuery))
                        .Switch()
                )
                .DistinctUntilChanged()
                .Finally(LiveQueryUnsubscribe);

            DotnetRef = DotNetObjectReference.Create(this);
            ID = GetHashCode();
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
                _db.DBBaseJS.Module.InvokeVoid("LiveQueryUnsubscribe", ID);
            }
        }

        public void Dispose()
        {
            DotnetRef.Dispose();
        }
    }
}
