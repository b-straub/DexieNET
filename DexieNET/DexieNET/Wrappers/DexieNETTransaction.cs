/*
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
    public enum TAMode
    {
        Read,
        ReadWrite
    }

    public sealed class TransactionException : InvalidOperationException
    {
        public TransactionException(string? message) : base(message)
        {
        }
    }

    public sealed class TransactionBase : JSObject
    {
        public DBBase DB { get; }

        internal string? FirstError { get; set; }

        internal TransactionBase(DBBase db, IJSObjectReference reference) : base(db.DBBaseJS.Module, reference)
        {
            DB = db;
        }
    }


    internal sealed class Transaction : JSObject, IDisposable
    {
        public TransactionBase? TransactionBase { get; set; }

        private readonly DBBase _db;
        private readonly DotNetObjectReference<Transaction> _dotnetRef;
        private Func<TransactionBase?, Task>? _create;
        private readonly HashSet<string> _tableNames;
        private TAMode _mode = TAMode.Read;
        public Transaction(DBBase db) : base(db.DBBaseJS.Module, null)
        {
            _db = db;
            _dotnetRef = DotNetObjectReference.Create(this);
            _create = null;
            _tableNames = new();
            TransactionBase = null;
        }

        public bool AddTableInfo((string Name, TAMode Mode) tableInfo)
        {
            if (TransactionBase is not null)
            {
                return false;
            }

            _tableNames.Add(tableInfo.Name);
            if (_mode == TAMode.Read && tableInfo.Mode == TAMode.ReadWrite)
            {
                _mode = TAMode.ReadWrite;
            }

            return true;
        }

        public async ValueTask Commit(Func<TransactionBase?, Task> create)
        {
            _create = create;

            var tableNames = _tableNames.ToArray();
            await Module.InvokeVoidAsync("Transaction", _db.DBBaseJS.Reference, _dotnetRef, tableNames, _mode == TAMode.Read ? "r" : "rw");
        }

        [JSInvokable]
        public async ValueTask TransactionCallback()
        {
            if (!SetBaseTransaction())
            {
                throw new InvalidOperationException("Can not set current transaction.");
            }

            if (_create is not null)
            {
                try
                {
                    await _create(TransactionBase);
                }
                catch (Exception ex)
                {
                    TransactionBase.FirstError ??= ex.Message;
                    await TransactionBase.Abort();
                }
            }
        }

        public void Dispose()
        {
            _dotnetRef.Dispose();
        }

        [MemberNotNullWhen(returnValue: true, member: nameof(TransactionBase))]
        private bool SetBaseTransaction()
        {
            var tx = Module.Invoke<IJSObjectReference>("CurrentTransaction");

            if (tx is null)
            {
                return false;
            }

            TransactionBase ??= new(_db, tx);

            return true;
        }
    }

    public static class TransactionExtensions
    {
        public static async ValueTask Abort(this TransactionBase transaction)
        {
            await transaction.InvokeVoidAsync("abort");
        }
    }
}
