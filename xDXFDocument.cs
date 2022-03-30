using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static xDXF.xDXFHelperMethods;


namespace xDXF
{
    
    public class code
    {
        #region Code Consts
        public const string HEADER = "0";
        public const string SUBCLASSHEADER = "100";
        public const string HANDLE = "5";
        public const string NAME = "2";
        public const string OWNER = "330";
        public const string ATTRIBUTETAG = "2";
        public const string ATTRIBUTEVALUE = "1";
        public const string COLOR = "62";
        public const string COLORRGB = "420";
        public const string LAYER = "8";
        public const string ROTATION = "50";
        public const string LOCATIONX = "10";
        public const string LOCATIONY = "20";
        public const string LOCATIONZ = "30";
        public const string SCALEX = "41";
        public const string SCALEY = "42";
        public const string SCALEZ = "43";
        #endregion
    }
    [Flags] public enum vpFlag
    {
        DELETE = 1,
        TEST = 2
        // Used flags:
        // 1 - marked for deleletion
        // 2 -
        // 4 - 
        // 8 -
        // 16 -
    }




    public class xDXFDocument
    {
        public string versionDescription = "Epic xDXF 0.4";

        public string[] RawData;
        public List<string> DataStrings;
        public List<ValPair> DataValPairs = new List<ValPair>();

        public string dSep = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

        #region Basic IO

        #region Load File

        public void Open(string FilePath)
        {
            RawData = File.ReadAllLines(FilePath);
            DataStrings = new List<string>();

            ValPair valPair = new ValPair();

            int valPairIndex = 0;

            for (int LineIndex = 0; LineIndex < RawData.Count(); LineIndex += 2)
            {
                valPair.Code = RawData[LineIndex];
                valPair.Value = RawData[LineIndex + 1];

                DataStrings.Add(valPair.Code + " ||| " + valPair.Value);
                DataValPairs.Add(new ValPair() { Code = valPair.Code, Value = valPair.Value, lineIndex = LineIndex });

                valPairIndex++;
            }

            BlockRecords = SubItems(DataValPairs, "0", "BLOCK_RECORD");
        }

        #endregion

        #region Write File

        public void SaveAs(String FilePath)
        {

            List<string> DXFData = new List<string>();

            foreach (var dataValPair in DataValPairs)
            {
                if (!dataValPair.flags.HasFlag(vpFlag.DELETE))
                {
                    DXFData.Add(dataValPair.Code);
                    DXFData.Add(dataValPair.Value);

                    foreach (var newDataValPair in dataValPair.insertAfterMe)
                    {
                        DXFData.Add(newDataValPair.Code);
                        DXFData.Add(newDataValPair.Value);
                    }

                }
            }

            System.IO.File.WriteAllLines(FilePath, DXFData);

        }

        #endregion

        #endregion

        List<List<ValPair>> BlockRecords;

        public Dictionary<string, xInsert> Inserts
        {
            get
            {
                return GetInserts();
            }
        }
        public Dictionary<string, ValPair> HeaderVariables
        {
            get
            {
                return GetHeaderVariables();
            }
        }
        public Dictionary<string, List<ValPair>> Layouts
        {
            get
            {
                return GetLayouts();
            }
        }
        public Dictionary<string, Layer> Layers
        {
            get 
            { 
                return GetLayers(); 
            }
            //set
            //{
            //    layers = value;
            //}
        }



        public Dictionary<string, List<ValPair>> Sections()
        {
            Dictionary<string, List<ValPair>> Result = new Dictionary<string, List<ValPair>>();

            var S = SubItems(DataValPairs, "0", "SECTION");

            foreach (var sect in S)
            {
                Result.Add(sect.FirstOrDefault(C => C.Code.Trim() == "2").Value, sect);
            }

            return Result;
        }

        #region Private Functions
        private Dictionary<string, Layer> GetLayers()
        {
            var Tables = SubItems(DataValPairs, "0", "TABLE");
            var Layers = Tables.FirstOrDefault(n => n.FirstOrDefault(x => x.Code.Trim() == "2").Value == "LAYER");
            var LayersSplit = SubItems(Layers, "0", "LAYER");

            Dictionary<string, Layer> R = new Dictionary<string, Layer>();
            foreach (var layer in LayersSplit)
            {
                var layName = SubItems(layer, code.NAME)[0][0].Value;
                Layer lyr = new Layer() { Data = layer };
                R.Add(layName, lyr);
            }

            return R;
        }
        private Dictionary<string, List<ValPair>> GetLayouts()
        {
            Dictionary<string, List<ValPair>> Result = new Dictionary<string, List<ValPair>>();

            var lyts = SubItems(DataValPairs, "0", "LAYOUT", true);

            foreach (var lyt in lyts)
            {

                var AcDbLayout = SubItems(lyt, "100", "AcDbLayout", true);


                Result.Add(AcDbLayout[0].FirstOrDefault(C => C.Code.Trim() == "1").Value, lyt);
            }

            return Result;
        }
        private Dictionary<string, ValPair> GetHeaderVariables()
        {

            Dictionary<string, ValPair> Result = new Dictionary<string, ValPair>();

            var Sections = SubItems(DataValPairs, "0", "SECTION");

            foreach (List<ValPair> I in SubItems(Sections[0], "9"))
            {
                Result.Add(I[0].Value, I[1]);
            }

            return Result;
        }
        private Dictionary<string, xInsert> GetInserts()
        {
            Dictionary<string, xInsert> R = new Dictionary<string, xInsert>();

            var Sections = SubItems(DataValPairs, "0", "SECTION");
            var Ents = Sections.FirstOrDefault(a => a.FirstOrDefault(x => x.Code.Trim() == "2").Value.Trim() == "ENTITIES");

            var dxfVersion = SubItems(SubItems(DataValPairs, "0", "SECTION")[0], "9", "$ACADVER")[0][1].Value;
            var inserts = SubItems(Ents, "0", "INSERT");

            foreach (var insert in inserts)
            {
                var handle = insert.FirstOrDefault(h => h.Code.Trim() == "5").Value;
                handle = SubItems(insert, "5")[0][0].Value;

                var insertName = insert.FirstOrDefault(c => c.Code.Trim() == code.NAME).Value;
                var insertBlockRecord = BlockRecords.FirstOrDefault(BR => BR.FirstOrDefault(C => C.Code.Trim() == code.NAME).Value == insertName);
                var blockTypeFlag = SubItems(insertBlockRecord, "70")[0][0].Value;

                // xref Check
                bool IsXref = false;
                string XrefPath = "";
                bool XrefResolved = false;
                if ((Int32.Parse(blockTypeFlag) & 4) == 4)
                {
                    IsXref = true;
                    var _xp = SubItems(insertBlockRecord, "1");
                    if (_xp.Count != 0)
                    {
                        XrefPath = _xp[0][0].Value;
                        XrefResolved = true;
                    }
                    else
                    {
                        XrefResolved = false;
                    }

                    //                    var bName = SubItems(insertBlockRecord, "2")[0][0].Value;

                }

                var AttrtbutesInInsert = SubItems(insert, "0", "ATTRIB");

                Dictionary<string, ValPair> AttributeValues = new Dictionary<string, ValPair>();

                if (AttrtbutesInInsert.Count != 0)
                {

                    foreach (List<ValPair> attr in AttrtbutesInInsert)
                    {
                        var item_AcDbAttribute = SubItems(attr, "100", "AcDbAttribute");
                        var item_AcDbText = SubItems(attr, "100", "AcDbText");

                        if (Int32.Parse(dxfVersion.Substring(2)) > 1027)
                        {
                            // dxf version above 2013 (2018+)
                            var item_embeddedObj = SubItems(attr, "101", "Embedded Object");
                            if (item_embeddedObj.Count != 0)
                            {
                                // MultiLine attributes
                                AttributeValues.Add(
                                    item_AcDbAttribute[0].FirstOrDefault(c => c.Code.Trim() == "2").Value,
                                    item_embeddedObj[0].FirstOrDefault(c => c.Code.Trim() == "1"));


                            }
                            else
                            {
                                // SingleLine attributes
                                AttributeValues.Add(
                                    item_AcDbAttribute[0].FirstOrDefault(c => c.Code.Trim() == "2").Value,
                                    item_AcDbText[0].FirstOrDefault(c => c.Code.Trim() == "1"));
                            }
                        }
                        else
                        {
                            // dxf version up to 2013
                            AttributeValues.Add(
                                    item_AcDbAttribute[0].FirstOrDefault(c => c.Code.Trim() == "2").Value,
                                    item_AcDbText[0].FirstOrDefault(c => c.Code.Trim() == "1"));
                        }
                    }
                }

                R.Add(handle, new xInsert
                {
                    Data = insert,
                    IsXref = IsXref,
                    XrefPath = XrefPath,
                    XrefResolved = XrefResolved,
                    _a = AttributeValues,
                    _tn = GetBlockName(insert)
                });
            }
            return R;
        }

        private string GetBlockName(List<ValPair> insert)
        {
            var insertHardOwner = insert.FirstOrDefault(c => c.Code.Trim() == "360");
            var insertName = insert.FirstOrDefault(c => c.Code.Trim() == code.NAME).Value;

            if (insertHardOwner == null)
            {
                return insertName;
            }
            else
            {
                //Method No 1
                var insertBlockRecord = BlockRecords.FirstOrDefault(BR => BR.FirstOrDefault(C => C.Code.Trim() == code.NAME).Value == insertName);
                var sourceBlockRecordHandle = insertBlockRecord.FirstOrDefault(C => C.Code.Trim() == "1005").Value;
                var sourceBlockReocrd = BlockRecords.FirstOrDefault(BR => BR.FirstOrDefault(C => C.Code.Trim() == code.HANDLE).Value == sourceBlockRecordHandle);
                return sourceBlockReocrd.FirstOrDefault(C => C.Code.Trim() == code.NAME).Value;

                //Method No 2
            }
        }

        #endregion



    }


    public class xInsert : Entity
    {
        public string dSep = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        Regex RX = new Regex("[.,]");
        public string Handle
        {
            get
            {
                return ReadCode(code.HANDLE);
            }
        }
        public string Name
        {
            get
            {
                return ReadCode(code.NAME);
            }
        }
        public string TrueName
        {
            get { return _tn; }
        }

        public bool IsXref { get; set; }
        public string XrefPath { get; set; }
        public bool XrefResolved { get; set; }
       
        // Dynamic Properties

        // TODO LineType

        // TODO Attributes not updated correctly when editing block Position, Scale or Rotation.

        /// <summary>
        /// <para> Get or Set block Position </para>
        /// <para> Don't use to set value on blocks with attributes. For now Block attributes are not updated correctly when changing block Position, Scale or Rotation. </para>
        /// </summary>
        public Vector3 Postion
        {
            get
            {
                var AcDbBlockReference = SubItems("100", "AcDbBlockReference")[0];
                postion.X = float.Parse(RX.Replace(AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "10").Value, dSep));
                postion.Y = float.Parse(RX.Replace(AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "20").Value, dSep));
                postion.Z = float.Parse(RX.Replace(AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "30").Value, dSep));
                return postion;
            }
            set
            {
                // TODO If block has attributes, their position is not updated correctly. ATTSYNC fixes it, but it's not ideal.
                // This needs to be fixed, by changing attribute positions as well.
                // New attribute positions need to be calculated with offset from data in block definition.

                var AcDbBlockReference = SubItems("100", "AcDbBlockReference")[0];
                AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "10").Value = value.X.ToString().Replace(",", ".");
                AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "20").Value = value.Y.ToString().Replace(",", ".");
                AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "30").Value = value.Z.ToString().Replace(",", ".");

                //postion = value;
            }
        }

        /// <summary>
        /// <para> Get or Set block Scale </para>
        /// <para> Don't use to set value on blocks with attributes. For now Block attributes are not updated correctly when changing block Position, Scale or Rotation. </para>
        /// </summary>
        public Vector3 Scale
        {
            get
            {
                var AcDbBlockReference = SubItems("100", "AcDbBlockReference")[0];
                var scValPair = AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "41");

                if (scValPair != null)
                {
                    scale.X = float.Parse(RX.Replace(AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "41").Value,dSep));
                    scale.Y = float.Parse(RX.Replace(AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "42").Value,dSep));
                    scale.Z = float.Parse(RX.Replace(AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "43").Value,dSep));
                }
                else
                {
                    scale.X = 1;
                    scale.Y = 1;
                    scale.Z = 1;
                }
                return scale;
            }
            set
            {
                var AcDbBlockReference = SubItems("100", "AcDbBlockReference")[0];
                var scValPair = AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "41");
                if (scValPair != null)
                {
                    AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "41").Value = value.X.ToString().Replace(",", ".");
                    AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "42").Value = value.Y.ToString().Replace(",", ".");
                    AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "43").Value = value.Z.ToString().Replace(",", ".");
                }

                //  scale = value;
            }
        }

        /// <summary>
        /// <para> Get or Set block Rotation </para>
        /// <para> Don't use to set value on blocks with attributes. For now Block attributes are not updated correctly when changing block Position, Scale or Rotation. </para>
        /// </summary>
        public float Rotation
        {
            get
            {
                var AcDbBlockReference = SubItems("100", "AcDbBlockReference")[0];
                var rotValpair = AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "50");
                string rot;
                if (rotValpair != null)
                {
                    rot = RX.Replace(rotValpair.Value, dSep);
                }
                else
                {
                    rot = "0";
                }
                rotation = float.Parse(rot);
                return rotation;
            }
            set
            {
                // TODO If block has attributes, their rotation is not updated correctly. ATTSYNC fixes it, but it's not ideal.
                // This needs to be fixed, by changing attribute rotation as well.
                // New attribute rotation needs to be calculated with offset from data in block definition.
                var AcDbBlockReference = SubItems("100", "AcDbBlockReference")[0];
                var rotValpair = AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "50");
                if (rotValpair != null)
                {
                    if (value == 0)
                    {
                        Data.Remove(rotValpair);
                        rotValpair.flags |= vpFlag.DELETE;
                    }
                    else 
                    {
                        rotValpair.Value = value.ToString();
                    }
                }
                else
                {
                    var poszValPair = AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "30");
                    poszValPair.insertAfterMe.Add(new ValPair { Code = "50", Value = value.ToString() });
                    // TODO If original rotation was 0, the ValPair entry does not exist.
                    // Need to add these lines to the file and test if it still works.
                }
            }
        }

        public List<List<ValPair>> Attributes()
        {
            return SubItems("0", "ATTRIB");
        }

        /// <summary>
        /// <para> Attribute values. Values can be changed. To get other attribute data, use method Attributes(). </para>
        /// </summary>
        public Dictionary<string, ValPair> AttributeValues { get => _a; }
        /// <summary>
        /// <para> READ ONLY!!! Attribute values. Joined multiline values (for dwg versions lower than 2018). </para>
        /// </summary>
        public Dictionary<string, ValPair> AttributeValuesJoined 
        {
            // TODO implement joining..
            get => _a; 
        }

        #region Constructor stuff and privates

        public xInsert()
        {
            _a = new Dictionary<string, ValPair>();
        }

        public Dictionary<string, ValPair> _a;
        public string _tn;

        private Vector3 scale;
        private Vector3 postion;
        private float rotation;
        #endregion

    }
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

                res |= 1;

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

                res |= 4;

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
    public class EntityClass
    {
        public List<ValPair> Data;

        public EntityClass()
        {
            Data = new List<ValPair>();
        }

        public List<List<ValPair>> SubItems(string subItemCode, string subItemValue = "")
        {
            return xDXFHelperMethods.SubItems(Data, subItemCode, subItemValue);

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
    public class EntitySubClass : EntityClass
    {

    }
    public class ValPair
    {
        public string Code;
        public string Value;

        public int lineIndex;

        public List<ValPair> insertAfterMe = new List<ValPair>();

        public vpFlag flags;





    }
    #endregion



    #region OLDSTUFF

    //private static Random random = new Random();
    //private static string RandomString(int length)
    //{
    //    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    //    return new string(Enumerable.Repeat(chars, length)
    //      .Select(s => s[random.Next(s.Length)]).ToArray());
    //}
    //public enum LineType
    //{
    //    Code,
    //    Value
    //}
    //public enum EntitySaveTarget
    //{
    //    SectionData,
    //    GroupData,
    //    SubClassData
    //}

    //public void Load(string FilePath)
    //{
    //    RawData = File.ReadAllLines(FilePath);
    //    Data = new Dictionary<string, Section>();
    //    DataStrings = new List<string>();

    //    LineType lineType = LineType.Code;
    //    ValPair valPair = new ValPair();
    //    Section section = null;

    //    Dictionary<string, Entity> TempEntityGroup = new Dictionary<string, Entity>();

    //    Entity TempEntity = new Entity();
    //    EntitySubClass TempSubClassEntity = new EntitySubClass();

    //    bool isGroupEnding = false;
    //    bool isSectionEnding = false;

    //    bool isSubClassData = false;
    //    string SubClassEntityName = "";

    //    EntitySaveTarget entitySaveTarget = EntitySaveTarget.SectionData;

    //    for (int LineIndex = 0; LineIndex < RawData.Count(); LineIndex++)
    //    {
    //        string Line = RawData[LineIndex];


    //        // Alternate between Code and Value. Use ENUM LineType.
    //        if (lineType == LineType.Code)
    //        {
    //            LineNo += 1;
    //            lineType = LineType.Value;
    //            valPair.Code = Line;
    //        }
    //        else if (lineType == LineType.Value)
    //        {
    //            LineNo += 1;
    //            lineType = LineType.Code;
    //            valPair.Value = Line;
    //            DataStrings.Add(valPair.Code + " ||| " + valPair.Value);

    //            // After code and value are stored, perform additional sorting

    //            // Reached new Header
    //            if (valPair.Code.Trim() == CONST.HEADER)
    //            {
    //                // ++++++++++++++++++++++++                       
    //                // ++++++++++++++++++++++++
    //                // END OF PREVIOUS
    //                // Before processing the new entity, previous data must be saved
    //                // Finalizing and saving previous data
    //                // Using flags set at the start of the previous data header

    //                if (TempEntity.Data.Count != 0)
    //                {
    //                    // Adding previous subclass to current entity
    //                    if (isSubClassData)
    //                    {
    //                        TempEntity.SubClassItems.Add(SubClassEntityName, TempSubClassEntity);
    //                        TempSubClassEntity = new EntitySubClass();
    //                        isSubClassData = false;
    //                    }

    //                    // Finalizing Entity and saving it to selected destination
    //                    string EntityHandle = FindByCode(CONST.HANDLE, TempEntity.Data);
    //                    if (entitySaveTarget == EntitySaveTarget.GroupData)
    //                    {
    //                        TempEntityGroup.Add(EntityHandle, TempEntity);
    //                    }
    //                    else if (entitySaveTarget == EntitySaveTarget.SectionData)
    //                    {
    //                        section.Entities.Add(EntityHandle, TempEntity);
    //                    }

    //                    // Finalizing Group, if group is ending
    //                    if (isGroupEnding == true)
    //                    {
    //                        isGroupEnding = false;
    //                        string GroupdHandle = FindByCode(CONST.HANDLE, TempEntityGroup.Values.ToList()[0].Data);
    //                        section.EntityGroups.Add(GroupdHandle, TempEntityGroup);
    //                    }

    //                    // Finalizing Section, if section is ending
    //                    else if (isSectionEnding == true)
    //                    {
    //                        isSectionEnding = false;
    //                        string SectionName = FindByCode(CONST.NAME, section.Entities.Values.ToList()[0].Data);
    //                        Data.Add(SectionName, section);
    //                    }
    //                }


    //                //
    //                // START OF NEW
    //                // Processing the new entity.
    //                //
    //                string HeaderName = valPair.Value.Trim();
    //                string HeaderCode = valPair.Code.Trim();

    //                TempEntity = new Entity();
    //                TempEntity.Data.Add(new ValPair { Code = valPair.Code, Value = valPair.Value });

    //                // Checking current header type and setting flags for execution before the start of the next header. see above.

    //                if (HeaderCode == CONST.HEADER)
    //                {
    //                    if (HeaderName == "SECTION")
    //                    {
    //                        section = new Section();
    //                        entitySaveTarget = EntitySaveTarget.SectionData;
    //                    }
    //                    else if (HeaderName == "BLOCK" || HeaderName == "TABLE")
    //                    {
    //                        TempEntityGroup = new Dictionary<string, Entity>();
    //                        entitySaveTarget = EntitySaveTarget.GroupData;
    //                    }
    //                    else if (HeaderName == "ENDSEC")
    //                    {
    //                        isSectionEnding = true;
    //                    }
    //                    else if (HeaderName == "ENDBLK" || HeaderName == "ENDTAB")
    //                    {
    //                        isGroupEnding = true;
    //                    }
    //                    else if (HeaderName == "EOF")
    //                    {
    //                        // Finalize File
    //                        section = new Section();
    //                        TempEntity = new Entity();
    //                        TempEntity.Data.Add(new ValPair { Code = valPair.Code, Value = valPair.Value });
    //                        section.Entities.Add("EOF", TempEntity);
    //                        Data.Add("EOF", section);
    //                    }
    //                }

    //            }
    //            else
    //            {
    //                // Not a Header


    //                // If subclass detected

    //                if (valPair.Code.Trim() == CONST.SUBCLASSHEADER)
    //                {
    //                    if (TempSubClassEntity.Data.Count != 0)
    //                    {
    //                        TempEntity.SubClassItems.Add(SubClassEntityName, TempSubClassEntity);
    //                    }
    //                    SubClassEntityName = valPair.Value + "__" + RandomString(10);
    //                    TempSubClassEntity = new EntitySubClass();
    //                    isSubClassData = true;
    //                }


    //                //Writing data values to entity
    //                if (isSubClassData)
    //                {
    //                    TempSubClassEntity.Data.Add(new ValPair { Code = valPair.Code, Value = valPair.Value });
    //                } else
    //                {
    //                    TempEntity.Data.Add(new ValPair { Code = valPair.Code, Value = valPair.Value });
    //                }


    //            }
    //        }
    //    }
    //}

    //public void Load_1_5(string FilePath)
    //{
    //    RawData = File.ReadAllLines(FilePath);
    //    Data = new Dictionary<string, Section>();
    //    DataStrings = new List<string>();

    //    LineType lineType = LineType.Code;
    //    ValPair valPair = new ValPair();
    //    Section section = null;

    //    Dictionary<string, Entity> TempEntityGroup = new Dictionary<string, Entity>();

    //    Entity TempEntity = new Entity();
    //    EntitySubClass TempSubClassEntity = new EntitySubClass();

    //    bool isGroupEnding = false;
    //    bool isSectionEnding = false;

    //    bool isSubClassData = false;
    //    string SubClassEntityName = "";

    //    EntitySaveTarget entitySaveTarget = EntitySaveTarget.SectionData;

    //    for (int LineIndex = 0; LineIndex < RawData.Count(); LineIndex++)
    //    {
    //        string Line = RawData[LineIndex];


    //        // Alternate between Code and Value. Use ENUM LineType.
    //        if (lineType == LineType.Code)
    //        {
    //            LineNo += 1;
    //            lineType = LineType.Value;
    //            valPair.Code = Line;
    //        }
    //        else if (lineType == LineType.Value)
    //        {
    //            LineNo += 1;
    //            lineType = LineType.Code;
    //            valPair.Value = Line;
    //            DataStrings.Add(valPair.Code + " ||| " + valPair.Value);
    //            DataValPairs.Add(new ValPair() { Code = valPair.Code, Value = valPair.Value }) ;

    //            // After code and value are stored, perform additional sorting

    //            // Reached new Header
    //            if (valPair.Code.Trim() == CONST.HEADER)
    //            {
    //                // ++++++++++++++++++++++++                       
    //                // ++++++++++++++++++++++++
    //                // END OF PREVIOUS
    //                // Before processing the new entity, previous data must be saved
    //                // Finalizing and saving previous data
    //                // Using flags set at the start of the previous data header

    //                if (TempEntity.Data.Count != 0)
    //                {
    //                    // Adding previous subclass to current entity
    //                    //if (isSubClassData)
    //                    //{
    //                    //    TempEntity.SubClassItems.Add(SubClassEntityName, TempSubClassEntity);
    //                    //    TempSubClassEntity = new EntitySubClass();
    //                    //    isSubClassData = false;
    //                    //}

    //                    // Finalizing Entity and saving it to selected destination
    //                    string EntityHandle = FindByCode(CONST.HANDLE, TempEntity.Data);
    //                    if (entitySaveTarget == EntitySaveTarget.GroupData)
    //                    {
    //                        TempEntityGroup.Add(EntityHandle, TempEntity);
    //                    }
    //                    else if (entitySaveTarget == EntitySaveTarget.SectionData)
    //                    {
    //                        section.Entities.Add(EntityHandle, TempEntity);
    //                    }

    //                    // Finalizing Group, if group is ending
    //                    if (isGroupEnding == true)
    //                    {
    //                        isGroupEnding = false;
    //                        string GroupdHandle = FindByCode(CONST.HANDLE, TempEntityGroup.Values.ToList()[0].Data);
    //                        section.EntityGroups.Add(GroupdHandle, TempEntityGroup);
    //                    }

    //                    // Finalizing Section, if section is ending
    //                    else if (isSectionEnding == true)
    //                    {
    //                        isSectionEnding = false;
    //                        string SectionName = FindByCode(CONST.NAME, section.Entities.Values.ToList()[0].Data);
    //                        Data.Add(SectionName, section);
    //                    }
    //                }


    //                //
    //                // START OF NEW
    //                // Processing the new entity.
    //                //
    //                string HeaderName = valPair.Value.Trim();
    //                string HeaderCode = valPair.Code.Trim();

    //                TempEntity = new Entity();
    //                TempEntity.Data.Add(new ValPair { Code = valPair.Code, Value = valPair.Value });

    //                // Checking current header type and setting flags for execution before the start of the next header. see above.

    //                if (HeaderCode == CONST.HEADER)
    //                {
    //                    if (HeaderName == "SECTION")
    //                    {
    //                        section = new Section();
    //                        entitySaveTarget = EntitySaveTarget.SectionData;
    //                    }
    //                    else if (HeaderName == "BLOCK" || HeaderName == "TABLE")
    //                    {
    //                        TempEntityGroup = new Dictionary<string, Entity>();
    //                        entitySaveTarget = EntitySaveTarget.GroupData;
    //                    }
    //                    else if (HeaderName == "ENDSEC")
    //                    {
    //                        isSectionEnding = true;
    //                    }
    //                    else if (HeaderName == "ENDBLK" || HeaderName == "ENDTAB")
    //                    {
    //                        isGroupEnding = true;
    //                    }
    //                    else if (HeaderName == "EOF")
    //                    {
    //                        // Finalize File
    //                        section = new Section();
    //                        TempEntity = new Entity();
    //                        TempEntity.Data.Add(new ValPair { Code = valPair.Code, Value = valPair.Value });
    //                        section.Entities.Add("EOF", TempEntity);
    //                        Data.Add("EOF", section);
    //                    }
    //                }

    //            }
    //            else
    //            {
    //                // Not a Header


    //                // If subclass detected

    //                //if (valPair.Code.Trim() == CONST.SUBCLASSHEADER)
    //                //{
    //                //    if (TempSubClassEntity.Data.Count != 0)
    //                //    {
    //                //        TempEntity.SubClassItems.Add(SubClassEntityName, TempSubClassEntity);
    //                //    }
    //                //    SubClassEntityName = valPair.Value + "__" + RandomString(10);
    //                //    TempSubClassEntity = new EntitySubClass();
    //                //    isSubClassData = true;
    //                //}


    //                //Writing data values to entity
    //                //if (isSubClassData)
    //                //{
    //                //    TempSubClassEntity.Data.Add(new ValPair { Code = valPair.Code, Value = valPair.Value });
    //                //}
    //                //else
    //                //{
    //                    TempEntity.Data.Add(new ValPair { Code = valPair.Code, Value = valPair.Value });
    //                //}


    //            }
    //        }
    //    }
    //}

    //public void Write(string FilePath)
    //{
    //    List<string> DXFData = new List<string>();

    //    // Write Section
    //    for (int writableSectionIndex = 0; writableSectionIndex < Data.Count; writableSectionIndex++)
    //    {
    //        Section writableSection = Data.Values.ToList()[writableSectionIndex];

    //        // Write Entity
    //        for (int sectionEntityIndex = 0; sectionEntityIndex < writableSection.Entities.Count; sectionEntityIndex++)
    //        {
    //            // Write Entity Data
    //            var sectionEntity = writableSection.Entities.Values.ToList()[sectionEntityIndex];
    //            for (int valPairIndex = 0; valPairIndex < sectionEntity.Data.Count; valPairIndex++)
    //            {
    //                DXFData.Add(sectionEntity.Data[valPairIndex].Code);
    //                DXFData.Add(sectionEntity.Data[valPairIndex].Value);
    //            }

    //            // Write Entity Subclass Data
    //            for (int subClassItemIndex = 0; subClassItemIndex < sectionEntity.SubClassItems.Count; subClassItemIndex++)
    //            {
    //                var subClassEntity = sectionEntity.SubClassItems.Values.ToList()[subClassItemIndex];
    //                for (int valPairIndex = 0; valPairIndex < subClassEntity.Data.Count; valPairIndex++)
    //                {
    //                    DXFData.Add(subClassEntity.Data[valPairIndex].Code);
    //                    DXFData.Add(subClassEntity.Data[valPairIndex].Value);
    //                }
    //            }
    //        }

    //        // Write Entity Groups
    //        var sectionGroupedEntities = writableSection.EntityGroups.Values.ToList();
    //        for (int sectionGroupedEntityIndex = 0; sectionGroupedEntityIndex < sectionGroupedEntities.Count; sectionGroupedEntityIndex++)
    //        {

    //            // Write Entity
    //            var GroupedEntity = sectionGroupedEntities[sectionGroupedEntityIndex].Values.ToList();
    //            for (int sectionEntityIndex = 0; sectionEntityIndex < GroupedEntity.Count; sectionEntityIndex++)
    //            {
    //                // Write Entity Data
    //                var sectionEntity = GroupedEntity[sectionEntityIndex];
    //                for (int valPairIndex = 0; valPairIndex < sectionEntity.Data.Count; valPairIndex++)
    //                {
    //                    DXFData.Add(sectionEntity.Data[valPairIndex].Code);
    //                    DXFData.Add(sectionEntity.Data[valPairIndex].Value);
    //                }

    //                // Write Entity Subclass Data
    //                for (int subClassItemIndex = 0; subClassItemIndex < sectionEntity.SubClassItems.Count; subClassItemIndex++)
    //                {
    //                    var subClassEntity = sectionEntity.SubClassItems.Values.ToList()[subClassItemIndex];
    //                    for (int valPairIndex = 0; valPairIndex < subClassEntity.Data.Count; valPairIndex++)
    //                    {
    //                        DXFData.Add(subClassEntity.Data[valPairIndex].Code);
    //                        DXFData.Add(subClassEntity.Data[valPairIndex].Value);
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    System.IO.File.WriteAllLines(FilePath, DXFData);
    //}

    //public void Write_1_5(String FilePath)
    //{
    //    List<string> DXFData = new List<string>();

    //    foreach (var dataValPair in DataValPairs)
    //    {
    //        DXFData.Add(dataValPair.Code);
    //        DXFData.Add(dataValPair.Value);
    //    }

    //    System.IO.File.WriteAllLines(FilePath, DXFData);


    //}

    //public class Section
    //{
    //    public Dictionary<string, Entity> Entities; //For Entities such as INSERT, PLINE, ect.

    //    public Dictionary<string, Dictionary<string, Entity>> EntityGroups; //For entity groups, such as BLOCK definitions

    //    public Section()
    //    {
    //        Entities = new Dictionary<string, Entity>();
    //        EntityGroups = new Dictionary<string, Dictionary<string, Entity>>();
    //    }
    //}

    //public Dictionary<string,xInsert> xInserts()
    //{
    //    Dictionary<string, xInsert> R = new Dictionary<string, xInsert>();
    //    var inserts = (from Entity in Data["ENTITIES"].Entities.Values where Entity.ReadCode(CONST.HEADER) == "INSERT" select Entity).ToList();
    //    var allAttributes = (from Entity in Data["ENTITIES"].Entities.Values where Entity.ReadCode(CONST.HEADER) == "ATTRIB" select Entity).ToList();

    //    foreach (var insert in inserts)
    //    {
    //        // Full attribute entity
    //        var brefAttributes = (from attribute in allAttributes where attribute.ReadCode(CONST.OWNER) == insert.ReadCode(CONST.HANDLE) select attribute)
    //            .ToDictionary(tag => tag.SubClassItems.FirstOrDefault(attrData => attrData.Value.ReadCode(CONST.SUBCLASSHEADER) == "AcDbAttribute").Value.ReadCode(CONST.ATTRIBUTETAG));

    //        Dictionary<string, ValPair> brefAttributeValues = new Dictionary<string, ValPair>();
    //        Dictionary<string, ValPair> brefAttributeValues1 = new Dictionary<string, ValPair>();

    //        //TODO 
    //        // Attribute values are stored in a fucked-up way.
    //        // First we need to check AcDbAttribute code 3. If it exists, we use that.
    //        // If it doesn't exist, we check AcDbAttribute code 1. If it exists, we use that.
    //        // If it doesn't exist, we use AcDbText code 1.

    //        //TODO
    //        // Need to add support for older DXF versions, where multiline attributes are split into seperate ones by using TAG_001, TAG_002, TAG_003, ect.

    //        foreach (var a in brefAttributes)
    //        {
    //            string attrTag = a.Value.SubClassItems.FirstOrDefault(attrData => attrData.Value.ReadCode("100") == "AcDbAttribute").Value.ReadCode("2");

    //            //This is shit.

    //            var attrValPair = a.Value.SubClassItems.FirstOrDefault(attrData => attrData.Value.ReadCode("100") == "AcDbAttribute").Value.Data.FirstOrDefault(x=>x.Code.Trim() == "3");
    //            var attrValPair1 = a.Value.SubClassItems.FirstOrDefault(attrData => attrData.Value.ReadCode("100") == "AcDbAttribute").Value.Data.FirstOrDefault(x => x.Code.Trim() == "1");

    //            if (attrValPair == null)
    //            {
    //                attrValPair = a.Value.SubClassItems.FirstOrDefault(attrData => attrData.Value.ReadCode("100") == "AcDbAttribute").Value.Data.FirstOrDefault(x => x.Code.Trim() == "1");

    //                if (attrValPair == null)
    //                {
    //                    attrValPair = a.Value.SubClassItems.FirstOrDefault(attrData => attrData.Value.ReadCode("100") == "AcDbText").Value.Data.FirstOrDefault(x => x.Code.Trim() == "1");
    //                }

    //            }

    //            brefAttributeValues.Add(attrTag, attrValPair);
    //            brefAttributeValues1.Add(attrTag, attrValPair1);

    //        }



    //        // Simplified attribute data (Tag/Value only)


    //        //var brefAttributeValues = (from ATTR in allAttributes where ATTR.ReadCode(CONST.OWNER) == insert.ReadCode(CONST.HANDLE) select ATTR).ToDictionary(
    //        //    attrTag => attrTag.SubClassItems.FirstOrDefault(attrData => attrData.Value.ReadCode(CONST.SUBCLASSHEADER) == "AcDbAttribute")
    //        //    .Value.ReadCode(CONST.ATTRIBUTETAG),
    //        //    attrValue => attrValue.SubClassItems.FirstOrDefault(attrTxtData => attrTxtData.Value.ReadCode(CONST.SUBCLASSHEADER) == "AcDbText")
    //        //    .Value.Data.FirstOrDefault(valuePair => valuePair.Code.Trim() == CONST.ATTRIBUTEVALUE));




    //        // TODO other properties



    //        // Adding to Result
    //        R.Add(insert.ReadCode(CONST.HANDLE), new xInsert { 
    //            Data = insert.Data,
    //            SubClassItems = insert.SubClassItems,
    //            attributes = brefAttributes, 
    //            attributeValues = brefAttributeValues,
    //            attributeValues1 = brefAttributeValues1
    //        });
    //    }

    //    return R;
    //    }

    //public string FindByCode(string Code, List<ValPair> Data)
    //{
    //    string R = "";

    //    var res = (from x in Data where x.Code.Trim() == Code select x.Value);

    //    if (res != null && res.Count() != 0)
    //    {
    //        R = res.ToList()[0];
    //    }
    //    else
    //    {
    //        R = RandomString(20);
    //    }

    //    return R;
    //}
    #endregion

}




