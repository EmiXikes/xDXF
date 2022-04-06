using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;


namespace xDXF
{
    public class xInsert : Entity
    {
        public xDXFDocument hostDoc { get; set; }
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
            get { return GetTrueName(); }
        }

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
                var AcDbBlockReference = SubItems("100", "AcDbBlockReference", new[] {"0","100"} )[0];
                var scValPair = AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "41");

                if (scValPair != null)
                {
                    var scVP = AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "41");
                    scale.X = scVP != null ? float.Parse(RX.Replace(scVP.Value, dSep)) : 1;

                    scVP = AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "42");
                    scale.Y = scVP != null ? float.Parse(RX.Replace(scVP.Value, dSep)) : 1;

                    scVP = AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "43");
                    scale.Z = scVP != null ? float.Parse(RX.Replace(scVP.Value, dSep)) : 1;

                    //scale.X = float.Parse(RX.Replace(AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "41").Value,dSep));
                    //scale.Y = float.Parse(RX.Replace(AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "42").Value,dSep));
                    //scale.Z = float.Parse(RX.Replace(AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "43").Value,dSep));
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
                var AcDbBlockReference = SubItems("100", "AcDbBlockReference", new[] { "0", "100" })[0];
                var scValPair = AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "41");
                if (scValPair != null)
                {
                    AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "41").Value = value.X.ToString().Replace(",", ".");

                    var scVP = AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "42");
                    if (scVP != null) { scVP.Value = value.Y.ToString().Replace(",", "."); }

                    scVP = AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "43");
                    if (scVP != null) { scVP.Value = value.Y.ToString().Replace(",", "."); }

                    //AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "42").Value = value.Y.ToString().Replace(",", ".");
                    //AcDbBlockReference.FirstOrDefault(C => C.Code.Trim() == "43").Value = value.Z.ToString().Replace(",", ".");
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

        /// <summary>
        /// <para> Attribute values. Only single-line attributes can be set. </para>
        /// <para> Multiline attributes can be read, but will not be set. </para>
        /// </summary>
        public Dictionary<string, string> AttributeValues
        {
            get
            {
                return GetAttributeValues();
            }
            set
            {
                SetAttributeValues(value);
            }
        }

        public bool IsXref { get; set; }
        public string XrefPath { get; set; }
        public bool XrefResolved { get; set; }

        #region privates
        private string dSep = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        private Regex RX = new Regex("[.,]");

        private string GetTrueName()
        {
            var insertHardOwner = Data.FirstOrDefault(c => c.Code.Trim() == "360");
            var insertName = Data.FirstOrDefault(c => c.Code.Trim() == code.NAME).Value;

            if (insertHardOwner == null || !insertName.StartsWith("*"))
            {
                return insertName;
            }
            else
            {
                //Method No 1
                var insertBlockRecord = hostDoc.BlockRecords.FirstOrDefault(BR => BR.FirstOrDefault(C => C.Code.Trim() == code.NAME).Value == insertName);
                var sourceBlockRecordHandle = insertBlockRecord.FirstOrDefault(C => C.Code.Trim() == "1005").Value;
                var sourceBlockReocrd = hostDoc.BlockRecords.FirstOrDefault(BR => BR.FirstOrDefault(C => C.Code.Trim() == code.HANDLE).Value == sourceBlockRecordHandle);
                return sourceBlockReocrd.FirstOrDefault(C => C.Code.Trim() == code.NAME).Value;

                //Method No 2
            }
            throw new NotImplementedException();
        }
        private void SetAttributeValues(Dictionary<string, string> value)
        {
            List<List<ValPair>> AttributesInInsert = SubItems("0", "ATTRIB");

            foreach (var newAttr in value)
            {
                if (Int32.Parse(hostDoc.DXFVersion.Substring(2)) > 1027)
                {
                    // dxf version above 2013 (2018+)
                    var insAttr = (from A in AttributesInInsert
                                   where xDXFHelperMethods.SubItems(A, "100", "AcDbAttribute")[0]
                                   .FirstOrDefault(c => c.Code.Trim() == "2").Value == newAttr.Key
                                   select A).ToList();
                    if (insAttr.Count == 0) continue;

                    var item_AcDbAttribute = xDXFHelperMethods.SubItems(insAttr[0], "100", "AcDbAttribute");
                    var item_AcDbText = xDXFHelperMethods.SubItems(insAttr[0], "100", "AcDbText");
                    var item_embeddedObj = xDXFHelperMethods.SubItems(insAttr[0], "101", "Embedded Object");

                    if (item_embeddedObj.Count != 0)
                    {
                        // MultiLine attributes
                        // not supported
                    }
                    else
                    {
                        // SingleLine attributes
                        item_AcDbText[0].FirstOrDefault(c => c.Code.Trim() == "1").Value = newAttr.Value;
                    }
                }
                else
                {
                    // dxf version up to 2013
                    var insAttr = (from A in AttributesInInsert
                                   where xDXFHelperMethods.SubItems(A, "100", "AcDbAttribute")[0]
                                   .FirstOrDefault(c => c.Code.Trim() == "2").Value == newAttr.Key
                                   select A).ToList();
                    if (insAttr.Count == 0) continue;
                    if (insAttr.Count > 1)
                    {
                        // Multiline attributes
                        // not supported
                    }
                    if (insAttr.Count == 1)
                    {
                        // SingleLine attributes
                        var item_AcDbAttribute = xDXFHelperMethods.SubItems(insAttr[0], "100", "AcDbAttribute");
                        var item_AcDbText = xDXFHelperMethods.SubItems(insAttr[0], "100", "AcDbText");
                        item_AcDbText[0].FirstOrDefault(c => c.Code.Trim() == "1").Value = newAttr.Value;
                    }


                }

            }

        }

        private Dictionary<string, string> GetAttributeValues()
        {
            var AttrtbutesInInsert = SubItems("0", "ATTRIB");

            Dictionary<string, string> AttributeValues = new Dictionary<string, string>();

            if (AttrtbutesInInsert.Count != 0)
            {
                foreach (List<ValPair> attr in AttrtbutesInInsert)
                {
                    var item_AcDbAttribute = xDXFHelperMethods.SubItems(attr, "100", "AcDbAttribute");
                    var item_AcDbText = xDXFHelperMethods.SubItems(attr, "100", "AcDbText");

                    if (Int32.Parse(hostDoc.DXFVersion.Substring(2)) > 1027)
                    {
                        // dxf version above 2013 (2018+)
                        var item_embeddedObj = xDXFHelperMethods.SubItems(attr, "101", "Embedded Object");
                        if (item_embeddedObj.Count != 0)
                        {
                            // MultiLine attributes

                            var attrValue = (from C in item_embeddedObj[0] where C.Code.Trim() == "3" select C.Value).ToList();
                            var attrValueEnd = item_embeddedObj[0].FirstOrDefault(c => c.Code.Trim() == "1");
                            if (attrValueEnd != null)
                            {
                                attrValue.Add(attrValueEnd.Value);
                            }

                            var attrValueFull = String.Join("", attrValue);

                            AttributeValues.Add(
                                item_AcDbAttribute[0].FirstOrDefault(c => c.Code.Trim() == "2").Value,
                                attrValueFull);

                        }
                        else
                        {
                            // SingleLine attributes
                            AttributeValues.Add(
                                item_AcDbAttribute[0].FirstOrDefault(c => c.Code.Trim() == "2").Value,
                                item_AcDbText[0].FirstOrDefault(c => c.Code.Trim() == "1").Value);
                        }
                    }
                    else
                    {
                        // dxf version up to 2013

                        var attrTAG = item_AcDbAttribute[0].FirstOrDefault(c => c.Code.Trim() == "2").Value;
                        var attrVALUE = item_AcDbText[0].FirstOrDefault(c => c.Code.Trim() == "1").Value;

                        var MLAttributePattern = Regex.Match(attrTAG, @"[A-Za-z]*_\d\d\d");
                        if (MLAttributePattern.Success)
                        {
                            attrTAG = Regex.Replace(attrTAG, @"_\d\d\d$", "");
                            if (AttributeValues.ContainsKey(attrTAG)) continue;

                            var allLinkedAttributes = (from A in AttrtbutesInInsert
                                                       where Regex.Match(
                                                           A.FirstOrDefault(c => c.Code.Trim() == "2").Value,
                                                           attrTAG + @"_\d\d\d").Success
                                                       select A.FirstOrDefault(c => c.Code.Trim() == "1").Value);

                            attrVALUE = String.Join(@"\P", allLinkedAttributes);
                        }

                        AttributeValues.Add(attrTAG, attrVALUE);
                    }
                }
            }

            AttributeValues = AttributeValues.ToDictionary(k => k.Key, v => v.Value.Replace("\\P", System.Environment.NewLine));

            return AttributeValues;
        }

        private Vector3 scale;
        private Vector3 postion;
        private float rotation;
        #endregion

    }




}




