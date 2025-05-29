using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SelfMovingCart.Patches
{
    public static class ReflectionHelper
    {
        /// <summary>
        /// Invokes a private method on an object instance
        /// </summary>
        /// <param name="instance">The object instance to invoke the method on</param>
        /// <param name="methodName">Name of the private method</param>
        /// <param name="parameters">Parameters to pass to the method (optional)</param>
        /// <returns>The return value of the method, or null if void</returns>
        public static object InvokePrivateMethod(object instance, string methodName, params object[] parameters)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            Type type = instance.GetType();
            MethodInfo methodInfo = AccessTools.Method(type, methodName);

            if (methodInfo == null)
                throw new MissingMethodException($"Method '{methodName}' not found on type '{type.FullName}'");

            return methodInfo.Invoke(instance, parameters);
        }

        /// <summary>
        /// Invokes a private static method on a type
        /// </summary>
        /// <param name="type">The type containing the static method</param>
        /// <param name="methodName">Name of the private static method</param>
        /// <param name="parameters">Parameters to pass to the method (optional)</param>
        /// <returns>The return value of the method, or null if void</returns>
        public static object InvokePrivateStaticMethod(Type type, string methodName, params object[] parameters)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            MethodInfo methodInfo = AccessTools.Method(type, methodName);

            if (methodInfo == null)
                throw new MissingMethodException($"Static method '{methodName}' not found on type '{type.FullName}'");

            return methodInfo.Invoke(null, parameters);
        }

        /// <summary>
        /// Gets the value of a private field from an object instance
        /// </summary>
        /// <typeparam name="T">The expected return type</typeparam>
        /// <param name="instance">The object instance</param>
        /// <param name="fieldName">Name of the private field</param>
        /// <returns>The field value cast to type T</returns>
        public static T GetPrivateField<T>(object instance, string fieldName)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            Type type = instance.GetType();
            FieldInfo fieldInfo = AccessTools.Field(type, fieldName);

            if (fieldInfo == null)
                throw new MissingFieldException($"Field '{fieldName}' not found on type '{type.FullName}'");

            return (T)fieldInfo.GetValue(instance);
        }

        /// <summary>
        /// Gets the value of a private static field
        /// </summary>
        /// <typeparam name="T">The expected return type</typeparam>
        /// <param name="type">The type containing the static field</param>
        /// <param name="fieldName">Name of the private static field</param>
        /// <returns>The field value cast to type T</returns>
        public static T GetPrivateStaticField<T>(Type type, string fieldName)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            FieldInfo fieldInfo = AccessTools.Field(type, fieldName);

            if (fieldInfo == null)
                throw new MissingFieldException($"Static field '{fieldName}' not found on type '{type.FullName}'");

            return (T)fieldInfo.GetValue(null);
        }

        /// <summary>
        /// Sets the value of a private field on an object instance
        /// </summary>
        /// <param name="instance">The object instance</param>
        /// <param name="fieldName">Name of the private field</param>
        /// <param name="value">Value to set</param>
        public static void SetPrivateField(object instance, string fieldName, object value)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            Type type = instance.GetType();
            FieldInfo fieldInfo = AccessTools.Field(type, fieldName);

            if (fieldInfo == null)
                throw new MissingFieldException($"Field '{fieldName}' not found on type '{type.FullName}'");

            fieldInfo.SetValue(instance, value);
        }

        /// <summary>
        /// Sets the value of a private static field
        /// </summary>
        /// <param name="type">The type containing the static field</param>
        /// <param name="fieldName">Name of the private static field</param>
        /// <param name="value">Value to set</param>
        public static void SetPrivateStaticField(Type type, string fieldName, object value)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            FieldInfo fieldInfo = AccessTools.Field(type, fieldName);

            if (fieldInfo == null)
                throw new MissingFieldException($"Static field '{fieldName}' not found on type '{type.FullName}'");

            fieldInfo.SetValue(null, value);
        }

        /// <summary>
        /// Gets a private property value from an object instance
        /// </summary>
        /// <typeparam name="T">The expected return type</typeparam>
        /// <param name="instance">The object instance</param>
        /// <param name="propertyName">Name of the private property</param>
        /// <returns>The property value cast to type T</returns>
        public static T GetPrivateProperty<T>(object instance, string propertyName)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            Type type = instance.GetType();
            PropertyInfo propertyInfo = AccessTools.Property(type, propertyName);

            if (propertyInfo == null)
                throw new MissingMemberException($"Property '{propertyName}' not found on type '{type.FullName}'");

            return (T)propertyInfo.GetValue(instance);
        }

        /// <summary>
        /// Gets an enum value from a private enum type
        /// </summary>
        /// <param name="containingType">The type containing the enum</param>
        /// <param name="enumTypeName">Name of the enum type</param>
        /// <param name="enumValueName">Name of the enum value</param>
        /// <returns>The enum value as an object</returns>
        public static object GetPrivateEnumValue(Type containingType, string enumTypeName, string enumValueName)
        {
            Type enumType = AccessTools.Inner(containingType, enumTypeName);

            if (enumType == null)
                throw new MissingMemberException($"Enum type '{enumTypeName}' not found in '{containingType.FullName}'");

            return Enum.Parse(enumType, enumValueName);
        }
    }
}
