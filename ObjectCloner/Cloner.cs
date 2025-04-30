
namespace ObjectCloner
{
    public class Cloner
    {
        public static T Clone<T>(T instance)
        {
            var type          = DynamicConstructorAssembler.Create(typeof(T));
            var all_instances = new Dictionary<int, object>();
            var dync          = type.BuildedType.GetConstructors()[0];
            var dyn_inst      = (T)dync.Invoke(new object[] { instance, all_instances });

            return (T)dyn_inst;
        }

    }
}
