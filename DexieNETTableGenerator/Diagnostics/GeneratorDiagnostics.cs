﻿/*
GeneratorDiagnostics.cs

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

using Microsoft.CodeAnalysis;

#pragma warning disable IDE0090 // Use 'new DiagnosticDescriptor(...)' doesn't work with target typed new

namespace DNTGenerator.Diagnostics
{
    internal partial class GeneratorDiagnostic
    {
        #region General
        public static DiagnosticDescriptor Internal =
            new DiagnosticDescriptor(
                "DNTGG000",
                "Internal error",
                "Internal error '{0}'",
                "DNTGenerator",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public static DiagnosticDescriptor Success =
            new DiagnosticDescriptor(
                "DNTGG001",
                "Generator success",
                "'{0}': successfully created 'DexieNetTables'",
                "DNTGenerator",
                DiagnosticSeverity.Info,
                isEnabledByDefault: true);

        public static DiagnosticDescriptor SyntaxReceiver =
           new DiagnosticDescriptor(
                "DNTGG002",
                "SyntaxReceiver error",
                "SyntaxReceiver creation failed",
                "DNTGenerator",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public static DiagnosticDescriptor Error =
            new DiagnosticDescriptor(
                "DNTGG003",
                "Generator error",
                "'{0}': failed to create 'DexieNetTables' check build and analyzer errors",
                "DNTGenerator",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);
        #endregion General

        #region DBRecord
        public static DiagnosticDescriptor NotPartial =
           new DiagnosticDescriptor(
               "DNTGG100",
               "Store error",
               "'{0}' is not partial but store without primary key or outbound index attribute must be partial",
               "DNTGenerator",
               DiagnosticSeverity.Error,
               isEnabledByDefault: true,
               "Store without primary key or outbound index attribute must be partial.",
               null,
               "Add partial modifier to '{0}'");

        public static DiagnosticDescriptor NotPartialAutoGuid =
          new DiagnosticDescriptor(
              "DNTGG101",
              "Store error",
              "'{0}' is not partial but store with Guid auto primary key must be partial",
              "DNTGenerator",
              DiagnosticSeverity.Error,
              isEnabledByDefault: true,
              "Store with Guid auto primary key must be partial.",
              null,
              "Add partial modifier to '{0}'");
        #endregion

        #region DBIndex
        public static DiagnosticDescriptor AutoIncrementNotNullable =
            new DiagnosticDescriptor(
                "DNTGG200",
                "Index error",
                "Autoincrement '{0}' is not nullable",
                "DNTGenerator",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                "Autoincrement index is not nullable.",
                null,
                "Make '{0}' nullable");

        public static DiagnosticDescriptor AutoIncrementNotNumeric =
            new DiagnosticDescriptor(
                "DNTGG201",
                "Index error",
                "Autoincrement '{0}' is not numeric",
                "DNTGenerator",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                "Autoincrement index type is not numeric.",
                null,
                new[] {
                    "Change '{0}'",
                    "Change '{0}' to 'ushort?'",
                    "Change '{0}' to 'uint?'",
                    "Change '{0}' to 'ulong?'"});

        public static DiagnosticDescriptor MultiEntryNotIEnumerable =
            new DiagnosticDescriptor(
                "DNTGG202",
                "Index error",
                "MultiEntry '{0}' is not IEnumerable",
                "DNTGenerator",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                "MultiEntry index type is not IEnumerable<type>.",
                null,
                "Change '{0}' to {1}");

        public static DiagnosticDescriptor NonMultiEntryNotArray =
           new DiagnosticDescriptor(
               "DNTGG203",
               "Index error",
               "Non MultiEntry '{0}' is IEnumerable",
               "DNTGenerator",
               DiagnosticSeverity.Error,
               isEnabledByDefault: true,
               "Non MultiEntry index type is IEnumerable<type>.",
               null,
               "Change '{0}' to {1}");

        public static DiagnosticDescriptor DuplicatePrimaryKeyMember =
            new DiagnosticDescriptor(
                "DNTGG210",
                "Primary key error",
                "'{0}' name identical to Schema 'PrimaryKeyName'",
                "DNTGenerator",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                "Parameter or property name identical to 'PrimaryKeyName'.",
                null,
                "Remove '{0}'");

        public static DiagnosticDescriptor DuplicatePrimaryKeySchemaArgument =
            new DiagnosticDescriptor(
                "DNTGG211",
                "Primary key error",
                "'{0}' is 'PrimaryKeyName' with same name as another parameter or property",
                "DNTGenerator",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                "Parameter or property name identical to 'PrimaryKeyName'.",
                null,
                "Remove '{0}'");

        public static DiagnosticDescriptor MultiplePrimaryKeysArgument =
           new DiagnosticDescriptor(
               "DNTGG220",
               "Primary key error",
               "'{0}' is 'IsPrimary', multiple 'Primary Keys' exist for same store",
               "DNTGenerator",
               DiagnosticSeverity.Error,
               isEnabledByDefault: true,
               "Multiple Primary Keys for same store.",
               null,
               "Remove '{0}'");

        public static DiagnosticDescriptor ReservedGeneratedPKNameSchemaArgument =
          new DiagnosticDescriptor(
              "DNTGG221",
              "Primary key error",
              "'{0}' has 'PrimaryKeyName' but 'IsPrimary' is already specified elsewhere",
              "DNTGenerator",
              DiagnosticSeverity.Error,
              isEnabledByDefault: true,
              "Reserved schema attribute for generated primary key.",
              null,
              "Remove '{0}'");

        public static DiagnosticDescriptor ReservedGeneratedPKGuidSchemaArgument =
          new DiagnosticDescriptor(
              "DNTGG222",
              "Primary key error",
              "'{0}' has 'PrimaryKeyGuid' but 'IsPrimary' is already specified elsewhere",
              "DNTGenerator",
              DiagnosticSeverity.Error,
              isEnabledByDefault: true,
              "Reserved schema attribute for generated primary key.",
              null,
              "Remove '{0}'");

        public static DiagnosticDescriptor AutoWithoutPrimaryKeyArgument =
          new DiagnosticDescriptor(
              "DNTGG223",
              "Primary key error",
              "'{0}' is 'IsAuto' but not 'IsPrimary'",
              "DNTGenerator",
              DiagnosticSeverity.Error,
              isEnabledByDefault: true,
              "Multiple Primary Keys for same store.",
              null,
              "Add 'IsPrimary'");

        public static DiagnosticDescriptor ReservedPrimaryKeyNameMember =
          new DiagnosticDescriptor(
              "DNTGG230",
              "Primary key error",
              "'ID' is a reserved name for auto-generated primary key",
              "DNTGenerator",
              DiagnosticSeverity.Error,
              isEnabledByDefault: true,
              "Reserved primary key name.",
              null,
              "Change name to '{0}'");

        public static DiagnosticDescriptor ReservedPrimaryKeyNameSchemaArgument =
          new DiagnosticDescriptor(
              "DNTGG231",
              "Primary key error",
              "'ID' is a reserved name for auto-generated primary key",
              "DNTGenerator",
              DiagnosticSeverity.Error,
              isEnabledByDefault: true,
              "Reserved primary key name.",
              null,
              "Change name to '{0}'");

        public static DiagnosticDescriptor CompoundIndexNotFound =
           new DiagnosticDescriptor(
               "DNTGG240",
               "CompoundIndex error",
               "CompoundIndexAttribute references properties that are not present in '{0}'",
               "DNTGenerator",
               DiagnosticSeverity.Error,
               isEnabledByDefault: true,
               "CompoundIndex not found.",
               null);

        public static DiagnosticDescriptor MissingIndexConverter =
           new DiagnosticDescriptor(
               "DNTGG300",
               "JSON Converter error",
               "Type for '{0}' needs a special converter, change to '{1}'",
               "DNTGenerator",
               DiagnosticSeverity.Error,
               isEnabledByDefault: true,
               "Missing JSON Converter.",
               null,
               "Change to '{0}'");
        #endregion
    }
#pragma warning restore IDE0090 // Use 'new DiagnosticDescriptor(...)'
}
