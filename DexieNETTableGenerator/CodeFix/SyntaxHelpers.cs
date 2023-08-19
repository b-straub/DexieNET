/*
SyntaxHelpers.cs

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

using DNTGenerator.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace DNTGenerator.CodeFix
{
    internal static class SyntaxHelpers
    {
        public static Document RemoveType(this Document document, SyntaxNode root, TypeDeclarationSyntax typeDeclaration, SyntaxNode typeSyntax)
        {
            switch (typeSyntax.Kind())
            {
                case SyntaxKind.Parameter:
                    var parameter = (ParameterSyntax)typeSyntax;

                    if (parameter is null || !(typeDeclaration.IsKind(SyntaxKind.RecordDeclaration) || typeDeclaration.IsKind(SyntaxKind.RecordStructDeclaration)))
                    {
                        throw new ArgumentException("No record", nameof(typeDeclaration));
                    }

                    var recordDeclaration = (RecordDeclarationSyntax)typeDeclaration;
                    var parameterList = recordDeclaration.ParameterList ?? throw new ArgumentException("No record", nameof(typeDeclaration));
                    var newParameters = parameterList.Parameters.RemoveKeepTrivia(parameter);

                    var newParameterList = parameterList.WithParameters(newParameters);
                    var newRecordDeclaration = recordDeclaration.WithParameterList(newParameterList);

                    return document.WithSyntaxRoot(root.ReplaceNode(typeDeclaration, newRecordDeclaration));

                case SyntaxKind.PropertyDeclaration:

                    var property = (PropertyDeclarationSyntax)typeSyntax ?? throw new ArgumentException("No property", nameof(typeDeclaration));
                    var members = typeDeclaration.Members;

                    var newMembers = members.RemoveKeepTrivia(property);
                    var newTypeDeclaration = typeDeclaration.WithMembers(newMembers);

                    return document.WithSyntaxRoot(root.ReplaceNode(typeDeclaration, newTypeDeclaration));
            };

            throw new ArgumentException("No type with parameters or members", nameof(typeDeclaration));
        }

        public static Document RemoveAttributeArgument(this Document document, SyntaxNode root, AttributeArgumentListSyntax argumentListDeclaration, AttributeArgumentSyntax argumentSyntax)
        {
            var newArguments = argumentListDeclaration.Arguments.Remove(argumentSyntax);

            if (newArguments.Any())
            {
                var newArgumentListDeclaration = argumentListDeclaration.WithArguments(newArguments);
                return document.WithSyntaxRoot(root.ReplaceNode(argumentListDeclaration, newArgumentListDeclaration));
            }

            var attribute = argumentListDeclaration.FirstAncestorOrSelf<AttributeSyntax>();

            if (attribute is null)
            {
                throw new ArgumentNullException(nameof(attribute), nameof(attribute));
            }

            var name = ((IdentifierNameSyntax)attribute.Name).Identifier.ValueText;

            if (name != "Index")
            {
                var attributeList = attribute.FirstAncestorOrSelf<AttributeListSyntax>();
                if (attributeList is null)
                {
                    throw new ArgumentNullException(nameof(attributeList), nameof(attributeList));
                }

                var newAttributes = attributeList.Attributes.RemoveKeepTrivia(attribute);

                if (newAttributes.Any())
                {
                    var newAttributeList = attributeList.WithAttributes(newAttributes);
                    return document.WithSyntaxRoot(root.ReplaceNode(attributeList, newAttributeList));
                }

                var typeDeclaration = argumentListDeclaration.FirstAncestorOrSelf<TypeDeclarationSyntax>();

                if (typeDeclaration is null)
                {
                    throw new ArgumentNullException(nameof(typeDeclaration), nameof(typeDeclaration));
                }

                var attributeLists = typeDeclaration.AttributeLists;
                var newAttributeLists = attributeLists.RemoveKeepTrivia(attributeList);

                var newTypeDeclaration = typeDeclaration.WithAttributeLists(newAttributeLists);

                if (!newAttributeLists.Any())
                {
                    newTypeDeclaration = newTypeDeclaration.WithAttributeLists(newAttributeLists).WithLeadingTrivia(attributeList.GetLeadingTrivia());
                }

                return document.WithSyntaxRoot(root.ReplaceNode(typeDeclaration, newTypeDeclaration));
            }

            var newAttribute = attribute.WithArgumentList(null);

            return document.WithSyntaxRoot(root.ReplaceNode(attribute, newAttribute));
        }

        public static Document AddAttributeArgument(this Document document, SyntaxNode root, AttributeArgumentListSyntax argumentListDeclaration, string newArgument)
        {
            var arguments = argumentListDeclaration.Arguments.ToFullString();
            arguments = arguments.Any() ? newArgument + ", " + arguments : newArgument;

            arguments = $"({arguments})";
            var argumentList = SyntaxFactory.ParseAttributeArgumentList(arguments);

            if (argumentList is null)
            {
                throw new ArgumentNullException(nameof(argumentList), nameof(argumentList));
            }
            var newArgumentListDeclaration = argumentListDeclaration.WithArguments(argumentList.Arguments);

            return document.WithSyntaxRoot(root.ReplaceNode(argumentListDeclaration, newArgumentListDeclaration));
        }

        public static Document RenameAttributeArgument(this Document document, SyntaxNode root, AttributeArgumentSyntax argument, string name)
        {
            var nameExpression = argument.DescendantNodesAndSelf().OfType<LiteralExpressionSyntax>().Single();
            var newNameToken = SyntaxFactory.Literal(name).WithTriviaFrom(nameExpression.Token);
            var newNameExpression = nameExpression.WithToken(newNameToken);
            var newArgument = argument.ReplaceNode(nameExpression, newNameExpression);

            return document.WithSyntaxRoot(root.ReplaceNode(argument, newArgument));
        }

        public static Document RenameMember(this Document document, SyntaxNode root, SyntaxNode syntaxNode, string name)
        {
            SyntaxNode? newSyntaxNode = null;
            SyntaxToken? newNameToken;

            switch (syntaxNode.Kind())
            {
                case SyntaxKind.Parameter:
                    var parameter = (ParameterSyntax)syntaxNode ?? throw new ArgumentNullException(syntaxNode?.ToString());
                    newNameToken = SyntaxFactory.Identifier(name).WithTriviaFrom(parameter.Identifier);
                    newSyntaxNode = parameter.WithIdentifier((SyntaxToken)newNameToken);
                    break;
                case SyntaxKind.PropertyDeclaration:
                    var property = (PropertyDeclarationSyntax)syntaxNode ?? throw new ArgumentNullException(syntaxNode?.ToString());
                    newNameToken = SyntaxFactory.Identifier(name).WithTriviaFrom(property.Identifier);
                    newSyntaxNode = property.WithIdentifier((SyntaxToken)newNameToken);
                    break;
            }

            if (newSyntaxNode is not null)
            {
                return document.WithSyntaxRoot(root.ReplaceNode(syntaxNode, newSyntaxNode));
            }

            return document;
        }

        public static Document MakeNullable(this Document document, SyntaxNode root, TypeSyntax typeSyntax)
        {
            var nullableType = SyntaxFactory.NullableType(typeSyntax.WithoutTrivia()).WithTriviaFrom(typeSyntax);

            return document.WithSyntaxRoot(root.ReplaceNode(
                        typeSyntax, nullableType));
        }

        public static Document MakeUShort(this Document document, SyntaxNode root, SyntaxNode syntaxNode)
        {
            var syntaxToken = SyntaxFactory.Token(SyntaxKind.UShortKeyword);
            return MakeNullableType(document, root, syntaxToken, syntaxNode);
        }

        public static Document MakeUInt(this Document document, SyntaxNode root, SyntaxNode syntaxNode)
        {
            var syntaxToken = SyntaxFactory.Token(SyntaxKind.UIntKeyword);
            return MakeNullableType(document, root, syntaxToken, syntaxNode);
        }

        public static Document MakeULong(this Document document, SyntaxNode root, SyntaxNode syntaxNode)
        {
            var syntaxToken = SyntaxFactory.Token(SyntaxKind.ULongKeyword);
            return MakeNullableType(document, root, syntaxToken, syntaxNode);
        }

        public static Document MakeString(this Document document, SyntaxNode root, SyntaxNode syntaxNode)
        {
            var syntaxToken = SyntaxFactory.Token(SyntaxKind.StringKeyword);
            return MakeNullableType(document, root, syntaxToken, syntaxNode);
        }

        public static Document MakeNullableType(this Document document, SyntaxNode root, SyntaxToken syntaxToken, SyntaxNode syntaxNode)
        {
            SyntaxNode? newNode = null;
            var type = SyntaxFactory.PredefinedType(syntaxToken);
            var nullableType = SyntaxFactory.NullableType(type.WithoutTrivia());

            switch (syntaxNode.Kind())
            {
                case SyntaxKind.Parameter:
                    var parameter = (ParameterSyntax)syntaxNode;
                    if (parameter.Type is not null)
                    {
                        nullableType = nullableType.WithTriviaFrom(parameter.Type);
                    }
                    newNode = parameter.WithType(nullableType);
                    break;
                case SyntaxKind.PropertyDeclaration:
                    var property = (PropertyDeclarationSyntax)syntaxNode;
                    if (property.Type is not null)
                    {
                        nullableType = nullableType.WithTriviaFrom(property.Type);
                    }
                    newNode = property.WithType(nullableType);
                    break;
            };

            if (newNode is null)
            {
                throw new ArgumentNullException(syntaxNode.ToString());
            }

            return document.WithSyntaxRoot(root.ReplaceNode(
                        syntaxNode, newNode));
        }

        public static Document ReplaceType(this Document document, SyntaxNode root, SyntaxNode syntaxNode, string newType)
        {
            var type = SyntaxFactory.ParseTypeName(newType) ?? throw new ArgumentNullException(newType.ToString());
            SyntaxNode? newNode = null;

            switch (syntaxNode.Kind())
            {
                case SyntaxKind.Parameter:
                    var parameter = (ParameterSyntax)syntaxNode;
                    if (parameter.Type is not null)
                    {
                        type = type.WithTriviaFrom(parameter.Type);
                    }
                    newNode = parameter.WithType(type);
                    break;
                case SyntaxKind.PropertyDeclaration:
                    var property = (PropertyDeclarationSyntax)syntaxNode;
                    if (property.Type is not null)
                    {
                        type = type.WithTriviaFrom(property.Type);
                    }
                    newNode = property.WithType(type);
                    break;
            };

            if (newNode is null)
            {
                throw new ArgumentNullException(syntaxNode.ToString());
            }

            return document.WithSyntaxRoot(root.ReplaceNode(
                        syntaxNode, newNode));
        }

        public static Document ChangeIndexConverter(this Document document, SyntaxNode root, IdentifierNameSyntax identifierNameSyntax, string indexConverter)
        {
            var newIdentifierToken = SyntaxFactory.Identifier(indexConverter);
            var newIdentifierNameSyntax = identifierNameSyntax.ReplaceToken(
                        identifierNameSyntax.Identifier, newIdentifierToken);

            return document.WithSyntaxRoot(root.ReplaceNode(
                        identifierNameSyntax, newIdentifierNameSyntax));
        }

        private static SyntaxList<TNode> RemoveKeepTrivia<TNode>(this SyntaxList<TNode> list, TNode node) where TNode : SyntaxNode
        {
            if (node is null)
            {
                return list;
            }

            var firstNode = (node?.Equals(list.First())).True();
            var lastNode = (node?.Equals(list.Last())).True();

            var newList = list.Remove(node!);

            if (firstNode)
            {
                var newFirst = newList.FirstOrDefault()?.WithLeadingTrivia(node!.GetLeadingTrivia());
                if (newFirst is not null)
                {
                    newList = newList.Replace(newList.First(), newFirst);
                }
            }

            if (lastNode)
            {
                var newLast = newList.LastOrDefault()?.WithTrailingTrivia(node!.GetTrailingTrivia());
                if (newLast is not null)
                {
                    newList = newList.Replace(newList.Last(), newLast);
                }
            }

            return newList;
        }

        private static SeparatedSyntaxList<TNode> RemoveKeepTrivia<TNode>(this SeparatedSyntaxList<TNode> list, TNode node) where TNode : SyntaxNode
        {
            if (node is null)
            {
                return list;
            }

            var firstNode = (node?.Equals(list.First())).True();
            var lastNode = (node?.Equals(list.Last())).True();

            var newList = list.Remove(node!);

            if (firstNode)
            {
                var newFirst = newList.FirstOrDefault()?.WithLeadingTrivia(node!.GetLeadingTrivia());
                if (newFirst is not null)
                {
                    newList = newList.Replace(newList.First(), newFirst);
                }
            }

            if (lastNode)
            {
                var newLast = newList.LastOrDefault()?.WithTrailingTrivia(node!.GetTrailingTrivia());
                if (newLast is not null)
                {
                    newList = newList.Replace(newList.Last(), newLast);
                }
            }

            return newList;
        }
    }
}
