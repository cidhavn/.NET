using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Sample
{
    public static class ObjectValidationExtensions
    {
        /// <summary>
        /// 驗證 ValidationAttribute，僅處理第一層的資料驗證
        /// <para>Sample:</para>
        /// <para>
        /// bool isValidate = obj.Validate();
        /// </para>
        /// </summary>
        public static bool Validate<T>(this T obj) where T : class, new()
        {
            return Validate(obj, null);
        }

        /// <summary>
        /// 驗證 ValidationAttribute，僅處理第一層的資料驗證
        /// <para>Sample:</para>
        /// <para>
        /// bool isValidate = obj.Validate(data, errorMsg => { throw new ArgumentException(errorMsg); });
        /// </para>
        /// </summary>
        /// <param name="obj">要驗證的 Model</param>
        /// <param name="onError">驗證失敗時執行的動作</param>
        public static bool Validate<T>(this T obj, Action<string> onError) where T : class, new()
        {
            bool validateResult = true;

            #region 處理 ViewModel MetadataTypeAttribute

            bool hasMetadata = false;
            Dictionary<string, ValidationAttribute> metadataValidAttr = new Dictionary<string, ValidationAttribute>();

            MetadataTypeAttribute metadataAttr = obj.GetType()
                                                      .GetCustomAttributes(typeof(MetadataTypeAttribute), true)
                                                      .Cast<MetadataTypeAttribute>()
                                                      .FirstOrDefault();

            if (metadataAttr != null)
            {
                hasMetadata = true;

                foreach (PropertyInfo prop in metadataAttr.MetadataClassType.GetProperties())
                {
                    object[] attrs = prop.GetCustomAttributes(true);

                    foreach (var attr in attrs)
                    {
                        var validateAttr = attr as ValidationAttribute;

                        if (validateAttr != null)
                        {
                            metadataValidAttr.Add(prop.Name, validateAttr);
                        }
                    }
                }
            }

            #endregion 處理 ViewModel MetadataTypeAttribute

            foreach (PropertyInfo prop in obj.GetType().GetProperties())
            {
                object[] attrs = prop.GetCustomAttributes(true);
                object value = prop.GetValue(obj, null);

                foreach (var attr in attrs)
                {
                    var validateAttr = attr as ValidationAttribute;

                    if (validateAttr == null
                        && hasMetadata
                        && metadataValidAttr.ContainsKey(prop.Name))
                    {
                        validateAttr = metadataValidAttr[prop.Name];
                    }

                    if (validateAttr != null && validateAttr.IsValid(value) == false)
                    {
                        validateResult = false;

                        onError?.Invoke(validateAttr.ErrorMessage);
                    }
                }
            }

            return validateResult;
        }
    }
}
