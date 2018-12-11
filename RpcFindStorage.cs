using Microsoft.AspNetCore.Http;
using Neo.IO.Json;
using Neo.Network.RPC;
using Neo.Ledger;
using Neo.SmartContract.Iterators;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Neo.Plugins
{
    public class RpcFindStorage : Plugin, IRpcPlugin
    {
        public override void Configure()
        {
        }

        public JObject OnProcess(HttpContext context, string method, JArray _params)
        {
            if (method == "findstorage")
            {
                UInt160 script_hash = UInt160.Parse(_params[0].AsString());
                byte[] prefix = _params[1].AsString().HexToBytes();
                byte[] prefix_key;
                int toskip = 0;
                int totake = 0;
                StorageIterator iterator;

                using (MemoryStream ms = new MemoryStream())
                {
                    int index = 0;
                    int remain = prefix.Length;
                    while (remain >= 16)
                    {
                        ms.Write(prefix, index, 16);
                        ms.WriteByte(0);
                        index += 16;
                        remain -= 16;
                    }
                    if (remain > 0)
                        ms.Write(prefix, index, remain);
                    prefix_key = script_hash.ToArray()
                        .Concat(ms.ToArray()).ToArray();
                } 

                if (_params.Count > 2)
                    toskip = int.Parse(_params[2].AsString());
	        if (_params.Count > 3)
                    totake = int.Parse(_params[3].AsString());

                iterator = new StorageIterator(
                    Blockchain.Singleton.Store.GetStorages()
                    .Find(prefix_key)
                    .Where(p => p.Key.Key.Take(prefix.Length)
                        .SequenceEqual(prefix)).GetEnumerator()
                );
                List<JObject> result = new List<JObject>();
                foreach(KeyValuePair<StorageKey, StorageItem> p in iterator)
		{
                    JObject item = new JObject();
                    item["key"] = iterator.Key().GetByteArray().ToHexString();
                    item["value"] = iterator.Value().GetByteArray().ToHexString();
                    result.Add(item);
                }
                iterator.Dispose();
                if (result.Count > 0)
                {
                   if (totake > 0)
                        return result.Skip(toskip).Take(totake).ToArray();
                   else
                        return result.Skip(toskip).ToArray();
                }
                else 
                   return "No matches found.";
            }
            return null;
        }
    }
}
