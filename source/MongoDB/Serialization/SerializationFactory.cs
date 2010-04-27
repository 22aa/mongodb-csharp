﻿using System;
using MongoDB.Bson;
using MongoDB.Configuration.Mapping;

namespace MongoDB.Serialization
{
    /// <summary>
    /// 
    /// </summary>
    public class SerializationFactory : ISerializationFactory
    {
        /// <summary>
        /// 
        /// </summary>
        public static readonly SerializationFactory Default = new SerializationFactory();

        private readonly IMappingStore _mappingStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializationFactory"/> class.
        /// </summary>
        public SerializationFactory()
            : this(new AutoMappingStore())
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializationFactory"/> class.
        /// </summary>
        /// <param name="mappingStore">The mapping store.</param>
        public SerializationFactory(IMappingStore mappingStore){
            if(mappingStore == null)
                throw new ArgumentNullException("mappingStore");

            _mappingStore = mappingStore;
        }

        /// <summary>
        /// Gets the builder.
        /// </summary>
        /// <param name="rootType">Type of the root.</param>
        /// <returns></returns>
        public IBsonObjectBuilder GetBsonBuilder(Type rootType)
        {
            return new BsonClassMapBuilder(_mappingStore, rootType);
        }

        /// <summary>
        /// Gets the descriptor.
        /// </summary>
        /// <param name="rootType">Type of the root.</param>
        /// <returns></returns>
        public IBsonObjectDescriptor GetBsonDescriptor(Type rootType)
        {
            return new BsonClassMapDescriptor(_mappingStore, rootType);
        }

        /// <summary>
        /// Gets the name of the collection given the rootType.
        /// </summary>
        /// <param name="rootType">Type of the root.</param>
        /// <returns></returns>
        public string GetCollectionName(Type rootType)
        {
            if (rootType == null)
                throw new ArgumentNullException("rootType");

            if (typeof(Document).IsAssignableFrom(rootType))
                throw new InvalidOperationException("Documents cannot have a default collection name.");

            return _mappingStore.GetClassMap(rootType).CollectionName;
        }

        /// <summary>
        /// Gets the object descriptor.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public IObjectDescriptor GetObjectDescriptor(Type type)
        {
            if (typeof(Document).IsAssignableFrom(type))
                return new DocumentObjectDescriptorAdapter();

            return new ClassMapObjectDescriptorAdapter(_mappingStore.GetClassMap(type));
        }
    }
}