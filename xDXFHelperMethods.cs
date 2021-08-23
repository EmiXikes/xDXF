using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xDXF
{

    public class xDXF
    { }
    public class xDXFHelperMethods
    {

        #region Helper methods
        public static List<List<ValPair>> SubItems_Old(List<ValPair> data, string subItemCode, string subItemValue = "")
        {
            List<ValPair> SubR = new List<ValPair>();
            List<List<ValPair>> R = new List<List<ValPair>>();
            bool validItem = false;
            foreach (var E in data)
            {

                if (E.Code.Trim() == subItemCode && 
                    (subItemValue.Trim() == E.Value.Trim() || subItemValue.Trim() == "" )
                    )
                {
                    if (SubR.Count != 0)
                    {
                        R.Add(SubR);
                    }
                    SubR = new List<ValPair>();
                    validItem = true;
                }

                if (validItem == true)
                {
                    SubR.Add(E);
                }


            }


            if (SubR.Count != 0)
            {
                R.Add(SubR);
            }
            

            return R;
        }

        public static List<List<ValPair>> SubItems(List<ValPair> data, string subItemCode, string subItemValue = "", bool endCheckCodeOnly = false)
        {
            List<ValPair> SubR = new List<ValPair>();
            List<List<ValPair>> R = new List<List<ValPair>>();
            bool validItem = false;
            foreach (var E in data)
            {
                // Range stop condition reached (e.g. code of the next similar entry). 
                // Add current range to result.
                if (E.Code.Trim() == subItemCode && (subItemValue.Trim() == E.Value.Trim() || subItemValue.Trim() == "" || endCheckCodeOnly))
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
        #endregion
    }
}
