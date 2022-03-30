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




