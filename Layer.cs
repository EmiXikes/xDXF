using System;
using System.Drawing;
using System.Linq;


namespace xDXF
{
    public class Layer : EntityClass
    {
        public string Name
        {
            get
            {
                var AcDbEntity = SubItems(code.SUBCLASSHEADER, "AcDbLayerTableRecord")[0];
                var LayName = AcDbEntity.FirstOrDefault(C => C.Code.Trim() == code.NAME);
                return LayName.Value;
            }
            //set
            //{
            //    var AcDbEntity = SubItems(code.SUBCLASSHEADER, "AcDbLayerTableRecord")[0];
            //    var LayName = AcDbEntity.FirstOrDefault(C => C.Code.Trim() == code.NAME);

            //    LayName.Value = value;
            //}
        }
        public bool IsVisible
        {
            get
            {
                var AcDbEntity = SubItems(code.SUBCLASSHEADER, "AcDbLayerTableRecord")[0];
                var col = AcDbEntity.FirstOrDefault(C => C.Code.Trim() == code.COLOR);

                return int.Parse(col.Value) >= 0;
            }
            set
            {
                var AcDbEntity = SubItems(code.SUBCLASSHEADER, "AcDbLayerTableRecord")[0];
                var col = AcDbEntity.FirstOrDefault(C => C.Code.Trim() == code.COLOR);

                if (int.Parse(col.Value) >= 0)
                {
                    //initial value true
                    if (!value)
                    {
                        var res = int.Parse(col.Value) * -1;
                        col.Value = res.ToString();
                    }

                }
                else
                {
                    //initial value false
                    if (value)
                    {
                        var res = int.Parse(col.Value) * -1;
                        col.Value = res.ToString();
                    }
                }
            }
        }
        public bool IsFrozen
        {
            get
            {
                var AcDbEntity = SubItems(code.SUBCLASSHEADER, "AcDbLayerTableRecord")[0];
                var LayFlags = AcDbEntity.FirstOrDefault(C => C.Code.Trim() == "70");

                if ((int.Parse(LayFlags.Value) & 1) == 1)
                {
                    return true;
                }
                return false;
            }
            set
            {
                var AcDbEntity = SubItems(code.SUBCLASSHEADER, "AcDbLayerTableRecord")[0];
                var LayFlags = AcDbEntity.FirstOrDefault(C => C.Code.Trim() == "70");

                int res = int.Parse(LayFlags.Value);

                if ((res & 1) == 1)
                { // initial value true
                    if (!value)
                    {
                        res &= ~1;
                    }
                } else
                { // initial value false
                    if (value)
                    {
                        res |= 1;
                    }
                }


                LayFlags.Value = res.ToString();

            }
        }
        public bool IsLocked
        {
            get
            {
                var AcDbEntity = SubItems(code.SUBCLASSHEADER, "AcDbLayerTableRecord")[0];
                var LayFlags = AcDbEntity.FirstOrDefault(C => C.Code.Trim() == "70");

                if ((int.Parse(LayFlags.Value) & 4) == 4)
                {
                    return true;
                }
                return false;
            }
            set
            {
                var AcDbEntity = SubItems(code.SUBCLASSHEADER, "AcDbLayerTableRecord")[0];
                var LayFlags = AcDbEntity.FirstOrDefault(C => C.Code.Trim() == "70");

                int res = int.Parse(LayFlags.Value);

                if ((res & 4) == 4)
                { // initial value true
                    if (!value)
                    {
                        res &= ~4;
                    }
                }
                else
                { // initial value false
                    if (value)
                    {
                        res |= 4;
                    }
                }

                LayFlags.Value = res.ToString();
            }
        }
        public bool IsPlotting
        {
            get 
            {
                var AcDbEntity = SubItems(code.SUBCLASSHEADER, "AcDbLayerTableRecord")[0];
                var LayPlot = AcDbEntity.FirstOrDefault(C => C.Code.Trim() == "290");
                
                if(LayPlot != null)
                {
                    return false;
                }
                return true;
            }
            set
            {
                var AcDbEntity = SubItems(code.SUBCLASSHEADER, "AcDbLayerTableRecord")[0];
                var LayPlot = AcDbEntity.FirstOrDefault(C => C.Code.Trim() == "290");
                var LineBeforeLayPlot = AcDbEntity.FirstOrDefault(C => C.Code.Trim() == "6");

                if (LayPlot != null)
                {
                    // if initial value false
                    if (value)
                    {
                        LayPlot.flags |= vpFlag.DELETE;
                    }
                }
                else
                {
                    //if initial value true
                    if (!value)
                    {
                        LineBeforeLayPlot.insertAfterMe.Add(new ValPair { Code = "290", Value = "0" });
                    }

                }
            }
        }
        public string Color
        {
            get
            {
                return GetLayerColor();
            }
            set
            {
                SetLayerColor(value);
            }
        }

        private void SetLayerColor(string value)
        {
            // if new value is RGB
            if (value.Count(c => c == ',') == 2)
            {
                SetLayerColorRGB(value);
                return;
            }
            // if new value is indexed
            if (int.TryParse(value, out int _value))
            {
                SetLayerColorIndexed(value);
                return;
            }
            else
            {
                // if an invalid entry is entered. Do nothing.
                return;
            }
        }

        private string GetLayerColor()
        {
            var AcDbEntity = SubItems(code.SUBCLASSHEADER, "AcDbLayerTableRecord")[0];
            var col = AcDbEntity.FirstOrDefault(C => C.Code.Trim() == code.COLOR);
            var colRGB = AcDbEntity.FirstOrDefault(C => C.Code.Trim() == code.COLORRGB);

            if (colRGB != null)
            {
                Color color = ColorTranslator.FromHtml(colRGB.Value.Trim());
                return color.R + "," + color.G + "," + color.B;
            }

            return col.Value.Trim();
        }

        private void SetLayerColorIndexed(string value)
        {
            var AcDbEntity = SubItems(code.SUBCLASSHEADER, "AcDbLayerTableRecord")[0];
            AcDbEntity.FirstOrDefault(C => C.Code.Trim() == code.COLOR).Value = value;

            // in case, if previous color was rgb
            var rgbColValPair = AcDbEntity.FirstOrDefault(C => C.Code.Trim() == code.COLORRGB);
            if (rgbColValPair != null)
            {
                Data.Remove(rgbColValPair);
                rgbColValPair.flags |= vpFlag.DELETE;
            }
        }

        private void SetLayerColorRGB(string value)
        {
            var AcDbEntity = SubItems(code.SUBCLASSHEADER, "AcDbLayerTableRecord")[0];
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


    }


}




