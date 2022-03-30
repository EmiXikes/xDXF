using System.Linq;


namespace xDXF
{
    #region Strcture Items


    public class Entity : EntityClass
    {
        public string Color
        {
            get
            {
                return GetEntityColor();
            }
            set
            {
                // if new value is ByLayer
                // ByLayer is the default color. Color is asumed ByLayer, if no color entry exists.
                // Therefore any color entries must be removed
                if (value == "ByLayer")
                {
                    var AcDbEntity = SubItems(code.SUBCLASSHEADER, "AcDbEntity")[0];
                    var col = AcDbEntity.FirstOrDefault(C => C.Code.Trim() == code.COLOR);
                    if(col!= null)
                    {
                        Data.Remove(col);
                        col.flags |= vpFlag.DELETE;
                    }
                    var colRGB = AcDbEntity.FirstOrDefault(C => C.Code.Trim() == code.COLORRGB);
                    if (colRGB != null)
                    {
                        Data.Remove(colRGB);
                        colRGB.flags |= vpFlag.DELETE;
                    }
                    return;
                }
                // if new value is ByBlock
                if (value == "ByBlock")
                {
                    var AcDbEntity = SubItems(code.SUBCLASSHEADER, "AcDbEntity")[0];
                    AcDbEntity.FirstOrDefault(C => C.Code.Trim() == code.COLOR).Value = "0";
                    
                    // in case, if previous color was rgb, rgb entry must be removed
                    var colRGB = AcDbEntity.FirstOrDefault(C => C.Code.Trim() == code.COLORRGB);
                    if (colRGB != null)
                    {
                        Data.Remove(colRGB);
                        colRGB.flags |= vpFlag.DELETE;
                    }
                    return;
                }
                // if new value is RGB
                if (value.Count(c => c == ',') == 2)
                {
                    SetEntityColorRGB(value);
                    return;
                }
                // if new value is indexed
                if (int.TryParse(value, out int _value))
                {
                    SetEntityColorIndexed(value);
                    return;
                }
                else
                {
                    // if an invalid entry is entered. Do nothing.
                    return;
                }
            }
        }



        public string Layer
        {
            get
            {
                var AcDbEntity = SubItems("100", "AcDbEntity")[0];
                return AcDbEntity.FirstOrDefault(C => C.Code.Trim() == code.LAYER).Value;
            }
            set
            {
                var AcDbEntity = SubItems("100", "AcDbEntity")[0];
                AcDbEntity.FirstOrDefault(C => C.Code.Trim() == code.LAYER).Value = value;
            }
        }
    }
    #endregion



}




