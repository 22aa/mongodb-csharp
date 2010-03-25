﻿using System;

using MongoDB.Driver;
using MongoDB.Driver.Configuration.IdGenerators;

namespace MongoDB.Driver.Configuration.Mapping.Conventions
{
    public class DefaultIdGeneratorConvention : IIdGeneratorConvention
    {
        public static readonly DefaultIdGeneratorConvention Instance = new DefaultIdGeneratorConvention();

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultIdGeneratorConvention"/> class.
        /// </summary>
        private DefaultIdGeneratorConvention()
        { }

        /// <summary>
        /// Gets the generator.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public IIdGenerator GetGenerator(Type type)
        {
            if (type == typeof(Oid))
                return new MongoDB.Driver.Configuration.IdGenerators.OidGenerator();

            if (type == typeof(Guid))
                return new GuidCombGenerator();

            return new AssignedIdGenerator();
        }
    }
}