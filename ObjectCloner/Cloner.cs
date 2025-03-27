using System.Reflection.Emit;
using System.Reflection;


namespace ObjectCloner
{

    public class Cloner
    {
        public static T Clone<T>(T instance)
        {
            var type = DynamicConstructorAssembler.Create(typeof(T));
            var dyn_inst = Activator.CreateInstance(type.BuildedType, instance);
            return (T)dyn_inst;
        }
    }


    internal class DynamicConstructorAssembler
    {

        int MAX_LEVELS = 4;

        public static DynamicConstructorInfo Create(Type baseType)
        {
            int level = 0;

            DynamicConstructorInfo dtype = null;
            if (TypeBuilders.TryGetValue(baseType, out dtype))
            {
                if (dtype.BuildedType != null)
                {
                    return dtype;
                }
            }
            else
            {
                dtype = DefineConstructor(baseType);
            }

            ILGenerator il = dtype.Constructor.GetILGenerator();

            EmitObject(il, baseType, level);

            dtype.BuildedType = dtype.Builder.CreateType();
            return dtype;
        }

        private static void EmitObject(ILGenerator il, Type baseType, int level)
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


        private static DynamicConstructorInfo EmitObjectAssignor(ILGenerator il, PropertyInfo property)
        {
            var baset = property.PropertyType;

            DynamicConstructorInfo b1 = null;
            if (!TypeBuilders.TryGetValue(baset, out b1))
            {
                b1 = DefineConstructor(baset);
            }

            Label notNullLabel = il.DefineLabel();
            // if (obj != null)
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Brtrue_S, notNullLabel);
            // return;
            il.Emit(OpCodes.Ret);
            il.MarkLabel(notNullLabel);

            var temp = il.DeclareLocal(typeof(bool));

            // Verifica se obj.Parent != null
            il.Emit(OpCodes.Ldarg_1); // Carrega "obj"
            MethodInfo getParent = property.GetGetMethod();
            il.Emit(OpCodes.Callvirt, getParent); // Chama get_Parent
            il.Emit(OpCodes.Ldnull); // Carrega null
            il.Emit(OpCodes.Cgt_Un); // Compara (1 se não null, 0 se null)
            il.Emit(OpCodes.Stloc, temp); // Armazena o resultado em temp

            Label skipLabel = il.DefineLabel();
            il.Emit(OpCodes.Ldloc, temp); // Carrega o resultado
            il.Emit(OpCodes.Brfalse_S, skipLabel); // Pula se false (obj.Parent == null)

            // Corpo do if: this.Parent = new ObjetoTeste_Dynamic(obj.Parent)
            il.Emit(OpCodes.Ldarg_0); // Carrega "this"
            il.Emit(OpCodes.Ldarg_1); // Carrega "obj"
            il.Emit(OpCodes.Callvirt, getParent); // Chama get_Parent

            // Emite a criação da nova instância
            il.Emit(OpCodes.Newobj, b1.Constructor);

            // Chama o setter de Parent
            MethodInfo setParent = property.GetSetMethod();
            il.Emit(OpCodes.Callvirt, setParent);
            il.MarkLabel(skipLabel);
           // il.Emit(OpCodes.Ret);
            return b1;
        }

        private static void EmitProperty(ILGenerator il, PropertyInfo property)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Callvirt, property.GetGetMethod());
            il.Emit(OpCodes.Callvirt, property.GetSetMethod());
        }

        private static void EmitArrayCopy(ILGenerator il, PropertyInfo source_property)
        {
            // Obtém os métodos get/set da propriedade Array.
            MethodInfo getArray = source_property.GetGetMethod();
            MethodInfo setArray = source_property.GetSetMethod();

            // O tipo do array (por exemplo, ObjetoTesteFilho[])
            Type arrayType = source_property.PropertyType;
            // Tipo do elemento do array (por exemplo, ObjetoTesteFilho)
            Type itemType = arrayType.GetElementType();

            // Declara variáveis locais:
            // local0: condição (int) para o teste se o array é válido (1 = sim, 0 = não)
            LocalBuilder condition = il.DeclareLocal(typeof(int));   // local index 0
                                                                     // local1: índice do loop (int)
            LocalBuilder index = il.DeclareLocal(typeof(int));         // local index 1
                                                                       // local2: condição do loop (int) (resultado de clt)
            LocalBuilder loopCond = il.DeclareLocal(typeof(int));      // local index 2
                                                                       // local3: novo array criado (arrayType)
            LocalBuilder newArray = il.DeclareLocal(arrayType);        // local index 3

            // Define rótulos de controle:
            Label labelIfFalse = il.DefineLabel();    // Se o array de origem for nulo ou com Length==0
            Label labelAfterTest = il.DefineLabel();  // Após o teste
            Label labelLoopStart = il.DefineLabel();  // Início do loop
            Label labelLoopCheck = il.DefineLabel();  // Verificação do loop
            Label labelExit = il.DefineLabel();       // Saída do método

            // --- Teste: if (obj.Array != null && obj.Array.Length > 0) ---
            // Carrega obj.Array e testa se é nulo.
            il.Emit(OpCodes.Ldarg_1);                      // Carrega obj
            il.EmitCall(OpCodes.Callvirt, getArray, null); // Chama obj.get_Array()
            il.Emit(OpCodes.Brfalse_S, labelIfFalse);      // Se nulo, pula

            // Se não for nulo, carrega obj.Array novamente e obtém seu comprimento.
            il.Emit(OpCodes.Ldarg_1);                      // Carrega obj
            il.EmitCall(OpCodes.Callvirt, getArray, null); // Chama obj.get_Array() novamente
            il.Emit(OpCodes.Ldlen);                        // Obtém o comprimento do array
            il.Emit(OpCodes.Conv_I4);                      // Converte para int32
            il.Emit(OpCodes.Ldc_I4_0);                     // Carrega 0
            il.Emit(OpCodes.Cgt_Un);                       // Compara: se (length > 0) empurra 1, senão 0
            il.Emit(OpCodes.Br_S, labelAfterTest);         // Pula para labelAfterTest

            // Se obj.Array era nulo, define 0.
            il.MarkLabel(labelIfFalse);
            il.Emit(OpCodes.Ldc_I4_0);

            // Após o teste, armazena o resultado na variável "condition".
            il.MarkLabel(labelAfterTest);
            il.Emit(OpCodes.Stloc, condition);

            // Se o teste for falso (0), sai do método.
            il.Emit(OpCodes.Ldloc, condition);
            il.Emit(OpCodes.Brfalse_S, labelExit);

            // --- Cria o novo array: Array = new ObjetoTesteFilho[obj.Array.Length]; ---
            il.Emit(OpCodes.Ldarg_1);                      // Carrega obj
            il.EmitCall(OpCodes.Callvirt, getArray, null); // Chama obj.get_Array()
            il.Emit(OpCodes.Ldlen);                        // Obtém o comprimento
            il.Emit(OpCodes.Conv_I4);                      // Converte para int32
            il.Emit(OpCodes.Newarr, itemType);             // Cria um novo array do mesmo tamanho
            il.Emit(OpCodes.Stloc, newArray);              // Armazena em newArray

            // Atribui o novo array à propriedade: this.Array = newArray
            il.Emit(OpCodes.Ldarg_0);                      // Carrega this
            il.Emit(OpCodes.Ldloc, newArray);              // Carrega newArray
            il.EmitCall(OpCodes.Call, setArray, null);     // Chama set_Array(newArray)

            // --- Inicializa o índice do loop: int ix = 0; ---
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, index);

            // --- Loop: while (ix < obj.Array.Length) ---
            il.Emit(OpCodes.Br_S, labelLoopCheck);

            il.MarkLabel(labelLoopStart);
            // Corpo do loop:
            // newArray[ix] = new ObjetoTesteFilho(obj.Array[ix]);
            il.Emit(OpCodes.Ldloc, newArray);               // Carrega newArray
            il.Emit(OpCodes.Ldloc, index);                  // Carrega ix

            // Carrega o elemento: obj.Array[ix]
            il.Emit(OpCodes.Ldarg_1);                       // Carrega obj
            il.EmitCall(OpCodes.Callvirt, getArray, null);  // Chama obj.get_Array()
            il.Emit(OpCodes.Ldloc, index);                  // Carrega ix
            il.Emit(OpCodes.Ldelem_Ref);                    // Obtém o elemento (referência)

            // Cria uma nova instância: new ObjetoTesteFilho(obj.Array[ix])
            DynamicConstructorInfo b1 = null;
            if (!TypeBuilders.TryGetValue(itemType, out b1))
            {
                b1 = Create(itemType);
            }
            il.Emit(OpCodes.Newobj, b1.Constructor);
            // Armazena o resultado em newArray[ix]
            il.Emit(OpCodes.Stelem_Ref);

            // Incrementa ix: ix++
            il.Emit(OpCodes.Ldloc, index);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, index);

            // Verifica a condição do loop: if (ix < obj.Array.Length)
            il.MarkLabel(labelLoopCheck);
            il.Emit(OpCodes.Ldloc, index);                  // Carrega ix
            il.Emit(OpCodes.Ldarg_1);                       // Carrega obj
            il.EmitCall(OpCodes.Callvirt, getArray, null);  // Chama obj.get_Array()
            il.Emit(OpCodes.Ldlen);                         // Obtém o comprimento
            il.Emit(OpCodes.Conv_I4);                       // Converte para int32
            il.Emit(OpCodes.Clt);                           // Compara: (ix < length) ? 1 : 0
            il.Emit(OpCodes.Stloc, loopCond);               // Armazena o resultado em loopCond
            il.Emit(OpCodes.Ldloc, loopCond);               // Carrega loopCond
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
            il.Emit(OpCodes.Ldarg_1);                                      // Carrega obj
            il.EmitCall(OpCodes.Callvirt, get_child, null);                // chama obj.Children
            il.Emit(OpCodes.Ldnull);                                       // Carrega null
            il.Emit(OpCodes.Cgt_Un);                                       // Compara: empurra 1 se != null, 0 se for null
            il.Emit(OpCodes.Stloc, localHasChildren);                      // Armazena o resultado

            Label continueLabel = il.DefineLabel();
            il.Emit(OpCodes.Ldloc, localHasChildren);                      // Carrega o resultado da comparação
            il.Emit(OpCodes.Brtrue_S, continueLabel);                      // Se não nulo, continua
            il.Emit(OpCodes.Ret);                                          // Se nulo, retorna

            // Marca o label para continuação quando não nulo
            il.MarkLabel(continueLabel);

            // Children = new List<ObjetoTesteFilho>();
            il.Emit(OpCodes.Ldarg_0);                                      // Carrega this
            il.Emit(OpCodes.Newobj, list_ctor);                            // Cria new List<ObjetoTesteFilho>()
            il.EmitCall(OpCodes.Call, set_child, null);                    // Chama set_Children na instância atual

            // Obtém o enumerador: List<ObjetoTesteFilho>.Enumerator enumerator = obj.Children.GetEnumerator();
            LocalBuilder localEnumerator = il.DeclareLocal(enum_type);
            il.Emit(OpCodes.Ldarg_1);                                      // Carrega obj
            il.EmitCall(OpCodes.Callvirt, get_child, null);                // Chama get_Children
            il.EmitCall(OpCodes.Callvirt, get_enum, null);                 // Chama GetEnumerator()
            il.Emit(OpCodes.Stloc, localEnumerator);                       // Armazena o enumerador

            // Prepara a estrutura try/finally
            Label exitLabel = il.DefineLabel(); // Novo label para saída do bloco try/finally
            il.BeginExceptionBlock();

            // Loop: while (enumerator.MoveNext())
            Label loopStart = il.DefineLabel();
            Label loopCheck = il.DefineLabel();
            il.Emit(OpCodes.Br_S, loopCheck);                              // Pula para o teste do loop

            il.MarkLabel(loopStart);
            // ObjetoTesteFilho a = enumerator.Current;
            LocalBuilder localChild = il.DeclareLocal(item_type);
            il.Emit(OpCodes.Ldloca_S, localEnumerator);                    // Carrega o endereço do enumerador
            il.EmitCall(OpCodes.Call, enum_current, null);                 // Chama get_Current
            il.Emit(OpCodes.Stloc, localChild);                            // Armazena em localChild

            // Children.Add(new ObjetoTesteFilho(a));
            il.Emit(OpCodes.Ldarg_0);                                      // Carrega this
            il.EmitCall(OpCodes.Call, get_child, null);                    // Chama get_Children para acessar a lista
            il.Emit(OpCodes.Ldloc, localChild);                            // Carrega a variável a



            DynamicConstructorInfo b1 = null;
            if (!TypeBuilders.TryGetValue(item_type, out b1))
            {
                b1 = Create(item_type);
            }
            il.Emit(OpCodes.Newobj, b1.Constructor);                       // Cria new ObjetoTesteFilho(a)

            il.EmitCall(OpCodes.Callvirt, list_add, null);                 // Chama Add no List<ObjetoTesteFilho>

            // Testa a condição do loop: if (enumerator.MoveNext()) continue;
            il.MarkLabel(loopCheck);
            il.Emit(OpCodes.Ldloca_S, localEnumerator);                    // Carrega o endereço do enumerador
            il.EmitCall(OpCodes.Call, enum_move, null);                    // Chama MoveNext()
            il.Emit(OpCodes.Brtrue_S, loopStart);                          // Se true, volta ao início do loop

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

            // Marca o label de saída e finaliza o método
            il.MarkLabel(exitLabel);
        }


        internal static DynamicConstructorInfo DefineConstructor(Type baseType)
        {
            if (TypeBuilders.TryGetValue(baseType, out DynamicConstructorInfo tt)) return tt;

            var builder = Module.DefineType(baseType.Name + "_Dynamic", TypeAttributes.Public, baseType);

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
