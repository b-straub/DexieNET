﻿/*
DexieNETTransaction.cs

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
using System.Diagnostics.CodeAnalysis;

namespace DexieNET
{
    public enum TAType
    {
        Nested,
        TopLevel,
        Parallel,
    }

    public enum TAMode
    {
        Read,
        ReadWrite
    }

    internal enum TAState
    {
        Collecting,
        ParallelCollecting,
        Executing,
        ParallelExecuting,
    }

    public sealed class TransactionException : InvalidOperationException
    {
        public TransactionException(string? message) : base(message)
        {
        }
    }

    public sealed class Transaction : JSObject, IDisposable
    {
        public DBBase DB { get; }
        public bool Collecting { get; private set; }

        internal string? Error { get; set; }

        private Func<Transaction, Task>? _create;
        private Func<Task>? _waitFor;

        private readonly DotNetObjectReference<Transaction> _dotnetRef;
        private readonly bool _parallel;

        private TAMode _mode;
        private bool _aborted = false;

        private readonly HashSet<string> _tables;
        public Transaction(DBBase db, bool parallel) : base(db.DBBaseJS.Module, null)
        {
            DB = db;

            _dotnetRef = DotNetObjectReference.Create(this);
            _mode = TAMode.Read;
            _parallel = parallel;
            _tables = new();
        }

        internal bool AddTableInfo((string Name, TAMode Mode) tableInfo)
        {
            if (_parallel)
            {
                throw new InvalidOperationException("Outer parallel Transaction must be empty.");
            }

            if (!Collecting)
            {
                return false;
            }

            _tables.Add(tableInfo.Name);
            if (tableInfo.Mode is TAMode.ReadWrite && _mode is not TAMode.ReadWrite)
            {
                _mode = TAMode.ReadWrite;
            }

            return true;
        }

        internal void Commit(Func<Transaction, Task> create)
        {
            if (!_tables.Any())
            {
                throw new InvalidOperationException("Found empty Transaction.");
            }

            var mode = _mode is TAMode.ReadWrite ? "rw!" : "r!";
            _create = create;
            Module.InvokeVoid("TopLevelTransaction", DB.DBBaseJS.Reference, _tables, mode, _dotnetRef);
        }

        internal async ValueTask CommitAsync(Func<Transaction, Task> create)
        {
            if (!_tables.Any())
            {
                throw new InvalidOperationException("Found empty Transaction.");
            }

            var mode = _mode is TAMode.ReadWrite ? "rw!" : "r!";
            _create = create;
            await Module.InvokeVoidAsync("TopLevelTransactionAsync", DB.DBBaseJS.Reference, _tables, mode, _dotnetRef);
        }

        internal void StartCollect()
        {
            _aborted = false;
            Error = null;
            _tables.Clear();
            _mode = TAMode.Read;
            Collecting = true;
        }

        internal void StopCollect()
        {
            Collecting = false;
        }

        public bool Abort(string? reason = null)
        {
            if (_aborted)
            {
                return false;
            }

            Error = reason;
            _aborted = true;
            StopCollect();
            _tables.Clear();
            _mode = TAMode.Read;
            Module.InvokeVoid("AbortTransaction", Reference);
            return true;
        }

        public async ValueTask WaitFor(Func<Task> waitFor)
        {
            _waitFor = waitFor;

            if (Collecting)
            {
               await waitFor();
            }
            else
            {
                await Module.InvokeVoidAsync("TransactioWaitFor", _dotnetRef);
            }
        }

        [JSInvokable]
        public async ValueTask TransactionWaitForCallback()
        {
            if (!Collecting && _waitFor is not null)
            {
                try
                {
                    await _waitFor();
                }
                catch (Exception ex)
                {
                    Error = ex.Message;
                }
            }
        }

        [JSInvokable]
        public async ValueTask TransactionCallback()
        {
            SetJSO(Module.Invoke<IJSObjectReference>("CurrentTransaction"));

            if (_create is not null)
            {
                try
                {
                    await _create(this);
                    if (Error is not null)
                    {
                        throw new TransactionException(Error);
                    }
                }
                catch (Exception ex)
                {
                    DB.CurrentTransaction = this;
                    Abort(ex.Message);
                }
            }
        }

        public void Dispose()
        {
            _dotnetRef.Dispose();
        }
    }
}
