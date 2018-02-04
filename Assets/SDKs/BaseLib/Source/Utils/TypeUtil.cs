using System;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;

using BaseLib;

public class TypeUtil 
{
    static public T EnumFromString<T>(string value)
    {
        return (T)System.Enum.Parse(typeof(T), value);
    }

    static public object CreateFromString(string typeName, Type[] arrParamTypes = null, object[] parameters = null)
    {
        System.Type type = Assembly.GetCallingAssembly().GetType(typeName);
        ConstructorInfo constructorInfo = type.GetConstructor(arrParamTypes == null ? new Type[] { } : arrParamTypes);

        return constructorInfo.Invoke(parameters == null? new object[] { } : parameters);
    }

    static public T CreateFromString<T>(string typeName, Type[] arrParamTypes = null, object[] arrParams = null) where T : class
    {
        System.Type type = Assembly.GetCallingAssembly().GetType(typeName);
        ConstructorInfo constructorInfo = type.GetConstructor(arrParamTypes == null ? new Type[] { } : arrParamTypes);

        return constructorInfo.Invoke(arrParams == null ? new object[] { } : arrParams) as T;
    }

    /// Note: currently not support 'property' member.
    public static T DictionaryToObject<T>(IDictionary<string, object> source)
        where T : class, new()
    {
        var someObject = DictionaryToObject(typeof(T), source);
        return someObject as T;
    }

    /// Note: currently not support 'property' member.
    public static object DictionaryToObject(Type type, IDictionary<string, object> source)
    {
        var newObject = Activator.CreateInstance(type);

        foreach (KeyValuePair<string, object> item in source)
        {
            FieldInfo fieldInfo = type.GetField(item.Key);
            Type valueType = item.Value.GetType();

            if(fieldInfo == null)
            {
                PropertyInfo propertyInfo = type.GetProperty(item.Key);
                Debugger.Assert(propertyInfo != null);

                if (valueType == typeof(Dictionary<string, object>))
                {
                    var obj = DictionaryToObject(propertyInfo.PropertyType, item.Value as Dictionary<string, object>);
                    propertyInfo.SetValue(newObject, obj, null);
                }
                else if (valueType.IsArray)
                {
                    var obj = __arrayToObject(propertyInfo.PropertyType, item.Value as Array);
                    propertyInfo.SetValue(newObject, obj, null);
                }
                else
                {
                    Type desiredType = propertyInfo.PropertyType;
                    if (desiredType.IsEnum)
                    {
                        propertyInfo.SetValue(newObject, Enum.Parse(propertyInfo.PropertyType, item.Value.ToString()), null);
                    }
                    else
                    {
                        var fieldValue = Convert.ChangeType(item.Value, desiredType);
                        propertyInfo.SetValue(newObject, fieldValue, null);
                    }
                }
            }
            else
            {
                if (valueType == typeof(Dictionary<string, object>))
                {
                    var obj = DictionaryToObject(fieldInfo.FieldType, item.Value as Dictionary<string, object>);
                    fieldInfo.SetValue(newObject, obj);
                }
                else if (valueType.IsArray)
                {
                    var obj = __arrayToObject(fieldInfo.FieldType, item.Value as Array);
                    fieldInfo.SetValue(newObject, obj);
                }
                else
                {
                    Type desiredType = fieldInfo.FieldType;
                    if (desiredType.IsEnum)
                    {
                        fieldInfo.SetValue(newObject, Enum.Parse(fieldInfo.FieldType, item.Value.ToString()));
                    }
                    else
                    {
                        var fieldValue = Convert.ChangeType(item.Value, desiredType);
                        fieldInfo.SetValue(newObject, fieldValue);
                    }
                }
            }
        }
        return newObject;
    }

#region Implementation
    private static object __arrayToObject(Type type, Array source)
    {
        object obj;
        if (type.IsArray)
        {
            obj = __arrayToObjArray(type, source);
        }
        else
        {
            obj = __arrayToObjList(type, source);
        }
        return obj;
    }

    private static object __arrayToObjList(Type type, Array source)
    {
        var elementType = type.GetGenericArguments()[0];

        var l = Activator.CreateInstance(type); 
        var lists = (l as IList);

        foreach (var item in source)
        {
            Type itemType = item.GetType();

            if (itemType == typeof(Dictionary<string, object>))
            {
                var obj = DictionaryToObject(elementType, item as Dictionary<string, object>);
                lists.Add(obj);
            }
            else if (itemType.IsArray)
            {
                var obj = __arrayToObject(elementType, item as Array);
                lists.Add(obj);
            }
            else
            {
                lists.Add(item);
            }
        }
        return lists;
    }

    private static object __arrayToObjArray(Type type, Array source)
    {
        var elementType = type.GetElementType();
        var lists = Array.CreateInstance(elementType, source.Length);

        var i = 0;
        foreach (var item in source)
        {
            Type itemType = item.GetType();

            if (itemType == typeof(Dictionary<string, object>))
            {
                var obj = DictionaryToObject(elementType, item as Dictionary<string, object>);
                lists.SetValue(obj, i);
            }
            else if (itemType.IsArray)
            {
                var obj = __arrayToObject(elementType, item as Array);
                lists.SetValue(obj, i);
            }
            else
            {
                lists.SetValue(item, i);
            }

            i++;
        }

        return lists;
    }

#endregion

}
