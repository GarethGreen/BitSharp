﻿using BitSharp.Common;
using BitSharp.Common.ExtensionMethods;
using BitSharp.Storage;
using BitSharp.Network;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitSharp.Data;
using System.Data.SqlClient;

namespace BitSharp.Storage.MongoDB
{
    public class InvalidBlockStorage : MongoDBDataStorage<string>
    {
        public InvalidBlockStorage(MongoDBStorageContext storageContext)
            : base(storageContext, "invalidBlocks",
                data => StorageEncoder.EncodeVarString(data),
                (blockHash, bytes) => StorageEncoder.DecodeVarString(bytes))
        { }
    }
}