DexieCloudNET
========

DexieCloudNET is a .NET wrapper for dexie.js minimalist wrapper for IndexedDB see https://dexie.org with cloud support see https://dexie.org/cloud/ .

*'DexieNET' used with permission of David Fahlander*

**DexieCloudNET** aims to be a feature complete .NET wrapper for **Dexie.js** the famous Javascript IndexedDB wrapper from David Fahlander including support for **cloud sync**.

I consists of two parts, a source generator converting  a C# record, class, struct to a DB store and a set of wrappers around the well known Dexie.js API constructs such as *Table, WhereClause, Collection*, ...

It's designed to work within a Blazor Webassembly application with minimal effort.

The port of the **ToDoSample** to .NET is a good starting point for exploring all the cloud features.