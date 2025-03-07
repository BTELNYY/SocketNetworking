﻿using System;

namespace SocketNetworking.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class TypeWrapperAttribute : Attribute
    {
        public Type TargetType { get; private set; }

        public TypeWrapperAttribute(Type targetType)
        {
            TargetType = targetType;
        }
    }
}