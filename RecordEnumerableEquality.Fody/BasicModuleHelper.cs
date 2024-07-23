//using System;
//using System.Diagnostics;
//using System.Linq;

//using Mono.Cecil;
//using Mono.Cecil.Cil;

//namespace RecordEnumerableEquality.Fody;

//public class BasicModuleHelper
//{
//    public static BasicModuleHelper Instance { get; } = new BasicModuleHelper();

//    public ModuleDefinition Module => _module.Value;
//    public TypeReference ComparerDefinitionReference => _comparerDefinitionReference.Value;

//    private readonly Lazy<ModuleDefinition> _module;
//    private readonly Lazy<TypeReference> _comparerDefinitionReference;

//    private BasicModuleHelper()
//    {
//        _module = new Lazy<ModuleDefinition>(() =>
//        {
//            try
//            {
//                var resolver = new DefaultAssemblyResolver();
//                resolver.AddSearchDirectory(typeof(object).Assembly.Location);
//                var readerParameters = new ReaderParameters { AssemblyResolver = resolver };
//                var markerType = typeof(EnumerableValueComparer<>);
//                var location = markerType.Assembly.Location;

//                return ModuleDefinition.ReadModule(location, readerParameters);
//            }
//            catch (Exception ex)
//            {
//                Debugger.Launch();
//                throw;
//            }
//        });

//        _comparerDefinitionReference = new Lazy<TypeReference>(() =>
//        {
//            return _module.Value.ImportReference(typeof(EnumerableValueComparer<>));
//            // return new GenericInstanceType(comparerTypeDef);
//        });
//    }

//    private BasicModuleHelper(ModuleDefinition module, TypeReference comparerDefinitionReference)
//    {
//        _module = new Lazy<ModuleDefinition>(() => module);
//        _comparerDefinitionReference = new Lazy<TypeReference>(() => comparerDefinitionReference);
//    }

//    public GenericInstanceType MakeComparerType(params TypeReference[] genericArguments)
//    {
//        //var defX = ComparerDefinitionReference;

//        //var ndef = new TypeDefinition
//        //(
//        //    "BasicFodyAddin",
//        //    "EnumerableValueComparer`1",
//        //    Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.BeforeFieldInit
//        //);
//        //ndef.Scope = defX.Scope;

//        var def = ComparerDefinitionReference;
//        var genericInstanceType = new GenericInstanceType(def);
//        foreach (var genericArgument in genericArguments)
//        {
//            genericInstanceType.GenericArguments.Add(genericArgument);
//        }

//        return genericInstanceType;
//    }

//    public TypeReference InjectType(ModuleDefinition module)
//    {
//        var existingType = module.Types.FirstOrDefault(t => t.FullName == ComparerDefinitionReference.FullName);
//        if (existingType != null)
//        {
//            return existingType;
//        }

//        // var comparerTypeDef = _module.Value.ImportReference(typeof(EnumerableValueComparer<>));
//        // var comparerTypeRef = ComparerDefinitionReference;
//        // var comparerTypeDef = comparerTypeRef.Resolve();
//        //
//        //
//        // var comparerType = new TypeDefinition
//        // (
//        //     comparerTypeRef.Namespace,
//        //     comparerTypeRef.Name,
//        //     comparerTypeDef.Attributes,
//        //     comparerTypeRef
//        // );
//        //
//        // comparerType.GenericParameters.Add(new GenericParameter("T", comparerType));
//        // comparerType.BaseType = comparerTypeRef;
//        //
//        // module.Types.Add(comparerType);
//        // // module.Assembly.Write();
//        //
//        // return comparerType;

//        var importer = new Importer(module);
//        var comparerType = importer.Import(ComparerDefinitionReference);
//        module.Types.Add(comparerType);
//        return comparerType;
//    }

//    // public BasicModuleHelper ForModule(ModuleDefinition module)
//    // {
//    //     var typeForModule = InjectType(module);
//    //     return new BasicModuleHelper(module, typeForModule);
//    // }
//}

//internal class Importer
//{
//    private readonly ModuleDefinition _module;

//    public Importer(ModuleDefinition module)
//    {
//        _module = module;
//    }

//    public TypeDefinition Import(TypeReference typeReference)
//    {
//        var type = typeReference.Resolve();

//        // Clone the type definition
//        var clonedType = new TypeDefinition(
//            type.Namespace,
//            type.Name,
//            type.Attributes,
//            _module.ImportReference(type.BaseType)
//        );

//        // Import fields
//        foreach (var field in type.Fields)
//        {
//            // If field type is the clone source type, use the cloned type
//            var fieldType = field.FieldType.FullName == typeReference.FullName
//                ? clonedType
//                : _module.ImportReference(field.FieldType);

//            var clonedField = new FieldDefinition(
//                field.Name,
//                field.Attributes,
//                fieldType
//            );
//            clonedType.Fields.Add(clonedField);
//        }

//        // Import methods
//        foreach (var method in type.Methods)
//        {
//            var clonedMethod = new MethodDefinition(
//                method.Name,
//                method.Attributes,
//                _module.ImportReference(method.ReturnType)
//            );

//            foreach (var parameter in method.Parameters)
//            {
//                clonedMethod.Parameters.Add(new ParameterDefinition(
//                    parameter.Name,
//                    parameter.Attributes,
//                    _module.ImportReference(parameter.ParameterType)
//                ));
//            }

//            if (method.HasBody)
//            {
//                clonedMethod.Body = new MethodBody(clonedMethod);
//                var ilProcessor = clonedMethod.Body.GetILProcessor();

//                foreach (var instruction in method.Body.Instructions)
//                {
//                    ilProcessor.Append(instruction);
//                }

//                foreach (var variable in method.Body.Variables)
//                {
//                    clonedMethod.Body.Variables.Add(new VariableDefinition(
//                        _module.ImportReference(variable.VariableType)
//                    ));
//                }
//            }

//            clonedType.Methods.Add(clonedMethod);
//        }

//        return clonedType;
//    }
//}