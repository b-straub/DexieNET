/*
DexieNETCloudPermissions.cs

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

using System.Linq.Expressions;
using R3;
using DexieNET;

// ReSharper disable once CheckNamespace
namespace DexieCloudNET
{
    public interface IUsePermissions<T> where T : IDBStore, IDBCloudEntity
    {
        public Observable<Unit> AsObservable { get; }
        public bool CanAdd(IDBCloudEntity? item = null, params ITable[] tables);
        public bool CanUpdate<Q>(IDBCloudEntity item, Expression<Func<T, Q>> query);
        public bool CanDelete(IDBCloudEntity item);
    }

    internal sealed class UsePermissions<T, I> : IUsePermissions<T> where T : IDBStore, IDBCloudEntity
    {
        public Observable<Unit> AsObservable { get; }

        private readonly Dictionary<string, PermissionChecker<T, I>> _permissionsCheckers = [];
        private readonly Table<T, I> _table;
        private readonly Subject<Unit> _changedSubject;
        private const string NoTableItemEntity = "noTableItemEntity";
        
        private UsePermissions(Table<T, I> table)
        {
            _table = table;
            _changedSubject = new();
            var pc = PermissionChecker<T, I>.Create(_changedSubject.AsObserver(), _table);
            _permissionsCheckers[NoTableItemEntity] = pc;
     
            AsObservable = _changedSubject
                .Do(onDispose:OnUnsubscribe)
                .Prepend(Unit.Default)
                .Share();
        }

        public static UsePermissions<T, I> Create(Table<T, I> table)
        {
            return new UsePermissions<T, I>(table);
        }

        public bool CanUpdate<Q>(IDBCloudEntity item, Expression<Func<T, Q>> query)
        {
            if (item.EntityKey is not null)
            {
                if (!_permissionsCheckers.TryGetValue(item.EntityKey, out PermissionChecker<T, I>? pc))
                {
                    pc = PermissionChecker<T, I>.Create(_changedSubject.AsObserver(), _table, item);
                    _permissionsCheckers[item.EntityKey] = pc;
                    _changedSubject.OnNext(Unit.Default);
                }

                return pc.CanUpdate(query);
            }

            return false;
        }

        public bool CanAdd(IDBCloudEntity? item = null, params ITable[] tables)
        {
            var tableNameList = tables.Select(t => t.Name).ToList();

            if (tableNameList.Count == 0)
            {
                tableNameList.Add(_table.Name);
            }

            if (item is null)
            {
                return _permissionsCheckers[NoTableItemEntity].CanAdd([.. tableNameList]);
            }

            if (item.EntityKey is not null)
            {
                if (!_permissionsCheckers.TryGetValue(item.EntityKey, out PermissionChecker<T, I>? pc))
                {
                    pc = PermissionChecker<T, I>.Create(_changedSubject.AsObserver(), _table, item);
                    _permissionsCheckers[item.EntityKey] = pc;
                    _changedSubject.OnNext(Unit.Default);
                }

                return pc.CanAdd([.. tableNameList]);
            }

            return false;
        }

        public bool CanDelete(IDBCloudEntity item)
        {
            if (item.EntityKey is not null)
            {
                if (!_permissionsCheckers.TryGetValue(item.EntityKey, out PermissionChecker<T, I>? pc))
                {
                    pc = PermissionChecker<T, I>.Create(_changedSubject.AsObserver(), _table, item);
                    _permissionsCheckers[item.EntityKey] = pc;
                    _changedSubject.OnNext(Unit.Default);
                }

                return pc.CanDelete();
            }

            return false;
        }

        private void OnUnsubscribe()
        {
            foreach (var pc in _permissionsCheckers.Values)
            {
                pc.Dispose();
            }

            _permissionsCheckers.Clear();
        }
    }

    internal sealed class PermissionChecker<T, I> : IDisposable where T : IDBStore, IDBCloudEntity
    {
        public string? EntityKey { get; private set; }
        private DexieJSObject? Cloud { get; }
        private readonly JSObservable<double> _jSObservable;
        private IDisposable? _jsSubscription;
        private bool _errorMode;
        private readonly Observer<Unit> _changedObserver;
      
        private PermissionChecker(Observer<Unit> changedObserver, Table<T, I> table, JSObservable<double> jSObservable, string? entityKey)
        {
            _errorMode = false;
            EntityKey = entityKey;
            Cloud = table.DB.Cloud;
            _jSObservable = jSObservable;
            _changedObserver = changedObserver;
            OnSubscribe();
        }

        public static PermissionChecker<T, I> Create(Observer<Unit> changedObserver, Table<T, I> table, IDBCloudEntity? item = null)
        {
            if (!table.CloudSync)
            {
                throw new InvalidOperationException("Can not create 'PermissionChecker' for non cloud table.");
            }

            var jso = JSObservable<double>.Create(table.DB, "SubscribePermissionChecker", "ClearPermissionChecker",
                table.Name, item);
            return new PermissionChecker<T, I>(changedObserver, table, jso, item?.EntityKey);
        }

        public bool CanAdd(params string[] tableNames)
        {
            if (Cloud is null)
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            if (_errorMode)
            {
                return true;
            }

            return Cloud.Module.Invoke<bool>("PermissionCheckerAdd", _jSObservable.Value, tableNames);
        }

        public bool CanUpdate<Q>(Expression<Func<T, Q>> query)
        {
            if (Cloud is null)
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            if (_errorMode)
            {
                return true;
            }

            return Cloud.Module.Invoke<bool>("PermissionCheckerUpdate", _jSObservable.Value, query.GetKey());
        }

        public bool CanDelete()
        {
            if (Cloud is null)
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            if (_errorMode)
            {
                return true;
            }


            return Cloud.Module.Invoke<bool>("PermissionCheckerDelete", _jSObservable.Value);
        }

        private void OnSubscribe()
        {
            _jsSubscription ??= _jSObservable
                .AsObservable
                // ReSharper disable once UnusedParameter.Local -> Debug
                .Subscribe(key =>
                    {
#if DEBUG
                        Console.WriteLine($"Subscribe PermissionChecker: {key}");
#endif
                        _changedObserver.OnNext(Unit.Default);
                    },
                    onCompleted: _ => { },
                    onErrorResume: (err) =>
                    {
                        Console.WriteLine($"Can not create PermissionChecker: {err.Message}");
                        _errorMode = true;
                        _changedObserver.OnNext(Unit.Default);
                    });
        }

        public void Dispose()
        {
            _jsSubscription?.Dispose();
            _jsSubscription = null;
        }
    }
}