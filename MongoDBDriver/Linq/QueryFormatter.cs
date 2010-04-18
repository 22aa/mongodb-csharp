﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using MongoDB.Driver.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Collections;

namespace MongoDB.Driver.Linq
{
    internal class QueryFormatter : MongoExpressionVisitor
    {
        private MongoQueryObject _queryObject;

        internal MongoQueryObject Format(Expression e)
        {
            _queryObject = new MongoQueryObject();
            Visit(e);
            return _queryObject;
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            int scopeDepth = _queryObject.ScopeDepth;
            Visit(b.Left);

            switch (b.NodeType)
            {
                case ExpressionType.Equal:
                    break;
                case ExpressionType.GreaterThan:
                    _queryObject.PushConditionScope("$gt");
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    _queryObject.PushConditionScope("$gte");
                    break;
                case ExpressionType.LessThan:
                    _queryObject.PushConditionScope("$lt");
                    break;
                case ExpressionType.LessThanOrEqual:
                    _queryObject.PushConditionScope("$lte");
                    break;
                case ExpressionType.NotEqual:
                    _queryObject.PushConditionScope("$ne");
                    break;
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    break;
                default:
                    throw new NotSupportedException(string.Format("The operation {0} is not supported.", b.NodeType));
            }

            Visit(b.Right);

            while (_queryObject.ScopeDepth > scopeDepth)
                _queryObject.PopConditionScope();

            return b;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            _queryObject.AddCondition(c.Value);
            return c;
        }

        protected override Expression VisitField(FieldExpression f)
        {
            _queryObject.PushConditionScope(f.Name);
            return f;
        }

        protected override Expression VisitMemberAccess(MemberExpression m)
        {
            var type = m.Member.DeclaringType;
            if (m.Member.DeclaringType.IsGenericType)
                type = m.Member.DeclaringType.GetGenericTypeDefinition();

            if (type == typeof(string))
            {
                if (m.Member.Name == "Length")
                {
                    Visit(m.Expression);
                    _queryObject.PushConditionScope("$size");
                    return m;
                }
            }
            else if (type == typeof(Array))
            {
                if (m.Member.Name == "Length")
                {
                    Visit(m.Expression);
                    _queryObject.PushConditionScope("$size");
                    return m;
                }
            }
            else if (typeof(ICollection).IsAssignableFrom(type))
            {
                if (m.Member.Name == "Count")
                {
                    Visit(m.Expression);
                    _queryObject.PushConditionScope("$size");
                    return m;
                }
            }
            else if (typeof(ICollection<>).IsAssignableFrom(type))
            {
                if (m.Member.Name == "Count")
                {
                    Visit(m.Expression);
                    _queryObject.PushConditionScope("$size");
                    return m;
                }
            }

            throw new NotSupportedException(string.Format("The member {0} is not supported.", m.Member.Name));
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(Queryable) || m.Method.DeclaringType == typeof(Enumerable))
            {
                switch (m.Method.Name)
                {
                    case "Count":
                        if (m.Arguments.Count == 1)
                        {
                            Visit(m.Arguments[0]);
                            _queryObject.PushConditionScope("$size");
                            return m;
                        }
                        throw new NotSupportedException("The method Count with a predicate is not supported for field.");
                }
            }
            else if (m.Method.DeclaringType == typeof(string))
            {
                var field = m.Object as FieldExpression;
                if (field == null)
                    throw new InvalidQueryException(string.Format("The mongo field must be the operator for a string operation of type {0}.", m.Method.Name));
                Visit(field);

                var value = EvaluateConstant<string>(m.Arguments[0]);
                if (m.Method.Name == "StartsWith")
                    _queryObject.AddCondition(new MongoRegex(string.Format("^{0}", value)));
                else if (m.Method.Name == "EndsWith")
                    _queryObject.AddCondition(new MongoRegex(string.Format("{0}$", value)));
                else if (m.Method.Name == "Contains")
                    _queryObject.AddCondition(new MongoRegex(string.Format("{0}", value)));
                else
                    throw new NotSupportedException(string.Format("The string method {0} is not supported.", m.Method.Name));

                _queryObject.PopConditionScope();
                return m;
            }
            else if (m.Method.DeclaringType == typeof(Regex))
            {
                if (m.Method.Name == "IsMatch")
                {
                    var field = m.Arguments[0] as FieldExpression;
                    if (field == null)
                        throw new InvalidQueryException(string.Format("The mongo field must be the operator for a string operation of type {0}.", m.Method.Name));

                    Visit(field);
                    string value = null;
                    if (m.Object == null)
                        value = EvaluateConstant<string>(m.Arguments[1]);
                    else
                        throw new InvalidQueryException(string.Format("Only the static Regex.IsMatch is supported.", m.Method.Name));

                    _queryObject.AddCondition(new MongoRegex(value));
                    _queryObject.PopConditionScope();
                    return m;
                }
            }

            throw new NotSupportedException(string.Format("The method {0} is not supported.", m.Method.Name));
        }

        protected override Expression VisitSelect(SelectExpression s)
        {
            if(s.From != null)
                VisitSource(s.From);
            if (s.Where != null)
                Visit(s.Where);

            foreach (var field in s.Fields)
                _queryObject.Fields[field] = 1;

            if (s.Order != null)
            {
                foreach (var order in s.Order)
                {
                    var field = Visit(order.Expression) as FieldExpression;
                    if (field == null)
                        throw new InvalidQueryException("Could not find the field name from the order expression.");
                    _queryObject.AddOrderBy(field.Name, order.OrderType == OrderType.Ascending ? 1 : -1);
                }
            }

            if (s.Limit != null)
                _queryObject.NumberToLimit = EvaluateConstant<int>(s.Limit);

            if (s.Skip!= null)
                _queryObject.NumberToSkip = EvaluateConstant<int>(s.Skip);

            return s;
        }

        protected override Expression VisitSource(Expression source)
        {
            switch ((MongoExpressionType)source.NodeType)
            {
                case MongoExpressionType.Collection:
                    var collection = (CollectionExpression)source;
                    _queryObject.CollectionName = collection.CollectionName;
                    _queryObject.Database = collection.Database;
                    _queryObject.DocumentType = collection.DocumentType;
                    break;
                case MongoExpressionType.Select:
                    Visit(source);
                    break;
                default:
                    throw new InvalidOperationException("Select source is not valid type");
            }
            return source;
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            switch (u.NodeType)
            {
                case ExpressionType.Not:
                    _queryObject.PushConditionScope("$not");
                    Visit(u.Operand);
                    _queryObject.PopConditionScope();
                    break;
                default:
                    throw new NotSupportedException(string.Format("The unary operator {0} is not supported.", u.NodeType));
            }

            return u;
        }

        private static T EvaluateConstant<T>(Expression e)
        {
            if (e.NodeType != ExpressionType.Constant)
                throw new ArgumentException("Expression must be a constant.");

            return (T)((ConstantExpression)e).Value;
        }

        private static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
                e = ((UnaryExpression)e).Operand;
            return e;
        }
    }
}