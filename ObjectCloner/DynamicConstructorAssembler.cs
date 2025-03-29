﻿using System.Reflection.Emit;
using System.Reflection;

namespace ObjectCloner
{
    internal class DynamicConstructorAssembler
    {

        internal static DynamicConstructorInfo Create(Type baseType)
        {
            PredefineAllTypes(baseType);
            GenerateAllIL();
            FinalizeAllTypes();

            var typeInfo = GetTypeInfo(baseType);

            return typeInfo;
        }


        private static void EmitObject(ILGenerator il, Type baseType)
        {
            Label notNullLabel = il.DefineLabel();
            // if (obj != null)
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Brtrue_S, notNullLabel);
            // return;
            il.Emit(OpCodes.Ret);
            il.MarkLabel(notNullLabel);

            var properties = baseType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in properties)
            {
                if (!property.CanWrite) continue;
                MethodInfo getMethod = property.GetGetMethod();
                MethodInfo setMethod = property.GetSetMethod();
                if (getMethod == null || setMethod == null) continue;
                Type propType = property.PropertyType;

                if (propType.IsClass && propType != typeof(string))
                {
                    if (propType.IsArray)
                    {
                        EmitArrayCopy(il, property);
                    }
                    else
                    {
                        string? namespaceName = propType.Namespace;
                        if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            EmitListCopy(il, property);
                        }
                        else if (namespaceName != null && !namespaceName.StartsWith("System") && !namespaceName.StartsWith("Microsoft"))
                        {
                            EmitObjectAssignor(il, property);
                        }
                    }
                }
                else
                {
                    EmitProperty(il, property);
                }
            }

            il.Emit(OpCodes.Ret);
        }

        private static void EmitObjectAssignor(ILGenerator il, PropertyInfo property)
        {
            MethodInfo getProperty = property.GetGetMethod();
            var baset = property.PropertyType;
            var b1 = TypeBuilders[baset];

            Label skipLabel = il.DefineLabel();
            // Verifica se obj.Property != null
            il.Emit(OpCodes.Ldarg_1);

            il.Emit(OpCodes.Callvirt, getProperty);
            il.Emit(OpCodes.Brfalse_S, skipLabel); // Pula se null

            // this.Property = new DynamicType(obj.Property)
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Callvirt, getProperty);
            il.Emit(OpCodes.Newobj, b1.Constructor);
            il.Emit(OpCodes.Callvirt, property.GetSetMethod());

            il.MarkLabel(skipLabel);
        }

        private static void EmitArrayCopy(ILGenerator il, PropertyInfo source_property)
        {
            // Obtém os métodos get/set da propriedade Array.
            MethodInfo getArray = source_property.GetGetMethod();
            MethodInfo setArray = source_property.GetSetMethod();

            Type arrayType = source_property.PropertyType;
            Type itemType = arrayType.GetElementType();

            // local0: condição (int) para o teste se o array é válido (1 = sim, 0 = não)
            LocalBuilder condition = il.DeclareLocal(typeof(int));
            LocalBuilder index = il.DeclareLocal(typeof(int));
            LocalBuilder loopCond = il.DeclareLocal(typeof(int));
            LocalBuilder newArray = il.DeclareLocal(arrayType);

            Label labelIfFalse = il.DefineLabel();
            Label labelAfterTest = il.DefineLabel();
            Label labelLoopStart = il.DefineLabel();
            Label labelLoopCheck = il.DefineLabel();
            Label labelExit = il.DefineLabel();

            // if (obj.Array != null && obj.Array.Length > 0)
            il.Emit(OpCodes.Ldarg_1);
            il.EmitCall(OpCodes.Callvirt, getArray, null);
            il.Emit(OpCodes.Brfalse_S, labelIfFalse);

            // Se não for nulo, carrega obj.Array novamente e obtém seu comprimento.
            il.Emit(OpCodes.Ldarg_1);
            il.EmitCall(OpCodes.Callvirt, getArray, null);
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Conv_I4);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Cgt_Un);
            il.Emit(OpCodes.Br_S, labelAfterTest);

            // Se obj.Array era nulo, define 0.
            il.MarkLabel(labelIfFalse);
            il.Emit(OpCodes.Ldc_I4_0);

            // Após o teste, armazena o resultado na variável "condition".
            il.MarkLabel(labelAfterTest);
            il.Emit(OpCodes.Stloc, condition);

            // Se o teste for falso (0), sai do método.
            il.Emit(OpCodes.Ldloc, condition);
            il.Emit(OpCodes.Brfalse_S, labelExit);

            // Array = new ObjetoTesteFilho[obj.Array.Length];
            il.Emit(OpCodes.Ldarg_1);
            il.EmitCall(OpCodes.Callvirt, getArray, null);
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Conv_I4);
            il.Emit(OpCodes.Newarr, itemType);
            il.Emit(OpCodes.Stloc, newArray);

            // this.Array = newArray
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, newArray);
            il.EmitCall(OpCodes.Call, setArray, null);

            // int ix = 0;
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, index);

            // while (ix < obj.Array.Length)
            il.Emit(OpCodes.Br_S, labelLoopCheck);

            il.MarkLabel(labelLoopStart);

            // newArray[ix] = new Objeto(obj.Array[ix]);
            il.Emit(OpCodes.Ldloc, newArray);
            il.Emit(OpCodes.Ldloc, index);

            // obj.Array[ix]
            il.Emit(OpCodes.Ldarg_1);
            il.EmitCall(OpCodes.Callvirt, getArray, null);
            il.Emit(OpCodes.Ldloc, index);
            il.Emit(OpCodes.Ldelem_Ref);

            // new Objeto(obj.Array[ix])
            var b1 = TypeBuilders[itemType];
            il.Emit(OpCodes.Newobj, b1.Constructor);
            il.Emit(OpCodes.Stelem_Ref);

            // ix++
            il.Emit(OpCodes.Ldloc, index);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, index);

            // if (ix < obj.Array.Length)
            il.MarkLabel(labelLoopCheck);
            il.Emit(OpCodes.Ldloc, index);
            il.Emit(OpCodes.Ldarg_1);
            il.EmitCall(OpCodes.Callvirt, getArray, null);  // obj.get_Array()
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Conv_I4);
            il.Emit(OpCodes.Clt);                           // (ix < length) ? 1 : 0
            il.Emit(OpCodes.Stloc, loopCond);
            il.Emit(OpCodes.Ldloc, loopCond);
            il.Emit(OpCodes.Brtrue_S, labelLoopStart);      // Se true, repete o loop

            il.MarkLabel(labelExit);
        }

        private static void EmitListCopy(ILGenerator il, PropertyInfo source_property)
        {
            var list_type = source_property.PropertyType;
            var list_ctor = list_type.GetConstructor(Type.EmptyTypes);
            var get_enum = list_type.GetMethod("GetEnumerator");
            var enum_type = get_enum.ReturnType;
            var enum_current = enum_type.GetProperty("Current").GetGetMethod();
            var enum_move = enum_type.GetMethod("MoveNext");
            var list_add = list_type.GetMethod("Add");

            var item_type = list_type.GetGenericArguments()[0];
            var get_child = source_property.GetGetMethod();
            var set_child = source_property.GetSetMethod();

            // Verifica se obj.Children é nulo
            LocalBuilder localHasChildren = il.DeclareLocal(typeof(bool));
            il.Emit(OpCodes.Ldarg_1);
            il.EmitCall(OpCodes.Callvirt, get_child, null);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Cgt_Un);
            il.Emit(OpCodes.Stloc, localHasChildren);

            Label continueLabel = il.DefineLabel();
            Label endLabel = il.DefineLabel();

            il.Emit(OpCodes.Ldloc, localHasChildren);
            il.Emit(OpCodes.Brtrue_S, continueLabel);
            il.Emit(OpCodes.Br_S, endLabel);

            // Marca o label para continuação quando não nulo
            il.MarkLabel(continueLabel);

            // Children = new List<ObjetoTesteFilho>();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Newobj, list_ctor);
            il.EmitCall(OpCodes.Call, set_child, null);

            // List<ObjetoTesteFilho>.Enumerator enumerator = obj.Children.GetEnumerator();
            LocalBuilder localEnumerator = il.DeclareLocal(enum_type);
            il.Emit(OpCodes.Ldarg_1);
            il.EmitCall(OpCodes.Callvirt, get_child, null);
            il.EmitCall(OpCodes.Callvirt, get_enum, null);
            il.Emit(OpCodes.Stloc, localEnumerator);

            // Prepara a estrutura try/finally
            Label exitLabel = il.DefineLabel();
            il.BeginExceptionBlock();

            // Loop: while (enumerator.MoveNext())
            Label loopStart = il.DefineLabel();
            Label loopCheck = il.DefineLabel();
            il.Emit(OpCodes.Br_S, loopCheck);

            il.MarkLabel(loopStart);
            // a = enumerator.Current;
            LocalBuilder localChild = il.DeclareLocal(item_type);
            il.Emit(OpCodes.Ldloca_S, localEnumerator);
            il.EmitCall(OpCodes.Call, enum_current, null);
            il.Emit(OpCodes.Stloc, localChild);

            // Children.Add(new ObjetoTesteFilho(a));
            il.Emit(OpCodes.Ldarg_0);
            il.EmitCall(OpCodes.Call, get_child, null);
            il.Emit(OpCodes.Ldloc, localChild);


            var b1 = TypeBuilders[item_type];
            il.Emit(OpCodes.Newobj, b1.Constructor);

            il.EmitCall(OpCodes.Callvirt, list_add, null);

            // Testa a condição do loop: if (enumerator.MoveNext()) continue;
            il.MarkLabel(loopCheck);
            il.Emit(OpCodes.Ldloca_S, localEnumerator);
            il.EmitCall(OpCodes.Call, enum_move, null);
            il.Emit(OpCodes.Brtrue_S, loopStart);

            // Sai do try block: direciona para exitLabel
            il.Emit(OpCodes.Leave_S, exitLabel);

            // Finally: ((IDisposable)enumerator).Dispose();
            il.BeginFinallyBlock();
            il.Emit(OpCodes.Ldloca_S, localEnumerator);
            il.Emit(OpCodes.Constrained, enum_type);
            MethodInfo disposeMethod = typeof(IDisposable).GetMethod("Dispose");
            il.EmitCall(OpCodes.Callvirt, disposeMethod, null);
            il.Emit(OpCodes.Nop);
            il.EndExceptionBlock();

            il.MarkLabel(exitLabel);
            il.MarkLabel(endLabel);
        }

        private static void EmitProperty(ILGenerator il, PropertyInfo property)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Callvirt, property.GetGetMethod());
            il.Emit(OpCodes.Callvirt, property.GetSetMethod());
        }

        private static DynamicConstructorInfo GetTypeInfo(Type baseType)
        {
            if (!TypeBuilders.TryGetValue(baseType, out var typeInfo))
            {
                throw new InvalidOperationException("Tipo não pré-definido: " + baseType.Name);
            }
            return typeInfo;
        }

        private static void FinalizeAllTypes()
        {
            foreach (var dtype in TypeBuilders.Values)
            {
                dtype.BuildedType = dtype.Builder.CreateType();
            }
        }

        private static void PredefineAllTypes(Type baseType)
        {
            if (!TypeBuilders.ContainsKey(baseType))
            {
                DefineConstructor(baseType);
                PredefineTypes(baseType);
            }
        }

        private static void PredefineTypes(Type baseType)
        {
            var properties = baseType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                Type propType = property.PropertyType;
                if (propType.IsClass && propType != typeof(string))
                {
                    if (propType.IsArray)
                    {
                        Type itemType = propType.GetElementType();
                        if (!TypeBuilders.ContainsKey(itemType))
                        {
                            DefineConstructor(itemType);
                            PredefineTypes(itemType);
                        }
                    }
                    else if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        Type itemType = propType.GetGenericArguments()[0];
                        if (!TypeBuilders.ContainsKey(itemType))
                        {
                            DefineConstructor(itemType);
                            PredefineTypes(itemType);
                        }
                    }
                    else if (!propType.Namespace.StartsWith("System") && !propType.Namespace.StartsWith("Microsoft"))
                    {
                        if (!TypeBuilders.ContainsKey(propType))
                        {
                            DefineConstructor(propType);
                            PredefineTypes(propType);
                        }
                    }
                }
            }
        }


        private static void GenerateAllIL()
        {
            foreach (var dtype in TypeBuilders.Values)
            {
                ILGenerator il = dtype.Constructor.GetILGenerator();
                EmitObject(il, dtype.OriginalType);
            }
        }

        private static DynamicConstructorInfo DefineConstructor(Type baseType)
        {
            if (TypeBuilders.TryGetValue(baseType, out DynamicConstructorInfo tt)) return tt;

            var builder = Module.DefineType(baseType.Name + "_Dynamic", TypeAttributes.Public | TypeAttributes.Class, baseType);

            var dci = new DynamicConstructorInfo()
            {
                Builder = builder,
                OriginalType = baseType,
                Constructor = builder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { baseType })
            };
            TypeBuilders.Add(baseType, dci);
            return dci;
        }


        private static Dictionary<Type, DynamicConstructorInfo> TypeBuilders;
        private static ModuleBuilder Module;

        static DynamicConstructorAssembler()
        {
            TypeBuilders = new();

            string assemblyName = "DynamicAssembly";
            AssemblyName an = new AssemblyName(assemblyName);
            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
            Module = ab.DefineDynamicModule(assemblyName);
        }
    }


    internal class DynamicConstructorInfo
    {
        internal Type OriginalType;
        internal TypeBuilder Builder;
        internal Type BuildedType;
        internal ConstructorBuilder Constructor;
    }
}
