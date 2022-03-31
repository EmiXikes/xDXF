using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xDXF
{

    public class xDictionary<Tkey, Tvalue> : Dictionary<string, object>
    {

    }

    public class xDXF
    { }
    public class xDXFHelperMethods
    {
        #region Helper methods
        public static List<List<ValPair>> SubItems(List<ValPair> data, 
            string subItemCode, string subItemValue = "", 
            string[] endCheckCodes = null)
        {
            List<ValPair> SubR = new List<ValPair>();
            List<List<ValPair>> R = new List<List<ValPair>>();
            bool validItem = false;
            foreach (var E in data)
            {
                // Range stop condition reached (e.g. code of the next similar entry). 
                // Add current range to result.
                if (IsEndConditionReached(E, subItemCode, subItemValue, endCheckCodes))
                {
                    if (SubR.Count != 0)
                    {
                        R.Add(SubR);
                    }
                    SubR = new List<ValPair>();
                    validItem = false;
                }

                // Start of valid range detected 
                if (E.Code.Trim() == subItemCode && (subItemValue.Trim() == E.Value.Trim() || subItemValue.Trim() == "") )
                {
                    validItem = true;
                }

                // Add item to range
                if (validItem == true)
                {
                    SubR.Add(E);
                }
            }

            // Add last range
            if (SubR.Count != 0)
            {
                R.Add(SubR);
            }
            return R;
        }

        private static bool IsEndConditionReached(ValPair testItem, string subItemCode, string subItemValue, string[] endCheckCodes)
        {
            if(endCheckCodes != null && (endCheckCodes.ToList().Count > 0) )
            {
                foreach (string endCheckCode in endCheckCodes)
                {
                    if (testItem.Code.Trim() == endCheckCode.Trim())
                    {
                        return true;
                    }
                }
            }

            if (testItem.Code.Trim() == subItemCode)
            {
                if (subItemValue.Trim() == "")
                {
                    return true;
                }
                else
                {
                    if (subItemValue.Trim() == testItem.Value.Trim())
                    {
                        return true;
                    }
                }
            }

                


            return false;
        }
        #endregion
    }
}
