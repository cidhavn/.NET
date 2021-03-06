using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace Sample
{
    public static class ObjectCloneExtensions
    {
        /// <summary>
        /// 淺複製 (Shallow clone)
        /// </summary>
        public static T Clone<T>(this T obj) where T : class, new()
        {
            var inst = obj.GetType().GetMethod("MemberwiseClone",
                                               BindingFlags.Instance | BindingFlags.NonPublic);

            return (T)inst?.Invoke(obj, null);
        }
        
        /// <summary>
        /// 深複製
        /// </summary>
        public static T DeepClone<T>(this T obj) where T : class, new()
        {
            return DeepCloneByReflection(obj);
        }

        #region Serialization Deep Clone

        /// <summary>
        /// Serialization 深複製物件，必需設定 [Serializable]，效能較 Reflection 差
        /// </summary>
        private static T DeepCloneBySerialization<T>(T item) where T : class, new()
        {
            if (Object.ReferenceEquals(item, null))
            {
                return default(T);
            }

            if (!typeof(T).IsSerializable)
            {
                throw new ArgumentException("The type must be serializable.", "source");
            }

            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, item);

                stream.Seek(0, SeekOrigin.Begin);

                return (T)formatter.Deserialize(stream); ;
            }
        }

        #endregion Serialization Deep Clone

        #region Reflection Deep Clone

        /// <summary>
        /// Reflection 深複製物件，只處理 Property
        /// </summary>
        private static T DeepCloneByReflection<T>(T cloneObj) where T : class, new()
        {
            if (Object.ReferenceEquals(cloneObj, null))
            {
                return default(T);
            }

            var itemType = cloneObj.GetType();

            #region List & Dictionary

            if (itemType.IsGenericType)
            {
                if (typeof(IDictionary).IsAssignableFrom(itemType))
                {
                    var dic = cloneObj as IDictionary;
                    var newDic = Activator.CreateInstance(itemType) as IDictionary;

                    if (dic != null && dic.Count > 0)
                    {
                        var firstValue = GetDictionaryFirstValue(dic);
                        Type valueType = firstValue.GetType();

                        if (valueType.IsPrimitive || valueType.IsValueType || valueType == typeof(string))
                        {
                            foreach (var key in dic.Keys)
                            {
                                newDic.Add(key, dic[key]);
                            }
                        }
                        else
                        {
                            foreach (var key in dic.Keys)
                            {
                                newDic.Add(key, DeepCloneByReflection(dic[key]));
                            }
                        }

                        return (T)newDic;
                    }
                }
                else
                {
                    var list = cloneObj as IList;
                    var newList = Activator.CreateInstance(itemType) as IList;

                    if (list != null && list.Count > 0)
                    {
                        var firstValue = list[0];
                        Type valueType = firstValue.GetType();

                        if (valueType.IsPrimitive || valueType.IsValueType || valueType == typeof(string))
                        {
                            foreach (var item in list)
                            {
                                newList.Add(item);
                            }
                        }
                        else
                        {
                            foreach (var item in list)
                            {
                                newList.Add(DeepCloneByReflection(item));
                            }
                        }

                        return (T)newList;
                    }
                }

                return default(T);
            }

            #endregion List & Dictionary

            #region Array

            if (itemType.IsArray)
            {
                var arr = cloneObj as Array;

                if (arr != null)
                {
                    return (T)arr.Clone();
                }

                return null;
            }

            #endregion Array

            #region Class

            // 處理 Property(屬性)，GetProperties() 只抓 Public 的屬性
            // 只處理可寫入屬性，因為只允許讀取的屬性設定，應是關聯某個可寫入屬性
            var properties = itemType.GetProperties().Where(x => x.CanWrite == true).ToList();

            var newObj = Activator.CreateInstance(itemType);
            var newObjType = newObj.GetType();

            foreach (var prop in properties)
            {
                var propType = prop.PropertyType;
                var value = prop.GetValue(cloneObj, null);
                var newPropInfo = newObjType.GetProperty(prop.Name);

                if (propType.IsPrimitive || propType.IsValueType || propType == typeof(string))
                {
                    newPropInfo.SetValue(newObj, Convert.ChangeType(value, prop.PropertyType));
                }
                else
                {
                    newPropInfo.SetValue(newObj, DeepCloneByReflection(value));
                }
            }

            return (T)newObj;

            #endregion Class
        }

        #endregion Reflection Deep Clone

        #region Expression Tree Deep Clone

        private static T DeepCloneByExpressionTree<T>(T cloneObj) where T : class, new()
        {
            if (Object.ReferenceEquals(cloneObj, null))
            {
                return default(T);
            }

            var func = ExpressionTreeClone(cloneObj.GetType());
            return (T)func(cloneObj);
        }

        private static Func<object, object> ExpressionTreeClone(Type cloneType)
        {
            List<Expression> bodyList = new List<Expression>();

            var constructor = cloneType.GetConstructor(Type.EmptyTypes);
            var properties = cloneType.GetProperties().Where(x => x.CanWrite == true).ToList();

            string variableKey = $"{Guid.NewGuid().ToString()}_";
            var input = Expression.Parameter(cloneType, $"{variableKey}model");
            var output = Expression.Variable(cloneType, $"{variableKey}newModel");

            // 建立新物件
            var newObj = Expression.New(constructor);
            bodyList.Add(Expression.Assign(output, Expression.New(constructor)));

            int propIndex = 0;
            foreach (var prop in properties)
            {
                var propType = prop.PropertyType;

                if (propType.IsPrimitive || propType == typeof(string))
                {
                    var inputProp = Expression.Field(input, prop.Name);
                    var outputProp = Expression.Field(output, prop.Name);

                    bodyList.Add(Expression.Assign(outputProp, inputProp));
                }

                //if (IsReferenceType(propType))
                //{
                //    if (propType.IsArray)
                //    {
                //        var varArr = Expression.Variable(propType, $"{variableKey}arr{propIndex}");
                //        var cloneMethod = Expression.Call(inputProp, typeof(Array).GetMethod("Clone"));
                //        var convertType = Expression.TypeAs(cloneMethod, propType);
                //        var condition = Expression.IfThen(
                //                            Expression.NotEqual(inputProp, Expression.Constant(null)),
                //                            Expression.Assign(outputProp, convertType));
                //        bodyList.Add(condition);
                //    }
                //    else if (propType.IsGenericType)
                //    {
                //        if (typeof(IDictionary).IsAssignableFrom(cloneType))
                //        {
                //            var varDic = Expression.Variable(propType, $"{variableKey}dic{propIndex}");
                //            var varDicFunc = Expression.Variable(propType, $"{variableKey}dicFunc{propIndex}");
                //        }
                //        else
                //        {
                //            var varList = Expression.Variable(propType, $"{variableKey}list{propIndex}");
                //            var varListFunc = Expression.Variable(propType, $"{variableKey}listFunc{propIndex}");
                //        }
                //    }
                //}
                //else
                //{
                //    var assign = Expression.Assign(outputProp, inputProp);
                //    bodyList.Add(assign);
                //}

                propIndex++;
            }

            var block = Expression.Block(bodyList);
            var body = Expression.Block(new[] { output }, block, output);
            var lambda = Expression.Lambda<Func<object, object>>(body, input);

            return lambda.Compile();
        }

        #endregion Expression Tree Deep Clone

        /// <summary>
        /// 取得 IDictionary 第1筆 Value
        /// </summary>
        private static object GetDictionaryFirstValue(IDictionary dic)
        {
            if (dic != null && dic.Count > 0)
            {
                foreach (var key in dic.Keys)
                {
                    return dic[key];
                }
            }

            return null;
        }
    }
}
