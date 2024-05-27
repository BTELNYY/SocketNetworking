using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

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