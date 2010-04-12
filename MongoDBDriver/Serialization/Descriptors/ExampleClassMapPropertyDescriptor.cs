﻿using System;
using System.Collections.Generic;
using System.Reflection;
using MongoDB.Driver.Configuration.Mapping.Model;
using MongoDB.Driver.Configuration.Mapping;

namespace MongoDB.Driver.Serialization.Descriptors
{
    internal class ExampleClassMapPropertyDescriptor : ClassMapPropertyDescriptorBase
    {
        private readonly object _example;
        private readonly Type _exampleType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleClassMapPropertyDescriptor"/> class.
        /// </summary>
        /// <param name="mappingStore">The mapping store.</param>
        /// <param name="classMap">The class map.</param>
        /// <param name="example">The example.</param>
        public ExampleClassMapPropertyDescriptor(IMappingStore mappingStore, IClassMap classMap, object example)
            : base(mappingStore, classMap)
        {
            if (example == null)
                throw new ArgumentNullException("example");

            _example = example;
            _exampleType = _example.GetType();
        }

        /// <summary>
        /// Gets the property names.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<KeyValuePair<string, KeyValuePair<Type, object>>> GetProperties()
        {
            if (ShouldPersistDiscriminator())
                yield return CreateProperty(ClassMap.DiscriminatorAlias, ClassMap.Discriminator.GetType(), ClassMap.Discriminator);

            foreach (PropertyInfo propertyInfo in _exampleType.GetProperties())
                yield return CreateProperty(GetAliasFromMemberName(propertyInfo.Name), GetPropertyTypeAndValue(propertyInfo));
        }

        /// <summary>
        /// Gets the property type and value.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        private KeyValuePair<Type, object> GetPropertyTypeAndValue(PropertyInfo propertyInfo)
        {
            Type type;

            var value = propertyInfo.GetValue(_example, null);
            var memberMap = GetMemberMapFromMemberName(propertyInfo.Name);
            if (memberMap != null)
            {
                type = memberMap.MemberReturnType;
                if (memberMap is CollectionMemberMap)
                    type = ((CollectionMemberMap)memberMap).ElementType;
            }
            else
                type = propertyInfo.PropertyType;

            return new KeyValuePair<Type, object>(type, value);
        }
    }
}