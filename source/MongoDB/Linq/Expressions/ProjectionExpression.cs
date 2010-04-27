﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace MongoDB.Driver.Linq.Expressions
{
    internal class ProjectionExpression : Expression
    {
        private readonly SelectExpression _source;
        private readonly Expression _projector;
        private readonly LambdaExpression _aggregator;

        public SelectExpression Source
        {
            get { return _source; }
        }

        public Expression Projector
        {
            get { return _projector; }
        }

        public LambdaExpression Aggregator
        {
            get { return _aggregator; }
        }

        public ProjectionExpression(SelectExpression source, Expression projector)
            : this(source, projector, null)
        { }

        public ProjectionExpression(SelectExpression source, Expression projector, LambdaExpression aggregator)
            : base((ExpressionType)MongoExpressionType.Projection, source.Type)
        {
            _source = source;
            _projector = projector;
            _aggregator = aggregator;
        }
    }
}
