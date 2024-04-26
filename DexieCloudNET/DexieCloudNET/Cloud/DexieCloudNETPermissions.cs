/*
DexieNETCloudPermissions.cs

Copyright(c) 2023 Bernhard Straub

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
using System.Data;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DexieNET;

namespace DexieCloudNET
{
    public interface IUsePermissions<T> : IObservable<Unit>, IDisposable where T : IDBStore, IDBCloudEntity
    {
        public bool CanAdd(IDBCloudEntity? item = null, params ITable[] tables);
        public bool CanUpdate<Q>(IDBCloudEntity item, Expression<Func<T, Q>> query);
        public bool CanDelete(IDBCloudEntity item);
    }

    public sealed class UsePermissions<T, I> : IUsePermissions<T>, IDisposable where T : IDBStore, IDBCloudEntity
    {
        private readonly Dictionary<string, PermissionChecker<T, I>> _updatePermissions = [];
        private PermissionChecker<T, I> _tablePermissions;
        private readonly Table<T, I> _table;
        private readonly Subject<Unit> _changedSubject;
        private readonly IDisposable? _tpDisposable;

        private UsePermissions(Table<T, I> table)
        {
            _table = table;
            _changedSubject = new();
            _tablePermissions = PermissionChecker<T, I>.Create(_table);

            _tpDisposable = _tablePermissions
                .Skip(1)
                .Subscribe(pc =>
                {
                    _tablePermissions = pc;
                    _changedSubject.OnNext(Unit.Default);
                });
        }

        public static UsePermissions<T, I> Create(Table<T, I> table)
        {
            return new UsePermissions<T, I>(table);
        }

        public bool CanUpdate<Q>(IDBCloudEntity item, Expression<Func<T, Q>> query)
        {
            if (item.EntityKey is not null)
            {
                if (!_updatePermissions.TryGetValue(item.EntityKey, out PermissionChecker<T, I>? pc))
                {
                    pc = PermissionChecker<T, I>.Create(_table, item);
                    _updatePermissions[item.EntityKey] = pc;
                    _changedSubject.OnNext(Unit.Default);
                }

                return pc.CanUpdate(query);
            }

            return false;
        }

        public IDisposable Subscribe(IObserver<Unit> observer)
        {
            return _changedSubject.Subscribe(observer);
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
                return _tablePermissions.CanAdd([.. tableNameList]);
            }

            if (item.EntityKey is not null)
            {
                if (!_updatePermissions.TryGetValue(item.EntityKey, out PermissionChecker<T, I>? pc))
                {
                    pc = PermissionChecker<T, I>.Create(_table, item);
                    _updatePermissions[item.EntityKey] = pc;
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
                if (!_updatePermissions.TryGetValue(item.EntityKey, out PermissionChecker<T, I>? pc))
                {
                    pc = PermissionChecker<T, I>.Create(_table, item);
                    _updatePermissions[item.EntityKey] = pc;
                    _changedSubject.OnNext(Unit.Default);
                }
                return pc.CanDelete();
            }
            return false;
        }

        public void Dispose()
        {
            _tpDisposable?.Dispose();
            _tablePermissions.Dispose();
            foreach (var pc in _updatePermissions.Values)
            {
                pc.Dispose();
            }
            _updatePermissions.Clear();
        }
    }

    internal interface IPermissionSubscription
    {
        public IJSInProcessObjectReference Subscription { get; }
        public double Key { get; }
    }

    internal sealed class PermissionChecker<T, I> : IObservable<PermissionChecker<T, I>>, IDisposable where T : IDBStore, IDBCloudEntity
    {
        public string? EntityKey { get; private set; }

        internal DexieJSObject PermissionCheckerJS { get; }

        private readonly JSObservableKey<double>? _jSObservable;

        private readonly BehaviorSubject<PermissionChecker<T, I>?> _updateSubject = new(null);
        private readonly IObservable<PermissionChecker<T, I>> _observable;
        private readonly IDisposable _jsDisposable;

        private bool _errorMode;

        private PermissionChecker(Table<T, I> table, JSObservableKey<double> jSObservable, string? entityKey)
        {
            _errorMode = false;
            EntityKey = entityKey;
            PermissionCheckerJS = new(table.TableJS.Module, null);
            _jSObservable = jSObservable;

            _jsDisposable = _jSObservable
                .Subscribe(_ =>
                {
                    _updateSubject.OnNext(this);
                },
                onError: (err) =>
                {
                    Console.WriteLine($"Can not create PermissionChecker: {err.Message}");
                    _errorMode = true;
                    _updateSubject.OnNext(this);
                });

            _observable = _updateSubject
               .Skip(1)
               .Where(x => x is not null)
               .Select(x => x!)
               .Publish()
               .RefCount();
        }

        public static PermissionChecker<T, I> Create(Table<T, I> table, IDBCloudEntity? item = default)
        {
            if (!table.CloudSync)
            {
                throw new InvalidOperationException("Can not create 'PermissionChecker' for non cloud table.");
            }

            var jso = new JSObservableKey<double>(table.DB, "SubscribePermissionChecker", "ClearPermissionChecker", table.Name, item);
            return new PermissionChecker<T, I>(table, jso, item?.EntityKey);
        }

        public bool CanAdd(params string[] tableNames)
        {
            if (_errorMode)
            {
                return true;
            }

            if (_jSObservable?.Value is null)
            {
                return false;
            }

            return PermissionCheckerJS.Module.Invoke<bool>("PermissionCheckerAdd", _jSObservable.Value, tableNames);
        }

        public bool CanUpdate<Q>(Expression<Func<T, Q>> query)
        {
            if (_errorMode)
            {
                return true;
            }

            if (_jSObservable?.Value is null)
            {
                return false;
            }

            return PermissionCheckerJS.Module.Invoke<bool>("PermissionCheckerUpdate", _jSObservable.Value, query.GetKey());
        }

        public bool CanDelete()
        {
            if (_errorMode)
            {
                return true;
            }

            if (_jSObservable?.Value is null)
            {
                return false;
            }

            return PermissionCheckerJS.Module.Invoke<bool>("PermissionCheckerDelete", _jSObservable.Value);
        }

        public IDisposable Subscribe(IObserver<PermissionChecker<T, I>> observer)
        {
            return _observable
                .StartWith(this)
                .Subscribe(observer);
        }

        public void Dispose()
        {
            _jsDisposable.Dispose();
        }
    }
}
