using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scopes.C;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MixinSourceGenerator
{
    [Microsoft.CodeAnalysis.Generator]
    public class MixinSourceGenerator : ISourceGenerator
    {
        private static readonly SymbolDisplayFormat SymbolDisplayFormat = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters
                );

        public void Initialize(GeneratorInitializationContext context)
        {

        }

        public void Execute(GeneratorExecutionContext context)
        {
            var allSymbols = context.Compilation.GetSymbolsWithName(_ => true);
            var typeSymbols = allSymbols.OfType<INamedTypeSymbol>();

            var mixinData = new List<MixinData>();
            foreach (var typeSymbol in typeSymbols)
            {
                var mixAttributes = 
                    typeSymbol.GetAttributes().
                    Where(__ => __.AttributeClass.ConstructedFrom.ToString() == "MixAttribute").
                    Select(__ => __.ConstructorArguments[0].Value as INamedTypeSymbol).
                    Where(__ => __ != null).
                    Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default);

                if (mixAttributes.Any() &&
                    ((TypeDeclarationSyntax)typeSymbol.DeclaringSyntaxReferences.First().GetSyntax())
                        .Modifiers.Any(__ => __.IsKind(SyntaxKind.PartialKeyword)))
                {
                    mixinData.Add(new MixinData
                    {
                        TargetType = typeSymbol,
                        MixinTypes = mixAttributes
                    });
                }
            }

            var result = new Scopes.Group() {
            "#pragma warning disable CS0109 // Member does not hide an inherited member; new keyword is not required"

            };

            foreach (var mixinRecord in mixinData)
            {
                foreach (var mixinType in mixinRecord.MixinTypes)
                {
                    result.Add(Mix(mixinRecord.TargetType, mixinType));
                }
            }

            context.AddSource("Generated.cs", result.ToString());
        }


        public void GetAllPublicMembers(INamedTypeSymbol type, Dictionary<string, ISymbol> result)
        {
            if (type.BaseType != null)
            {
                var name = type.BaseType.ToDisplayString(SymbolDisplayFormat);
                if (name != typeof(Object).FullName)
                {
                    GetAllPublicMembers(type.BaseType, result);
                }

            }


            foreach (var member in type.GetMembers()
                .Where(x => !x.IsStatic)
                .Where(x => x.DeclaredAccessibility == Accessibility.Public)
                )
            {
                var name = member.Name;
                if (member is IMethodSymbol methodSymbol)
                {//TODO: Test me

                    var genericParametersCount = methodSymbol.TypeParameters.Length;
                    var parametersTypes = string.Join(",", methodSymbol.Parameters.Select(x => x.Type.ToDisplayString(SymbolDisplayFormat)));
                    name = $"{name}<{genericParametersCount}>({parametersTypes})";
                }
                result[name] = member;
            }
        }


        public Scope Mix(INamedTypeSymbol aggregateType, INamedTypeSymbol mixinType/*, Dictionary<string, INamedTypeSymbol> mixinSpecializationMap*/)
        {



            var keyword = aggregateType.TypeKind.ToString().ToLower();

            //TODO: think more about private variable naming
            var mixinVariableName = mixinType.ToDisplayString(SymbolDisplayFormat).Replace('.', '_').Replace('<', '_').Replace('>', '_');




            var result = new Scope($"partial {keyword} {aggregateType.Name}") {
                $"private {mixinType.ToDisplayString(SymbolDisplayFormat)} {mixinVariableName} = new {mixinType.ToDisplayString(SymbolDisplayFormat)}();"
            };


            //var publicInstanceMembers = mixinType.GetMembers()
            //    .Where(x => !x.IsStatic)
            //    .Where(x => x.DeclaredAccessibility == Accessibility.Public).ToArray();



            Scopes.Group SetAggrigator(object inner)
            {
                return new Scopes.Group() {
                    "var previousAggregator = Aggregator.Current;",
                    new Scope("try"){
                        "Aggregator.Current = this;",
                        inner
                    },
                    new Scope("finally"){
                        "Aggregator.Current = previousAggregator;"
                    }
                };
            }

            var members = new Dictionary<string, ISymbol>();
            GetAllPublicMembers(mixinType, members);




            foreach (var member in members.Values)
            {
                var name = member.Name;
                foreach (var a in member.GetAttributes())
                {
                    result.Add($"[{a.ToString()}]");
                }


                if (member is IFieldSymbol fieldSymbol)
                {
                    result.Add(
                        new Scope($"new public {fieldSymbol.Type.ToDisplayString(SymbolDisplayFormat)} {name}") {
                            new Scope("get"){
                                $"return {mixinVariableName}.{name};"
                            },
                            new Scope("set"){
                                $"{mixinVariableName}.{name} = value;"
                            }
                        }
                    );
                    continue;
                }

                if (member is IPropertySymbol propertySymbol)
                {
                    bool get = (propertySymbol.GetMethod != null) && (propertySymbol.GetMethod.DeclaredAccessibility == Accessibility.Public);
                    bool set = (propertySymbol.SetMethod != null) && (propertySymbol.SetMethod.DeclaredAccessibility == Accessibility.Public);
                    Scope property;
                    if (propertySymbol.IsIndexer)
                    {
                        var parametersDeclaration = string.Join(",", propertySymbol.Parameters.Select(x => $"{x.Type.ToDisplayString(SymbolDisplayFormat)} {x.Name}"));
                        var parametersUsage = string.Join(",", propertySymbol.Parameters.Select(x => x.Name));
                        property = new Scope($"new public {propertySymbol.Type.ToDisplayString(SymbolDisplayFormat)} this[{parametersDeclaration}]");

                        if (get)
                            property.Add(new Scope("get") {
                            SetAggrigator($"return {mixinVariableName}[{parametersUsage}];")
                        });
                        if (set)
                            property.Add(new Scope("set") {
                            SetAggrigator($"{mixinVariableName}[{parametersUsage}] = value;")
                        });

                    }
                    else
                    {
                        property = new Scope($"new public {propertySymbol.Type.ToDisplayString(SymbolDisplayFormat)} {name}");
                        if (get)
                            property.Add(new Scope("get") {
                            SetAggrigator($"return {mixinVariableName}.{name};")
                        });

                        if (set)
                            property.Add(new Scope("set") {
                            SetAggrigator($"{mixinVariableName}.{name} = value;")
                        });
                    }


                    result.Add(property);



                    continue;
                }

                if (member is IMethodSymbol methodSymbol)
                {
                    if (methodSymbol.MethodKind == MethodKind.Ordinary)
                    {
                        var parametersCall = new List<string>();
                        var parametersDeclaration = new List<string>();

                        var genericFragment = methodSymbol.TypeParameters.Any()
                            ? $"<{string.Join(",", methodSymbol.TypeParameters.Select(x => x.Name))}>"
                            : "";


                        var constraintClauses = new List<string>();

                        foreach (var t in methodSymbol.OriginalDefinition.DeclaringSyntaxReferences)
                        {
                            var syntax = t.GetSyntax();
                            if (syntax is MethodDeclarationSyntax methodDeclarationSyntax)
                            {
                                foreach (var c in methodDeclarationSyntax.ConstraintClauses)
                                {
                                    var normalized = c.NormalizeWhitespace();
                                    var text = normalized.ToString();
                                    constraintClauses.Add(" " + text);
                                }
                            }
                        }
                        var constraintClausesFragment = string.Concat(constraintClauses);


                        foreach (var p in methodSymbol.Parameters)
                        {
                            var refKinds = p.RefKind.ToParameterPrefix();
                            parametersCall.Add(refKinds + p.Name);
                            parametersDeclaration.Add(refKinds + p.Type.ToDisplayString(SymbolDisplayFormat) + " " + p.Name);
                        }
                        var callParemeterList = string.Join(", ", parametersDeclaration);

                        var resultTypeString = methodSymbol.ReturnsVoid ? "void" : methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat);

                        result.Add(new Scope($"new public {resultTypeString} {methodSymbol.Name}{genericFragment}({callParemeterList}){constraintClausesFragment}") {
                            SetAggrigator((methodSymbol.ReturnsVoid?"":"return ")+$"{mixinVariableName}.{methodSymbol.Name}{genericFragment}({string.Join(",", parametersCall)});")
                        });

                    }
                    continue;
                }


            }


            var namespaceName = aggregateType.ContainingNamespace.ToDisplayString(SymbolDisplayFormat);

            if (!string.IsNullOrEmpty(namespaceName))
            {
                result = new Scope($"namespace {namespaceName}") {
                    result
                };
            }



            return result;

            /*

            foreach (var member in mixinTypeInfo.GetMembers()) {

                if (!member.IsPrivate() && !member.IsStatic()) {

                    if (member is FieldDeclarationSyntax fieldDeclarationSyntax) {
                        var declaration = fieldDeclarationSyntax.Declaration;

                        //Console.WriteLine(declaration.Type.GetType().FullName);

                        var fieldTypeInfo = AssemblyInfo.FindType(declaration.Type);

                        foreach (var variable in declaration.Variables) {
                            result.Add($"{member.GetVisibility()} {variable.Identifier.ValueText} ");

                        }
                        continue;
                    }



                }

            }



            var namespacePath = aggregateTypeInfo.Parent.PathString;
            if (!string.IsNullOrEmpty(namespacePath)) {

                result = new Scope($"namespace {aggregateTypeInfo.Parent.PathString}") {
                    result
                };
            }
                
                
            return result;*/
        }
    }
}