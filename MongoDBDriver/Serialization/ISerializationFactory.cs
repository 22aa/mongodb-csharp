﻿using System;
using MongoDB.Driver.Bson;

namespace MongoDB.Driver.Serialization
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISerializationFactory
    {
        /// <summary>
        /// Gets the bson builder.
        /// </summary>
        /// <param name="rootType">Type of the root.</param>
        /// <returns></returns>
        IBsonObjectBuilder GetBsonBuilder(Type rootType);

        /// <summary>
        /// Gets the bson descriptor.
        /// </summary>
        /// <param name="rootType">Type of the root.</param>
        /// <returns></returns>
        IBsonObjectDescriptor GetBsonDescriptor(Type rootType);

        /// <summary>
        /// Gets the object descriptor.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        IObjectDescriptor GetObjectDescriptor(Type type);
    }
}