/*
DexieJSObject.cs

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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DexieNET
{
    public class DexieJSObject(IJSInProcessObjectReference module, IJSInProcessObjectReference? reference) : IJSInProcessObjectReference
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

    internal static class JsonExtensions
    {
         static readonly JsonSerializerOptions Options = new()
         {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static JsonElement? FromObject(this object? element)
        {
            if (element is null)
            {
                return null;
            }

            var json = JsonSerializer.SerializeToElement(element, Options);
            return json;
        }
    }
}
