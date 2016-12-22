using ProtoBuf;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Solace.Channels
{
    static class ProtobufConverterGenerator
    {
        const string convertersFieldName = "converters";
        const string objParameterName = "obj";
        const string parametersName = "parameters";
        const string castedObjName = "castedObj";

        public static CodeCompileUnit Create(
            string _namespace, string contractName, string converterName,
            IEnumerable<RequestParameter> parameters,
            Type returnType,
            IReadOnlyList<IValueConverter> converters)
        {
            try
            {
                return new CodeCompileUnit
                {
                    Namespaces =
                    {
                        new CodeNamespace(_namespace)
                        {
                            Imports = { new CodeNamespaceImport("System") },
                            Types =
                            {
                                GenerateContractClass(contractName, parameters, converters),
                                GenerateConverterClass(converterName, contractName, parameters, returnType, converters)
                            }
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"Error generating protobuf type converter for {converterName}.", ex);
            }
        }

        static CodeTypeDeclaration GenerateContractClass(string name, IEnumerable<RequestParameter> parameters,
            IReadOnlyList<IValueConverter> converters)
        {
            var contract = new CodeTypeDeclaration(name)
            {
                TypeAttributes = TypeAttributes.Public,
                CustomAttributes =
                {
                    new CodeAttributeDeclaration(
                        new CodeTypeReference(typeof(ProtoContractAttribute)))
                }
            };

            contract.Members.AddRange((from parameter in parameters
                                      where !parameter.IsFromProperty
                                      select GenerateProperty(parameter,
                                        converters.SingleOrDefault(converter =>
                                            converter.CanConvert(parameter.Type))
                                                ?.ConvertedType))
                .ToArray());

            return contract;
        }

        static CodeTypeDeclaration GenerateConverterClass(string name, string contractName,
            IEnumerable<RequestParameter> parameters,
            Type returnType, IReadOnlyList<IValueConverter> converters)
        {
            return new CodeTypeDeclaration(name)
            {
                TypeAttributes = TypeAttributes.Public,
                BaseTypes =
                {
                    returnType == typeof(void)
                    ? new CodeTypeReference(typeof(ProtobufConverterBase<>))
                    {
                        TypeArguments = { new CodeTypeReference(contractName) }
                    }
                    : new CodeTypeReference(typeof(ProtobufConverterBase<,>))
                    {
                        TypeArguments =
                        {
                            new CodeTypeReference(contractName),
                            new CodeTypeReference(returnType)
                        }
                    }
                },
                Members =
                {
                    new CodeMemberField(
                        new CodeTypeReference(typeof(IReadOnlyList<IValueConverter>)),
                        convertersFieldName)
                    {
                        Attributes = MemberAttributes.Private
                    },
                    new CodeConstructor
                    {
                        Attributes = MemberAttributes.Public,
                        Parameters =
                        {
                            new CodeParameterDeclarationExpression(
                                new CodeTypeReference(typeof(IReadOnlyList<IValueConverter>)),
                                convertersFieldName)
                        },
                        Statements =
                        {
                            new CodeAssignStatement(
                                new CodeFieldReferenceExpression(
                                    new CodeThisReferenceExpression(),
                                    convertersFieldName),
                                new CodeVariableReferenceExpression(convertersFieldName))
                        }
                    },
                    GenerateToObjectMethod(contractName, parameters, converters),
                    GenerateFromObjectMethod(contractName, parameters, converters)
                }
            };
        }

        static CodeTypeMember GenerateProperty(RequestParameter parameter, Type convertedType)
        {
            var field = new CodeMemberField
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Name = parameter.Name,
                Type = new CodeTypeReference(convertedType ?? parameter.Type),
                CustomAttributes =
                {
                    new CodeAttributeDeclaration(
                        new CodeTypeReference(typeof(ProtoMemberAttribute)),
                        new CodeAttributeArgument(
                            new CodePrimitiveExpression(parameter.Index + 1)))
                }
            };

            field.Name += " { get; set; }//";

            return field;
        }

        static CodeTypeMember GenerateToObjectMethod(string type, IEnumerable<RequestParameter> parameters,
            IReadOnlyList<IValueConverter> converters)
        {
            var method = new CodeMemberMethod
            {
                Name = "ToObject",
                Attributes = MemberAttributes.Family | MemberAttributes.Override,
                ReturnType = new CodeTypeReference(typeof(object)),
                Parameters =
                {
                    new CodeParameterDeclarationExpression(
                        new CodeTypeReference(typeof(object[])), parametersName),
                }
            };

            method.Statements.Add(new CodeVariableDeclarationStatement(
                new CodeTypeReference(type),
                objParameterName,
                new CodeObjectCreateExpression(new CodeTypeReference(type))));

            method.Statements.AddRange((from parameter in parameters
                                        where !parameter.IsFromProperty
                                        select GenerateConvertStatement(parameter,
                                            converters.SingleOrDefault(converter =>
                                                converter.CanConvert(parameter.Type))
                                                    ?.ConvertedType,
                                            converters.Select((converter, index) =>
                                                converter.CanConvert(parameter.Type) ? (int?)index : null)
                                                .SingleOrDefault(i => i.HasValue)))
                                            .ToArray<CodeStatement>());

            method.Statements.Add(new CodeMethodReturnStatement(
                new CodeVariableReferenceExpression(objParameterName)));

            return method;
        }

        private static CodeAssignStatement GenerateConvertStatement(RequestParameter parameter,
            Type convertedType,
            int? converterIndex)
        {
            var parametersReference = new CodeVariableReferenceExpression(parametersName);
            var objReference = new CodeVariableReferenceExpression(objParameterName);
            var parameterIndex = new CodeArrayIndexerExpression(
                        parametersReference,
                        new CodePrimitiveExpression(parameter.Index));

            return new CodeAssignStatement(
                new CodePropertyReferenceExpression(
                    objReference, parameter.Name),
                new CodeCastExpression(
                    new CodeTypeReference(convertedType ?? parameter.Type),
                        converterIndex.HasValue
                        ? (CodeExpression)new CodeMethodInvokeExpression(
                            new CodeArrayIndexerExpression(
                                new CodeFieldReferenceExpression(
                                    new CodeThisReferenceExpression(), convertersFieldName),
                                new CodePrimitiveExpression(converterIndex)),
                            "ConvertBack", parameterIndex)
                        : parameterIndex));
        }

        static CodeTypeMember GenerateFromObjectMethod(string type, IEnumerable<RequestParameter> parameters,
            IReadOnlyList<IValueConverter> converters)
        {

            var method = new CodeMemberMethod
            {
                Name = "FromObject",
                Attributes = MemberAttributes.Family | MemberAttributes.Override,
                Parameters =
                {
                    new CodeParameterDeclarationExpression(
                        new CodeTypeReference(typeof(object)), objParameterName),
                    new CodeParameterDeclarationExpression(
                        new CodeTypeReference(typeof(object[])), parametersName)
                }
            };

            method.Statements.Add(new CodeVariableDeclarationStatement(
                new CodeTypeReference(type),
                castedObjName,
                new CodeCastExpression(
                    new CodeTypeReference(type),
                    new CodeVariableReferenceExpression(objParameterName))));

            method.Statements.AddRange((from parameter in parameters
                                        where !parameter.IsFromProperty
                                        select GenerateConvertBackStatement(parameter,
                                            converters.Select((converter, index) =>
                                                converter.CanConvert(parameter.Type) ? (int?)index : null)
                                                .SingleOrDefault(i => i.HasValue)))
                .ToArray<CodeStatement>());

            return method;
        }

        static CodeAssignStatement GenerateConvertBackStatement(RequestParameter parameter, int? converterIndex)
        {
            var parametersReference = new CodeVariableReferenceExpression(parametersName);
            var castedOObjReference = new CodeVariableReferenceExpression(castedObjName);

            CodeExpression propertyReference = new CodePropertyReferenceExpression(
                                    castedOObjReference, parameter.Name);
            return new CodeAssignStatement(
                                new CodeArrayIndexerExpression(
                                    parametersReference,
                                    new CodePrimitiveExpression(parameter.Index)),
                                converterIndex.HasValue
                                ? new CodeMethodInvokeExpression(
                                    new CodeArrayIndexerExpression(
                                        new CodeFieldReferenceExpression(
                                            new CodeThisReferenceExpression(), convertersFieldName),
                                        new CodePrimitiveExpression(converterIndex)),
                                    "Convert", propertyReference)
                                : propertyReference);
        }
    }
}
