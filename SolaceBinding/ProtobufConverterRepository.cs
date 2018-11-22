using Microsoft.CSharp;
using ProtoBuf;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Solace.Channels
{
    internal static class ProtobufConverterRepository
    {
        public static IProtobufConverter Create(
            IEnumerable<RequestParameter> parameters,
            Type returnType,
            IReadOnlyList<IValueConverter> converters)
        {
            try
            {
                string name = CreateRandomConverterName();
                var types = parameters.Select(p => p.Type).Concat(new[] { returnType });
                var compilerParameters = new CompilerParameters
                {
                    GenerateInMemory = true,
                    TreatWarningsAsErrors = true,
                    WarningLevel = 4,
                    ReferencedAssemblies =
                    {
                        "System.dll",
                        typeof(ProtoContractAttribute).Assembly.Location,
                        Assembly.GetExecutingAssembly().Location
                    }
                };

                compilerParameters.ReferencedAssemblies.AddRange((from t in types
                                                                  from type in GetReferencedTypes(t)
                                                                  let assembly = type.Assembly
                                                                  let assemblyName = assembly.GetName().Name
                                                                  where assemblyName != "mscorlib" && assemblyName != "System"
                                                                  select assembly.Location)
                                                                  .Distinct().ToArray());

                var compilerResults = new CSharpCodeProvider()
                    .CompileAssemblyFromDom(
                        compilerParameters,
                        ProtobufConverterGenerator.Create("DynamicClasses", name, name + "Converter",
                        parameters, returnType, converters));

                if (compilerResults.Errors.Count > 0)
                    throw new ApplicationException(
                        $"Error generating protobuf type converter for {name}: " +
                            string.Join(Environment.NewLine,
                            compilerResults.Errors.OfType<CompilerError>()
                                .Select(error => error.ErrorText)));

                return (IProtobufConverter)Activator.CreateInstance(
                    compilerResults.CompiledAssembly.GetType($"DynamicClasses.{name}Converter"), converters);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"Error generating protobuf type converter.", ex);
            }
        }

        private static string CreateRandomConverterName()
        {
            var r = new Random();

            return new string(Enumerable.Range(0, 10)
                .Select(i => (char)('a' + r.Next(25)))
                .ToArray());
        }

        private static IEnumerable<Type> GetReferencedTypes(Type t)
        {
            if (t.IsGenericType)
                foreach (var type in from argument in t.GetGenericArguments()
                                     from type in GetReferencedTypes(argument)
                                     select type)
                    yield return type;

            yield return t;
        }
    }
}
