
namespace ObjectCloner
{
    public class Cloner
    {
        public static T Clone<T>(T instance)
        {
            var type = DynamicConstructorAssembler.Create(typeof(T));

            var all_instances = new Dictionary<int, object>();

            var ff = type.BuildedType.GetConstructors()[0].GetParameters();

            var dyn_inst = Activator.CreateInstance(type.BuildedType, instance, type, all_instances);
            return (T)dyn_inst;
        }


        public static object CreateInstance(object source, DynamicConstructorInfo info, Dictionary<int, object> instances)
        {
            if (source == null) return null;

            int HID = source.GetHashCode();
            if (instances.TryGetValue(HID, out var exist))
            {
                return exist;
            }
            else
            {
                instances.Add(HID, source);
                var inst = Activator.CreateInstance(info.BuildedType, source, info, instances);
                return inst;
            }
        }

    }
}
