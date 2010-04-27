﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections.ObjectModel;

namespace MongoDB.Linq.Expressions
{
    internal class FindExpression : Expression
    {
        private readonly bool _distinct;
        private readonly ReadOnlyCollection<FieldExpression> _fields;
        private readonly Expression _from;
        private readonly ReadOnlyCollection<Expression> _groupBy;
        private readonly Expression _limit;
        private readonly ReadOnlyCollection<OrderExpression> _orderBy;
        private readonly Expression _skip;
        private readonly Expression _where;

        public bool Distinct
        {
            get { return _distinct; }
        }

        public ReadOnlyCollection<FieldExpression> Fields
        {
            get { return _fields; }
        }

        public Expression From
        {
            get { return _from; }
        }

        public ReadOnlyCollection<Expression> GroupBy
        {
            get { return _groupBy; }
        }

        public Expression Limit
        {
            get { return _limit; }
        }

        public ReadOnlyCollection<OrderExpression> OrderBy
        {
            get { return _orderBy; }
        }

        public Expression Skip
        {
            get { return _skip; }
        }

        public Expression Where
        {
            get { return _where; }
        }

        public FindExpression(Type type, IEnumerable<FieldExpression> fields, Expression from, Expression where)
            : this(type, fields, from, where, null, null, false, null, null)
        { }

        public FindExpression(Type type, IEnumerable<FieldExpression> fields, Expression from, Expression where, IEnumerable<OrderExpression> orderBy, IEnumerable<Expression> groupBy, bool distinct, Expression skip, Expression limit)
            : base((ExpressionType)MongoExpressionType.Select, type)
        {
            _fields = fields as ReadOnlyCollection<FieldExpression>;
            if (_fields == null)
                _fields = new List<FieldExpression>(fields).AsReadOnly();

            _orderBy = orderBy as ReadOnlyCollection<OrderExpression>;
            if (_orderBy == null && orderBy != null)
                _orderBy = new List<OrderExpression>(orderBy).AsReadOnly();

            _groupBy = groupBy as ReadOnlyCollection<Expression>;
            if (_groupBy == null && groupBy != null)
                _groupBy = new List<Expression>(groupBy).AsReadOnly();

            _distinct = distinct;
            _from = from;
            _limit = limit;
            _where = where;
            _skip = skip;
        }
    }
}