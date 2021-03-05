/*
 * Copyright © 2021 robby & EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 *
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */

using System;
using System.Linq;
using static BaseUtils.JSON.JToken;

namespace BaseUtils.JSON
{
    public static class JTokenExtensions
    {
        public static T ToObjectQ<T>(this JToken tk)            // quick version, with checkcustomattr off
        {
            return ToObject<T>(tk, false, false);
        }

        public static T ToObject<T>(this JToken tk, bool ignoretypeerrors = false, bool checkcustomattr = true)  // backwards compatible naming
        {
            Type tt = typeof(T);
            try
            {
                Object ret = tk.ToObject(tt, ignoretypeerrors, checkcustomattr);        // paranoia, since there are a lot of dynamics, trap any exceptions
                if (ret is ToObjectError)
                {
                    System.Diagnostics.Debug.WriteLine("To Object error:" + ((ToObjectError)ret).ErrorString + ":" + ((ToObjectError)ret).PropertyName);
                    return default(T);
                }
                else if (ret != null)      // or null
                    return (T)ret;          // must by definition have returned tt.
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception JSON ToObject " + ex.Message + " " + ex.StackTrace);
            }

            return default(T);
        }

        public class ToObjectError
        {
            public string ErrorString;
            public string PropertyName;
            public ToObjectError(string s) { ErrorString = s; PropertyName = ""; }
        };

        // returns Object of type tt, or ToObjectError, or null if tk == JNotPresent.
        // ignoreerrors means don't worry if individual fields are wrong type in json vs in classes/dictionaries
        // checkcustomattr check for custom attributes - this takes time so you may want to turn it off
        // will return an instance of tt or ToObjectError, or null for token is null
        // this may except in unusual circumstances (which i've not found yet, but there are dynamic type changes in there)

        public static Object ToObject(this JToken tk, Type tt, bool ignoretypeerrors, bool checkcustomattr)
        {
            if (tk == null)
            {
                return null;
            }
            else if (tk.IsArray)
            {
                JArray jarray = (JArray)tk;

                if (tt.IsArray)
                {
                    dynamic instance = Activator.CreateInstance(tt, tk.Count);   // dynamic holder for instance of array[]

                    for (int i = 0; i < tk.Count; i++)
                    {
                        Object ret = ToObject(tk[i], tt.GetElementType(), ignoretypeerrors, checkcustomattr);      // get the underlying element, must match array element type

                        if (ret != null && ret.GetType() == typeof(ToObjectError))      // arrays must be full, any errors means an error
                        {
                            ((ToObjectError)ret).PropertyName = tt.Name + "." + i.ToString() + "." + ((ToObjectError)ret).PropertyName;
                            return ret;
                        }
                        else
                        {
                            dynamic d = tt.GetElementType().ChangeTo(ret);
                            instance[i] = d;
                        }
                    }

                    return instance;
                }
                else if (typeof(System.Collections.IList).IsAssignableFrom(tt))
                {
                    dynamic instance = Activator.CreateInstance(tt);        // create the List
                    var types = tt.GetGenericArguments();

                    for (int i = 0; i < tk.Count; i++)
                    {
                        Object ret = ToObject(tk[i], types[0], ignoretypeerrors, checkcustomattr);      // get the underlying element, must match types[0] which is list type

                        if (ret != null && ret.GetType() == typeof(ToObjectError))  // lists must be full, any errors are errors
                        {
                            ((ToObjectError)ret).PropertyName = tt.Name + "." + i.ToString() + "." + ((ToObjectError)ret).PropertyName;
                            return ret;
                        }
                        else
                        {
                            dynamic d = types[0].ChangeTo(ret);
                            instance.Add(d);
                        }
                    }

                    return instance;
                }
                else
                    return new ToObjectError("JSONToObject: Not array");
            }
            else if (tk.TokenType == JToken.TType.Object)                   // objects are best efforts.. fills in as many fields as possible
            {
                if (typeof(System.Collections.IDictionary).IsAssignableFrom(tt))       // if its a Dictionary<x,y> then expect a set of objects
                {
                    dynamic instance = Activator.CreateInstance(tt);        // create the class, so class must has a constructor with no paras
                    var types = tt.GetGenericArguments();

                    foreach (var kvp in (JObject)tk)
                    {
                        Object ret = ToObject(kvp.Value, types[1], ignoretypeerrors, checkcustomattr);        // get the value as the dictionary type - it must match type or it get OE

                        if (ret != null && ret.GetType() == typeof(ToObjectError))
                        {
                            ((ToObjectError)ret).PropertyName = tt.Name + "." + kvp.Key + "." + ((ToObjectError)ret).PropertyName;

                            if (ignoretypeerrors)
                            {
                                System.Diagnostics.Debug.WriteLine("Ignoring Object error:" + ((ToObjectError)ret).ErrorString + ":" + ((ToObjectError)ret).PropertyName);
                            }
                            else
                            {
                                return ret;
                            }
                        }
                        else
                        {
                            dynamic d = types[1].ChangeTo(ret);
                            instance[kvp.Key] = d;
                        }
                    }

                    return instance;
                }
                else if (tt.IsClass ||      // if class
                         (tt.IsValueType && !tt.IsPrimitive && !tt.IsEnum && tt != typeof(DateTime)))   // or struct, but not datetime (handled below)
                {
                    var instance = Activator.CreateInstance(tt);        // create the class, so class must has a constructor with no paras

                    System.Reflection.MemberInfo[] fi = tt.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static |
                                                          System.Reflection.BindingFlags.Public);
                    string[] finames = null;

                    System.Reflection.MemberInfo[] pi = null;   // lazy load this
                    string[] pinames = null;

                    if (checkcustomattr)
                    {
                        finames = new string[fi.Length];
                        for (int i = 0; i < fi.Length; i++)
                        {
                            var rename = fi[i].GetCustomAttributes(typeof(JsonNameAttribute), false);
                            finames[i] = rename.Length == 1 ? (string)((dynamic)rename[0]).Name : fi[i].Name;
                        }
                    }

                    foreach (var kvp in (JObject)tk)
                    {
                        System.Reflection.MemberInfo mi = null;

                        var fipos = finames != null ? System.Array.IndexOf(finames, kvp.Key) : System.Array.FindIndex(fi, x => x.Name == kvp.Key);
                        if (fipos >= 0)     // try and find field first..
                        {
                            mi = fi[fipos];
                        }
                        else
                        {
                            if (pi == null)     // lazy load pick up, only load these if fields not found
                            {
                                pi = tt.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static |
                                                              System.Reflection.BindingFlags.Public);
                                if (checkcustomattr)
                                {
                                    pinames = new string[pi.Length];
                                    for (int i = 0; i < pi.Length; i++)
                                    {
                                        var rename = pi[i].GetCustomAttributes(typeof(JsonNameAttribute), false);
                                        pinames[i] = rename.Length == 1 ? (string)((dynamic)rename[0]).Name : pi[i].Name;
                                    }
                                }
                            }

                            var pipos = pinames != null ? System.Array.IndexOf(pinames, kvp.Key) : System.Array.FindIndex(pi, x => x.Name == kvp.Key);
                            if (pipos >= 0)
                                mi = pi[pipos];
                        }

                        if (mi != null)                                   // if we found a class member
                        {
                            var ca = checkcustomattr ? mi.GetCustomAttributes(typeof(JsonIgnoreAttribute), false) : null;

                            if (ca == null || ca.Length == 0)                                              // ignore any ones with JsonIgnore on it.
                            {
                                Type otype = mi.FieldPropertyType();

                                if (otype != null)                          // and its a field or property
                                {
                                    Object ret = ToObject(kvp.Value, otype, ignoretypeerrors, checkcustomattr);    // get the value - must match otype.. ret may be zero for ? types

                                    if (ret != null && ret.GetType() == typeof(ToObjectError))
                                    {
                                        ((ToObjectError)ret).PropertyName = tt.Name + "." + kvp.Key + "." + ((ToObjectError)ret).PropertyName;

                                        if (ignoretypeerrors)
                                        {
                                            System.Diagnostics.Debug.WriteLine("Ignoring Object error:" + ((ToObjectError)ret).ErrorString + ":" + ((ToObjectError)ret).PropertyName);
                                        }
                                        else
                                        {
                                            return ret;
                                        }
                                    }
                                    else
                                    {
                                        if (!mi.SetValue(instance, ret))         // and set. Set will fail if the property is get only
                                        {
                                            if (ignoretypeerrors)
                                            {
                                                System.Diagnostics.Debug.WriteLine("Ignoring cannot set value on property " + mi.Name);
                                            }
                                            else
                                            {
                                                return new ToObjectError("Cannot set value on property " + mi.Name);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("JSONToObject: No such member " + kvp.Key + " in " + tt.Name);
                        }
                    }

                    return instance;
                }
                else
                    return new ToObjectError("JSONToObject: Not class");
            }
            else
            {
                string name = tt.Name;                              // compare by name quicker than is

                if (name.Equals("Nullable`1"))                      // nullable types
                {
                    if (tk.IsNull)
                        return null;

                    name = tt.GenericTypeArguments[0].Name;         // get underlying type..
                }

                if (name.Equals("String"))                          // copies of QuickJSON explicit operators in QuickJSON.cs
                {
                    if (tk.IsNull)
                        return null;
                    else if (tk.IsString)
                        return tk.Value;
                }
                else if (name.Equals("Int32"))
                {
                    if (tk.TokenType == TType.Long)                  // it won't be a ulong/bigint since that would be too big for an int
                        return (int)(long)tk.Value;
                    else if (tk.TokenType == TType.Double)           // doubles get trunced.. as per previous system
                        return (int)(double)tk.Value;
                }
                else if (name.Equals("Int64"))
                {
                    if (tk.TokenType == TType.Long)
                        return tk.Value;
                    else if (tk.TokenType == TType.Double)
                        return (long)(double)tk.Value;
                }
                else if (name.Equals("Boolean"))
                {
                    if (tk.TokenType == TType.Boolean)
                        return (bool)tk.Value;
                    else if (tk.TokenType == TType.Long)
                        return (long)tk.Value != 0;
                }
                else if (name.Equals("Double"))
                {
                    if (tk.TokenType == TType.Long)
                        return (double)(long)tk.Value;
                    else if (tk.TokenType == TType.ULong)
                        return (double)(ulong)tk.Value;
#if JSONBIGINT
                    else if (tk.TokenType == TType.BigInt)
                        return (double)(System.Numerics.BigInteger)tk.Value;
#endif
                    else if (tk.TokenType == TType.Double)
                        return (double)tk.Value;
                }
                else if (name.Equals("Single"))
                {
                    if (tk.TokenType == TType.Long)
                        return (float)(long)tk.Value;
                    else if (tk.TokenType == TType.ULong)
                        return (float)(ulong)tk.Value;
#if JSONBIGINT
                    else if (tk.TokenType == TType.BigInt)
                        return (float)(System.Numerics.BigInteger)tk.Value;
#endif
                    else if (tk.TokenType == TType.Double)
                        return (float)(double)tk.Value;
                }
                else if (name.Equals("UInt32"))
                {
                    if (tk.TokenType == TType.Long && (long)tk.Value >= 0)
                        return (uint)(long)tk.Value;
                    else if (tk.TokenType == TType.Double && (double)tk.Value >= 0)
                        return (uint)(double)tk.Value;
                }
                else if (name.Equals("UInt64"))
                {
                    if (tk.TokenType == TType.ULong)
                        return (ulong)tk.Value;
                    else if (tk.TokenType == TType.Long && (long)tk.Value >= 0)
                        return (ulong)(long)tk.Value;
                    else if (tk.TokenType == TType.Double && (double)tk.Value >= 0)
                        return (ulong)(double)tk.Value;
                }
                else if (name.Equals("DateTime"))
                {
                    DateTime? dt = tk.DateTime(System.Globalization.CultureInfo.InvariantCulture);
                    if (dt != null)
                        return dt;
                }
                else if (tt.IsEnum)
                {
                    if (!tk.IsString)
                        return null;

                    try
                    {
                        Object p = Enum.Parse(tt, tk.Str(), true);
                        return Convert.ChangeType(p, tt);
                    }
                    catch
                    {
                        System.Diagnostics.Debug.WriteLine("Unable to convert to enum " + tk.Str());
                        return null;
                    }
                }

                return new ToObjectError("JSONToObject: Bad Conversion " + tk.TokenType + " to " + tt.Name);
            }
        }
    }
}



