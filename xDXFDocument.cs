using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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

            DXFVersion = SubItems(SubItems(DataValPairs, "0", "SECTION")[0],"9", "$ACADVER")[0][1].Value;

            BlockRecords = SubItems(DataValPairs, "0", "BLOCK_RECORD");

            Inserts = GetInserts();
            Layers = GetLayers();
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

        public string DXFVersion;
        public List<List<ValPair>> BlockRecords;
        public Dictionary<string, xInsert> Inserts;
        public Dictionary<string, Layer> Layers;

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

            var lyts = SubItems(DataValPairs, "0", "LAYOUT");

            foreach (var lyt in lyts)
            {

                var AcDbLayout = SubItems(lyt, "100", "AcDbLayout");


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

                R.Add(handle, new xInsert
                {
                    hostDoc = this,
                    Data = insert,
                    IsXref = IsXref,
                    XrefPath = XrefPath,
                    XrefResolved = XrefResolved
                });
            }
            return R;
        }

        #endregion



    }

    #region Strcture Items
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

}




