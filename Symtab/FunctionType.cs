﻿namespace Symtab
{
    using System.Collections.Generic;

    /// <summary>
    /// For C types like "void (*)(int)", we need that to be a pointer to a function
    ///  taking a single integer argument returning void.
    /// </summary>
    public class FunctionType : IType
    {
        protected internal readonly IType returnType;
        protected internal readonly IList<IType> argumentTypes;

        public FunctionType(IType returnType, IList<IType> argumentTypes)
        {
            this.returnType = returnType;
            this.argumentTypes = argumentTypes;
        }

        public virtual string Name
        {
            get
            {
                return ToString();
            }
        }

        public virtual int TypeIndex
        {
            get
            {
                return -1;
            }
        }

        public virtual IList<IType> ArgumentTypes
        {
            get
            {
                return argumentTypes;
            }
        }

        public override string ToString()
        {
            return "*" + returnType;
        }
    }

}