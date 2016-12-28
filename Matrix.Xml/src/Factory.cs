using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Matrix.Core.Attributes;

namespace Matrix.Xml
{
    /// <summary>
    /// Factory for registering XmppXElement types
    /// </summary>
    public class Factory
    {
        static readonly Dictionary<string, Type> FactoryTable = new Dictionary<string, Type>();

        /// <summary>
        /// Builds the key for looking up.
        /// </summary>
        /// <param name="ns">The ns.</param>
        /// <param name="localName">Name of the local.</param>
        /// <returns></returns>
        private static string BuildKey(string ns, string localName)
        {
            return "{" + ns + "}" + localName;
        }

        public static XmppXElement GetElement(string prefix, string localName, string ns)
        {
            Type t = null;
            string key = BuildKey(ns, localName);
            lock (FactoryTable)
            {
                if (FactoryTable.ContainsKey(key))
                    t = FactoryTable[key];
            }

            XmppXElement ret;
            if (t != null)
            {
                /*
                 * unity webplayer does not support Activator.CreateInstance,
                 * but can create types with compiled lambdas instead.             
                 */
                ret = (XmppXElement)Activator.CreateInstance(t);
            }
            else
                ret = !string.IsNullOrEmpty(ns) ? new XmppXElement(ns, localName) : new XmppXElement(localName);
                             
            return ret;
        }

        #region << register over methods >>
        public static void RegisterElement<T>(string localName) where T : XmppXElement
        {
            RegisterElement<T>("", localName);
        }

        /// <summary>
        /// Adds new Element Types to the Hashtable
        /// Use this function also to register your own created Elements.
        /// If a element is already registered it gets overwritten. This behaviour is also useful if you you want to overwrite
        /// classes and add your own derived classes to the factory.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ns"></param>
        /// <param name="localName"></param>
        public static void RegisterElement<T>(string ns, string localName) where T : XmppXElement
        {
            RegisterElement(typeof(T), ns, localName);           
        }

        private static void RegisterElement(Type type, string ns, string localName)
        {
            string key = BuildKey(ns, localName);

            // added thread safety on a user request
            lock (FactoryTable)
            {
                if (FactoryTable.ContainsKey(key))
                    FactoryTable[key] = type;
                else
                    FactoryTable.Add(key, type);
            }
        }
        #endregion

        #region register over attributes   
        private static void RegisterElement(Type type)
        {
            type
                .GetTypeInfo()
                .GetCustomAttributes<XmppTagAttribute>(false)
                .ToList()
                .ForEach(att =>
                    RegisterElement(type, att.Namespace, att.Name)
                );
        }

        /// <summary>
        /// Registers the element.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void RegisterElement<T>() where T : XmppXElement
        {
            var t = typeof(T).GetTypeInfo();
            var att = t.GetCustomAttributes(typeof(XmppTagAttribute), false).FirstOrDefault() as XmppTagAttribute;

            if (att != null)
                RegisterElement<T>(att.Namespace, att.Name);
        }

        /// <summary>
        /// Looks in a complete assembly for all XmppXElements and registered them using the XmppTag attribute.
        /// The XmppTag attribute must be present on the classes to register
        /// </summary>
        /// <param name="assembly"></param>
        public static void RegisterElementsFromAssembly(Assembly assembly)
        {
            var types = GetTypesWithAttribueFromAssembly<XmppTagAttribute>(assembly);

            foreach (var type in types)
                RegisterElement(type.GetType());
        }

        /*
        private static void RegisterFromAttributes()
        {
            RegisterElementsFromAssembly(typeof(Factory).GetTypeInfo().Assembly);
        }
        */
        private static IEnumerable<TypeInfo> GetTypesWithAttribueFromAssembly<TAttribute>(Assembly assembly) where TAttribute : Attribute
        {
            return assembly
                .DefinedTypes
                .Where(t => t.IsSubclassOf(typeof(XmppXElement)))
                .Where(t => t.IsDefined(typeof(TAttribute), false));
        }
        #endregion
    }
}