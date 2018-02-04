using System;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;

namespace BaseLib
{
    public interface IPoolable
    {
        void Destroy();
    }
}