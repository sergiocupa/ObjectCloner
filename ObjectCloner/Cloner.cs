
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
}
