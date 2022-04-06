using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;


namespace xDXF
{
    public class EntityClass
    {
        public List<ValPair> Data;

        public EntityClass()
        {
            Data = new List<ValPair>();
        }

        public List<List<ValPair>> SubItems(string subItemCode, string subItemValue = "", string[] endCodes = null)
        {
            return xDXFHelperMethods.SubItems(Data, subItemCode, subItemValue, endCodes);

        }

        public string ReadCode(string Code)
        {

            var R = Data.FirstOrDefault(x => x.Code.Trim() == Code.Trim());

            if (R != null)
            {
                return R.Value;
            }
            else
            {
                return null;
            }

        }

        public void WriteCode(string Code, string Value)
        {
            Data.FirstOrDefault(x => x.Code.Trim() == Code.Trim()).Value = Value;
        }

        public void SetEntityColorIndexed(string value)
        {
            var AcDbEntity = SubItems(code.SUBCLASSHEADER, "AcDbEntity")[0];
            AcDbEntity.FirstOrDefault(C => C.Code.Trim() == code.COLOR).Value = value;
            // TODO must fix case if previous color was ByLayer. 62 tag is not present.

            // in case, if previous color was rgb
            var rgbColValPair = AcDbEntity.FirstOrDefault(C => C.Code.Trim() == code.COLORRGB);
            if (rgbColValPair != null)
            {
                Data.Remove(rgbColValPair);
                rgbColValPair.flags |= vpFlag.DELETE;
            }
        }

        public void SetEntityColorRGB(string value)
        {
            var AcDbEntity = SubItems(code.SUBCLASSHEADER, "AcDbEntity")[0];
            var RGB = value.Split(',').ToList();
            System.Drawing.Color rgbColor = System.Drawing.Color.FromArgb(Convert.ToInt32(RGB[0]), Convert.ToInt32(RGB[1]), Convert.ToInt32(RGB[2]));
            var colorCodeIntString = Convert.ToInt32(ColorTranslator.ToHtml(rgbColor).Replace("#", ""), 16).ToString();

            var col = AcDbEntity.FirstOrDefault(C => C.Code.Trim() == code.COLOR);
            var colRGB = AcDbEntity.FirstOrDefault(C => C.Code.Trim() == code.COLORRGB);
            if (colRGB != null)
            {
                colRGB.Value = colorCodeIntString;
            }
            else
            {
                ValPair newColRGB = new ValPair()
                {
                    Code = code.COLORRGB,
                    Value = colorCodeIntString,
                };
                col.insertAfterMe.Add(newColRGB);
            }
        }

        public string GetEntityColor()
        {
            var AcDbEntity = SubItems(code.SUBCLASSHEADER, "AcDbEntity")[0];
            var col = AcDbEntity.FirstOrDefault(C => C.Code.Trim() == code.COLOR);
            var colRGB = AcDbEntity.FirstOrDefault(C => C.Code.Trim() == code.COLORRGB);

            if (col == null)
            {
                return "ByLayer";
            }

            if (col.Value.Trim() == "0")
            {
                return "ByBlock";
            }

            if (colRGB != null)
            {
                Color color = ColorTranslator.FromHtml(colRGB.Value.Trim());
                return color.R + "," + color.G + "," + color.B;
            }

            return col.Value.Trim();
        }

    }
}




