﻿using System;
using System.Collections;
using System.Collections.Generic;
using MongoDB.Driver.Bson;
using MongoDB.Driver.Configuration.Mapping;
using MongoDB.Driver.Serialization.Descriptors;

namespace MongoDB.Driver.Serialization
{
    internal class BsonClassMapDescriptor : IBsonObjectDescriptor
    {
        private readonly Stack<Type> _types;
        private readonly IMappingStore _mappingStore;

        public BsonClassMapDescriptor(IMappingStore mappingStore, Type rootType)
        {
            if (mappingStore == null)
                throw new ArgumentNullException("mappingStore");
            if (rootType == null)
                throw new ArgumentNullException("rootType");

            _mappingStore = mappingStore;
            _types = new Stack<Type>();
            _types.Push(rootType);
        }

        public object BeginObject(object instance)
        {
            if (instance is Document)
                return BeginDocument((Document)instance);

            var currentClassMap = _mappingStore.GetClassMap(_types.Peek());
            var instanceType = instance.GetType();
            if (currentClassMap.ClassType.IsAssignableFrom(instanceType))
            {
                if (currentClassMap.ClassType != instanceType) //we are a subclass
                    currentClassMap = _mappingStore.GetClassMap(instanceType);

                return new ClassMapPropertyDescriptor(currentClassMap, instance);
            }

            return new ExampleClassMapPropertyDescriptor(currentClassMap, instance);
        }

        public object BeginArray(object instance)
        {
            return new ArrayDescriptor((IEnumerable)instance);
        }

        public IEnumerable<string> GetPropertyNames(object instance)
        {
            return ((IPropertyDescriptor)instance).GetPropertyNames();
        }

        public object BeginProperty(object instance, string name)
        {
            var pair = ((IPropertyDescriptor)instance).GetPropertyTypeAndValue(name);
            _types.Push(pair.Key);
            return pair.Value;
        }

        public void EndProperty(object instance, string name, object value)
        {
            _types.Pop();
        }

        public void EndArray(object instance)
        { }

        public void EndObject(object instance)
        { }

        public bool IsArray(object instance)
        {
            if (instance is Document)
                return false;

            return instance is IEnumerable;
        }

        public bool IsObject(object instance)
        {
            return !IsNativeToMongo(instance.GetType());
        }

        private object BeginDocument(Document document)
        {
            //if we are expecting a document, no translation is necessary...
            if (typeof(Document).IsAssignableFrom(_types.Peek()))
                return new DocumentPropertyDescriptor(document);

            var currentClassMap = _mappingStore.GetClassMap(_types.Peek());

            return new DocumentClassMapPropertyDescriptor(currentClassMap, document);
        }

        private static bool IsNativeToMongo(Type type)
        {
            var typeCode = Type.GetTypeCode(type);

            if (typeCode != TypeCode.Object)
                return true;

            if (type == typeof(Guid))
                return true;

            if (type == typeof(Oid))
                return true;

            if (type == typeof(byte[]))
                return true;

            return false;
        }
    }
}