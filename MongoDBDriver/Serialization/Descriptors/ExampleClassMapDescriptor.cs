﻿using System;
using System.Collections.Generic;
using System.Reflection;
using MongoDB.Driver.Configuration.Mapping.Model;

namespace MongoDB.Driver.Serialization.Descriptors
{
    internal class ExampleClassMapDescriptor : ClassMapDescriptorBase
    {
        private readonly object _example;
        private readonly Type _exampleType;

        public ExampleClassMapDescriptor(IClassMap classMap, object example)
            : base(classMap)
        {
            if (example == null)
                throw new ArgumentNullException("example");

            _example = example;
            _exampleType = _example.GetType();
        }

        public override PersistentMemberMap GetMemberMap(string name)
        {
            return _classMap.GetMemberMapFromAlias(name);
        }

        public override IEnumerable<string> GetPropertyNames()
        {
            if (ShouldPersistDiscriminator())
                yield return _classMap.DiscriminatorAlias;

            PersistentMemberMap memberMap;
            foreach (PropertyInfo propertyInfo in _exampleType.GetProperties())
            {
                memberMap = _classMap.GetMemberMapFromMemberName(propertyInfo.Name) as PersistentMemberMap;
                if (memberMap == null)
                    yield return propertyInfo.Name; //if it isn't mapped, we'll persist it anyways...
                else
                    yield return memberMap.Alias;
            }
        }

        public override KeyValuePair<Type, object> GetPropertyTypeAndValue(string name)
        {
            if (_classMap.DiscriminatorAlias == name && ShouldPersistDiscriminator())
                return new KeyValuePair<Type, object>(_classMap.Discriminator.GetType(), _classMap.Discriminator);

            Type type;
            object value;

            var memberMap = _classMap.GetMemberMapFromAlias(name);
            var propInfo = _exampleType.GetProperty(memberMap.MemberName);
            value = propInfo.GetValue(_example, null);
            if (value is Document)
                type = typeof(Document);
            else if (memberMap != null)
                type = memberMap.MemberReturnType;
            else
                type = propInfo.PropertyType;

            return new KeyValuePair<Type, object>(type, value);
        }
    }
}