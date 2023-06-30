using System.Reflection;

namespace VectorSearchAiAssistant.Service.Utils
{
    public static class ObjectExtensions
    {
        public static List<string> GetPropertyValues(this object obj, List<string> propertyNames) 
        {
            var type = obj.GetType();

            // Only string properties are supported
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(string) && p.CanRead && propertyNames.Contains(p.Name))
                .Select(p => p.GetGetMethod(false))
                .Where(mget => mget != null)
                .Select(mget => (string)mget.Invoke(obj, null))
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }
    }
}
