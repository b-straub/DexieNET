/*
JSObject.cs

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

namespace DexieNET
{
    public interface IJSObject : IJSObjectReference
    {
        public IJSInProcessObjectReference Module { get; }
    }

    public class JSObject : IJSObject
    {
        public IJSInProcessObjectReference Module { get; }
        public IJSObjectReference? Reference { get; private set; }

        private bool _disposed;

        public JSObject(IJSInProcessObjectReference module, IJSObjectReference? reference)
        {
            Module = module;
            Reference = reference;
        }

        public void SetJSO(IJSObjectReference? reference)
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

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (!_disposed && Reference is not null)
            {
                await Reference.DisposeAsync().ConfigureAwait(false);
            }

            _disposed = true;
        }
    }
}
