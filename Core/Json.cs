﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sheet
{
    #region IJsonSerializer

    public interface IJsonSerializer
    {
        string Serialize(object value);
        T Deerialize<T>(string value);
    }

    #endregion

    #region Newtonsoft IJsonSerializer

    public class NewtonsoftJsonSerializer : IJsonSerializer
    {
        public string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, Formatting.Indented);
        }

        public T Deerialize<T>(string value)
        {
            return JsonConvert.DeserializeObject<T>(value);
        }
    } 

    #endregion
}
