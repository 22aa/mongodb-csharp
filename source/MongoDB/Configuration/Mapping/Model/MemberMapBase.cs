﻿using System;
using System.ComponentModel;

namespace MongoDB.Driver.Configuration.Mapping.Model
{
    /// <summary>
    /// 
    /// </summary>
    public class MemberMapBase
    {
        private readonly Func<object, object> _getter;
        private readonly string _memberName;
        private readonly Type _memberReturnType;
        private readonly Action<object, object> _setter;

        /// <summary>
        /// Gets the name of the member.
        /// </summary>
        /// <value>The name of the member.</value>
        public string MemberName
        {
            get { return _memberName; }
        }

        /// <summary>
        /// Gets the type of the member return.
        /// </summary>
        /// <value>The type of the member return.</value>
        public Type MemberReturnType
        {
            get { return _memberReturnType; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemberMapBase"/> class.
        /// </summary>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="memberReturnType">Type of the member return.</param>
        /// <param name="getter">The getter.</param>
        /// <param name="setter">The setter.</param>
        protected MemberMapBase(string memberName, Type memberReturnType, Func<object, object> getter, Action<object, object> setter)
        {
            _getter = getter;
            _memberName = memberName;
            _memberReturnType = memberReturnType;
            _setter = setter;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns></returns>
        public virtual object GetValue(object instance)
        {
            return _getter(instance);
        }

        /// <summary>
        /// Sets the value on the specified instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="value">The value.</param>
        public virtual void SetValue(object instance, object value)
        {
            var valueType = value != null ? value.GetType() : typeof(object);

            if(valueType!=_memberReturnType)
            {
                var code = Convert.GetTypeCode(value);

                if(code!=TypeCode.Object)
                {
                    value = Convert.ChangeType(value, _memberReturnType);
                }
            }
            
            _setter(instance, value);
        }
    }
}