/*
DiagnosticException.cs

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
*/

using DNTGenerator.Verifier;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using static DNTGenerator.Verifier.DBRecordExtensions;

namespace DNTGenerator.Diagnostics
{
    internal partial class GeneratorDiagnostic
    {
        private readonly DiagnosticDescriptor _descriptor;
        private readonly Location? _location;
        private readonly object?[]? _messageArgs;
        private readonly Dictionary<string, string?> _properties = new();

        public string Id => _descriptor.Id;

        public GeneratorDiagnostic(DiagnosticDescriptor descriptor, params string?[] message)
        {
            _descriptor = descriptor;
            if (message is not null)
            {
                _messageArgs = message;
            }
        }

        public GeneratorDiagnostic(DiagnosticDescriptor descriptor, Location location, params string?[] message)
        {
            _descriptor = descriptor;
            _location = location;

            if (message is not null)
            {
                _messageArgs = message;
            }

            if (message is not null)
            {
                _messageArgs = message;
            }
        }

        public GeneratorDiagnostic(DiagnosticDescriptor descriptor, Location location, KeyValuePair<string, string?> property)
        {
            _descriptor = descriptor;
            _location = location;
            AddProperties(property);
        }

        public GeneratorDiagnostic(DiagnosticDescriptor descriptor, ISymbol symbol)
        {
            _descriptor = descriptor;
            _location = symbol.Locations.First();
            _messageArgs = new[] { symbol.Name };
        }

        public GeneratorDiagnostic(DiagnosticDescriptor descriptor, DBRecord dbRecord)
        {
            _descriptor = descriptor;
            _location = dbRecord.Symbol.Locations.First();
            _messageArgs = new[] { dbRecord.Symbol.Name };
        }

        public GeneratorDiagnostic(DiagnosticDescriptor descriptor, DBRecord dbRecord, params string?[] message)
        {
            _descriptor = descriptor;
            _location = dbRecord.Symbol.Locations.First();
            List<string?>? messages = new()
            {
                dbRecord.Symbol.Name
            };
            messages.AddRange(message);

            _messageArgs = messages.ToArray();
        }

        public GeneratorDiagnostic(DiagnosticDescriptor descriptor, IndexDescriptor id, params string?[] message)
        {
            _descriptor = descriptor;
            _location = id.Symbol.Locations.First();
            List<string?>? messages = new()
            {
                id.Name
            };
            messages.AddRange(message);

            _messageArgs = messages.ToArray();
        }
        public GeneratorDiagnostic(DiagnosticDescriptor descriptor, IndexDescriptor id)
        {
            _descriptor = descriptor;
            _location = id.Symbol.Locations.First();
            _messageArgs = new[] { id.Name };
        }

        public void ReportDiagnostic(Action<Diagnostic> reportDiagnostics)
        {
            reportDiagnostics(Diagnostic.Create(_descriptor, _location, _properties.ToImmutableDictionary(), _messageArgs));
        }

        public void AddProperties(KeyValuePair<string, string?> property)
        {
            _properties.Add(property.Key, property.Value);
        }
    }

    internal class DiagnosticException : Exception
    {
        public GeneratorDiagnostic Diagnostic { get; }

        public DiagnosticException()
        {
            Diagnostic = new(GeneratorDiagnostic.Internal, "Unknown error");
        }

        public DiagnosticException(string message)
            : base(message)
        {
            Diagnostic = new(GeneratorDiagnostic.Internal, message);
        }

        public DiagnosticException(string message, Exception inner)
            : base(message, inner)
        {
            Diagnostic = new(GeneratorDiagnostic.Internal, message);
        }

        public DiagnosticException(GeneratorDiagnostic diagnostic)
        {
            Diagnostic = diagnostic;
        }
    }
}
