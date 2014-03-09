﻿using BitSharp.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitSharp.Data
{
    public interface UtxoBuilder : IDisposable
    {
        bool ContainsKey(UInt256 txHash);

        bool Remove(UInt256 txHash);

        void Clear();

        void Add(UInt256 txHash, UnspentTx unspentTx);

        int Count { get; }

        UnspentTx this[UInt256 txHash] { get; set; }

        Utxo Close(UInt256 blockHash);
    }
}
