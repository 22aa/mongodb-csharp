﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MongoDB.Driver;
using MongoDB.Driver.Configuration.Mapping.Model;

namespace MongoDB.Driver.Serialization.Descriptors
{
    internal class DocumentClassMapDescriptor : IClassMapDescriptor
    {
        private readonly IClassMap _classMap;
        private readonly Document _document;

        public DocumentClassMapDescriptor(IClassMap classMap, Document document)
        {
            if (classMap == null)
                throw new ArgumentNullException("classMap");
            if (document == null)
                throw new ArgumentNullException("document");

            _classMap = classMap;
            _document = document;
        }

        public PersistentMemberMap GetMemberMap(string name)
        {
            return _classMap.GetMemberMapFromAlias(name);
        }

        public IEnumerable<string> GetPropertyNames()
        {
            if (_classMap.ShouldPersistDiscriminator())
                yield return _classMap.DiscriminatorAlias;

            PersistentMemberMap memberMap;
            foreach (string key in _document.Keys)
            {
                memberMap = _classMap.GetMemberMapFromMemberName(key) as PersistentMemberMap;
                if (memberMap == null)
                    yield return key; //if it isn't mapped, we'll persist it anyways...
                else
                    yield return memberMap.Alias;
            }
        }

        public KeyValuePair<Type, object> GetPropertyTypeAndValue(string name)
        {
            if (_classMap.ShouldPersistDiscriminator() && _classMap.DiscriminatorAlias == name)
                return new KeyValuePair<Type, object>(_classMap.Discriminator.GetType(), _classMap.Discriminator);

            var memberMap = _classMap.GetMemberMapFromAlias(name);
            if (memberMap == null) //if it isn't mapped, return it as is...
                return new KeyValuePair<Type, object>(_document[name].GetType(), _document[name]);

            var value = _document[memberMap.MemberName];
            if (value is Document)
                return new KeyValuePair<Type, object>(typeof(Document), value);

            return new KeyValuePair<Type, object>(memberMap.MemberReturnType, value);
        }
    }
}