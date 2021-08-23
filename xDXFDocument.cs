using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static xDXF.xDXFHelperMethods;


namespace xDXF
{
  public class CONST
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

    public class xDXFDocument
    {
        public string versionDescription = "Epic xDXF 0.4";
        
        public string[] RawData;
        public List<string> DataStrings;
        public List<ValPair> DataValPairs = new List<ValPair>();

        #region Basic IO

        #region Load File

        public void Load2(string FilePath)
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
        }

        #endregion

        #region Write File

        public void Write2(String FilePath)
        {
            List<string> DXFData = new List<string>();

            foreach (var dataValPair in DataValPairs)
            {
                DXFData.Add(dataValPair.Code);
                DXFData.Add(dataValPair.Value);
            }

            System.IO.File.WriteAllLines(FilePath, DXFData);

        }

        #endregion

        #endregion

        public Dictionary<string, xInsert> Inserts()
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

                R.Add(handle, new xInsert { Data = insert, _attributeValues = AttributeValues });
            }
            return R;
        }

        public Dictionary<string, ValPair> HeaderVariables()
        {

            Dictionary<string, ValPair> Result = new Dictionary<string, ValPair>();

            var Sections = SubItems(DataValPairs, "0", "SECTION");

            foreach (List<ValPair> I in SubItems(Sections[0], "9"))
            {
                Result.Add(I[0].Value, I[1]);
            }

            return Result;
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

        public Dictionary<string, List<ValPair>> Layouts()
        {
            Dictionary<string, List<ValPair>> Result = new Dictionary<string, List<ValPair>>();

            var lyts =  SubItems(DataValPairs, "0", "LAYOUT", true);

            foreach (var lyt in lyts)
            {

                var AcDbLayout = SubItems(lyt, "100", "AcDbLayout", true);


                Result.Add(AcDbLayout[0].FirstOrDefault(C => C.Code.Trim() == "1").Value, lyt);
            }

            return Result;
        }



    }




    public class xInsert : Entity
    {
        public string Handle
        {
            get
            {
                return ReadCode(CONST.HANDLE);
            }
        }
        public string Name
        {
            get
            {
                return ReadCode(CONST.NAME);
            }
        }

        public Dictionary<string, ValPair> AttributeValues { get => _attributeValues;}

        public List<List<ValPair>> Attributes()
        {
            return SubItems("0", "ATTRIB");
        }

        public Dictionary<string, ValPair> _attributeValues;

        public xInsert()
        {
            _attributeValues = new Dictionary<string, ValPair>();
        }


    }






    #region Strcture Items
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

    }



    public class Entity :EntityClass
    {

    }


    public class EntitySubClass: EntityClass
    {

    }


    public class ValPair
    {
        public int lineIndex;
        public string Code;
        public string Value;
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




