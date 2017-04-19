﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Murtain.Web.Attributes;
using Murtain.SDK.Attributes;

namespace Murtain.Web.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="Enum"/>.
    /// </summary>
    public static class EnumExtensions
    {
        public static HttpStatusCode TryHttpStatusCode(this Enum enumValue)
        {
            string str = enumValue.ToString();
            FieldInfo field = enumValue.GetType().GetField(str);
            object[] objs = field.GetCustomAttributes(typeof(HttpCorrespondingAttribute), false);
            if (objs == null || objs.Length == 0)
                return HttpStatusCode.InternalServerError;
            HttpCorrespondingAttribute httpStatus = (HttpCorrespondingAttribute)objs[0];
            return httpStatus.HttpStatusCode;
        }
    }
}
