﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitSharp.Core
{
    public enum PruningMode
    {
        None,
        ReplayOnly,
        ReplayAndRollback,
        ReplayAndRollbackAndTxes
    }
}
