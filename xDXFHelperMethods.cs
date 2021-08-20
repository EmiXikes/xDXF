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
        public static List<List<ValPair>> SubItems(List<ValPair> data, string subItemCode, string subItemValue = "")
        {
            List<ValPair> SubR = new List<ValPair>();
            List<List<ValPair>> R = new List<List<ValPair>>();
            bool validItem = false;
            foreach (var E in data)
            {

                if (E.Code.Trim() == subItemCode && (subItemValue.Trim() == E.Value.Trim() || subItemValue.Trim() == ""))
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
        #endregion
    }
}
