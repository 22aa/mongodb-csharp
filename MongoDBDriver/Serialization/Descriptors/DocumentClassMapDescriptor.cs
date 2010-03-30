﻿using System;
using System.Collections.Generic;
using MongoDB.Driver.Configuration.Mapping.Model;

namespace MongoDB.Driver.Serialization.Descriptors
{
    internal class DocumentClassMapDescriptor : ClassMapDescriptorBase
    {
        private readonly Document _document;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentClassMapDescriptor"/> class.
        /// </summary>
        /// <param name="classMap">The class map.</param>
        /// <param name="document">The document.</param>
        public DocumentClassMapDescriptor(IClassMap classMap, Document document)
            : base(classMap)
        {
            if (document == null)
                throw new ArgumentNullException("document");

            _document = document;
        }

        /// <summary>
        /// Gets the member map.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public override PersistentMemberMap GetMemberMap(string name)
        {
            return ClassMap.GetMemberMapFromAlias(name);
        }

        /// <summary>
        /// Gets the property names.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<string> GetPropertyNames()
        {
            if (ShouldPersistDiscriminator())
                yield return ClassMap.DiscriminatorAlias;

            PersistentMemberMap memberMap;
            foreach (string key in _document.Keys)
            {
                memberMap = ClassMap.GetMemberMapFromMemberName(key) as PersistentMemberMap;
                if (memberMap == null)
                    yield return key; //if it isn't mapped, we'll persist it anyways...
                else
                    yield return memberMap.Alias;
            }
        }

        /// <summary>
        /// Gets the property type and value.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public override KeyValuePair<Type, object> GetPropertyTypeAndValue(string name)
        {
            if (ClassMap.DiscriminatorAlias == name && ShouldPersistDiscriminator())
                return new KeyValuePair<Type, object>(ClassMap.Discriminator.GetType(), ClassMap.Discriminator);

            object value;

            var memberMap = ClassMap.GetMemberMapFromAlias(name);
            if (memberMap != null)
                value = _document[memberMap.MemberName];
            else
                value = _document[name];

            var type = typeof(Document);

            if (memberMap != null)
                type = memberMap.MemberReturnType;
            else if (value != null)
                type = value.GetType();

            return new KeyValuePair<Type, object>(type, value);
        }
    }
}