﻿using API;
using System.Collections.Generic;

namespace PxStat.Data
{
    internal class PxApiMetadata
    {
        internal List<PxApiItem> ReadSubjectsAsObjectList(JSONRPC_Output result)
        {
            List<PxApiItem> pList = new List<PxApiItem>();

            foreach (var item in result.data)
            {
                pList.Add(new PxApiItem() { id = item.SbjCode.ToString(), text = item.SbjValue, type = Utility.GetCustomConfig("APP_PXAPI_LIST") });
            }

            return pList;
        }

        internal List<PxApiItem> ReadProductsAsObjectList(JSONRPC_Output result)
        {
            List<PxApiItem> pList = new List<PxApiItem>();


            foreach (var item in result.data)
            {
                pList.Add(new PxApiItem() { id = item.PrcCode.ToString(), text = item.PrcValue, type = Utility.GetCustomConfig("APP_PXAPI_LIST") });
            }

            return pList;
        }

        internal List<PxApiItem> ReadCollectionAsObjectList(JSONRPC_Output result)
        {
            List<PxApiItem> pList = new List<PxApiItem>();

            foreach (var item in result.data)
            {
                pList.Add(new PxApiItem() { id = item.MtrCode, text = item.MtrTitle, type = Utility.GetCustomConfig("APP_PXAPI_TABLE") });
            }

            return pList;
        }


    }


    public class PxApiItem
    {
        public string id { get; set; }
        public string type { get; set; }
        public string text { get; set; }

    }

    public class PxApiMetadataItem : PxApiItem
    {
        public string updated { get; set; }
    }
}
