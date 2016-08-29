﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contract.Exceptions
{
    public class StandardProfileNotFoundException : Exception
    {
        public StandardProfileNotFoundException(string message) : base(message)
        {
        }

        public StandardProfileNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
