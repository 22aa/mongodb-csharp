﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using MongoDB.Driver.Linq.Expressions;
using System.Text.RegularExpressions;

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
            if (b.NodeType == ExpressionType.And || b.NodeType == ExpressionType.AndAlso)
            {
                Visit(b.Left);
                Visit(b.Right);
                return b;
            }

            var left = b.Left;
            var right = b.Right;

            //xor operation
            if (left.NodeType != (ExpressionType)MongoExpressionType.Field && right.NodeType != (ExpressionType)MongoExpressionType.Field)
                throw new InvalidQueryException();
            else if (left.NodeType == (ExpressionType)MongoExpressionType.Field && right.NodeType == (ExpressionType)MongoExpressionType.Field)
                throw new InvalidQueryException();

            if (right.NodeType == (ExpressionType)MongoExpressionType.Field)
            {
                left = b.Right;
                right = b.Left;
                //reverse the order so that the field is on the left side...
            }

            if (right.NodeType != ExpressionType.Constant)
                throw new InvalidQueryException();

            var fieldName = ((FieldExpression)left).Name;

            switch (b.NodeType)
            {
                case ExpressionType.Equal:
                    _queryObject.AddCondition(fieldName, EvaluateConstant((ConstantExpression)right));
                    break;
                case ExpressionType.GreaterThan:
                    _queryObject.AddCondition(fieldName, Op.GreaterThan(EvaluateConstant((ConstantExpression)right)));
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    _queryObject.AddCondition(fieldName, Op.GreaterThanOrEqual(EvaluateConstant((ConstantExpression)right)));
                    break;
                case ExpressionType.LessThan:
                    _queryObject.AddCondition(fieldName, Op.LessThan(EvaluateConstant((ConstantExpression)right)));
                    break;
                case ExpressionType.LessThanOrEqual:
                    _queryObject.AddCondition(fieldName, Op.LessThanOrEqual(EvaluateConstant((ConstantExpression)right)));
                    break;
                case ExpressionType.NotEqual:
                    _queryObject.AddCondition(fieldName, Op.NotEqual(EvaluateConstant((ConstantExpression)right)));
                    break;
            }

            return b;
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(string))
            {
                var field = m.Object as FieldExpression;
                if (field == null)
                    throw new InvalidQueryException(string.Format("The mongo field must be the operator for a string operation of type {0}.", m.Method.Name));

                var value = (string)((ConstantExpression)Visit(m.Arguments[0])).Value;
                if (m.Method.Name == "StartsWith")
                    _queryObject.AddCondition(field.Name, new MongoRegex(string.Format("^{0}", value)));
                else if (m.Method.Name == "EndsWith")
                    _queryObject.AddCondition(field.Name, new MongoRegex(string.Format("{0}$", value)));
                else if (m.Method.Name == "Contains")
                    _queryObject.AddCondition(field.Name, new MongoRegex(string.Format("{0}", value)));
                else
                    throw new NotSupportedException(string.Format("The string method {0} is not supported.", m.Method.Name));

                return m;
            }
            else if (m.Method.DeclaringType == typeof(Regex))
            {
                if (m.Method.Name == "IsMatch")
                {
                    var field = m.Arguments[0] as FieldExpression;
                    if (field == null)
                        throw new InvalidQueryException(string.Format("The mongo field must be the operator for a string operation of type {0}.", m.Method.Name));

                    string value = null;
                    if(m.Object == null)
                        value = (string)((ConstantExpression)Visit(m.Arguments[1])).Value;
                    else
                        throw new InvalidQueryException(string.Format("Only the static Regex.IsMatch is supported.", m.Method.Name));

                    _queryObject.AddCondition(field.Name, new MongoRegex(value));
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
                _queryObject.NumberToLimit = (int)((ConstantExpression)Visit(s.Limit)).Value;

            if (s.Skip!= null)
                _queryObject.NumberToSkip = (int)((ConstantExpression)Visit(s.Skip)).Value;

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

        private static object EvaluateConstant(ConstantExpression c)
        {
            return c.Value;
        }

        private static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
                e = ((UnaryExpression)e).Operand;
            return e;
        }
    }
}