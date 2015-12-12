using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Sql
{
    internal sealed class DataCache
    {
        private static volatile DataCache instance;
        private static object syncRoot = new object();

        public delegate void GenericSetter(object target, object value);
        private static Dictionary<PropertyInfo, GenericSetter> cachedSetters;

        private DataCache()
        {
            cachedSetters = new Dictionary<PropertyInfo, GenericSetter>();
        }

        /// <summary>
        /// The public facing instance of the singleton
        /// </summary>
        public static DataCache Cache
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new DataCache();
                        }
                    }
                }

                return instance;
            }
        }

        public GenericSetter GetSetter<T>(PropertyInfo property)
        {
            Type type = typeof(T);
            GenericSetter setter;

            if (cachedSetters.TryGetValue(property, out setter))
            {
                return setter;
            }
            else
            {
                var setterDelegate = CreateSetMethod(property);
                cachedSetters.Add(property, setterDelegate);
                return setterDelegate;
            }
        }

        private static GenericSetter CreateSetMethod(PropertyInfo propertyInfo)
        {
            MethodInfo setMethod = propertyInfo.GetSetMethod();
            if (setMethod == null)
                return null;

            Type[] arguments = new Type[2];
            arguments[0] = arguments[1] = typeof(object);

            DynamicMethod setter = new DynamicMethod(string.Concat("_Set", propertyInfo.Name, "_"), typeof(void), arguments, propertyInfo.DeclaringType);
            ILGenerator generator = setter.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
            generator.Emit(OpCodes.Ldarg_1);

            if (propertyInfo.PropertyType.IsClass)
                generator.Emit(OpCodes.Castclass, propertyInfo.PropertyType);
            else
                generator.Emit(OpCodes.Unbox_Any, propertyInfo.PropertyType);

            generator.EmitCall(OpCodes.Callvirt, setMethod, null);
            generator.Emit(OpCodes.Ret);

            return (GenericSetter)setter.CreateDelegate(typeof(GenericSetter));
        }
    }
}
