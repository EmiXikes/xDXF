using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;


namespace xDXF
{
    public class xInsert : Entity
    {
        public string dSep = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        Regex RX = new Regex("[.,]");

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

        private string GetTrueName()
        {
            var insertHardOwner = Data.FirstOrDefault(c => c.Code.Trim() == "360");
            var insertName = Data.FirstOrDefault(c => c.Code.Trim() == code.NAME).Value;

            if (insertHardOwner == null)
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

        public List<List<ValPair>> GetAttributesData()
        {
            return SubItems("0", "ATTRIB");
        }

        /// <summary>
        /// <para> Attribute values. Values can be changed. To get other attribute data, use method Attributes(). </para>
        /// </summary>
        public Dictionary<string, List<ValPair>> AttributeValues
        {
            get
            {
                return null;
            }
        }
        /// <summary>
        /// <para> READ ONLY!!! Attribute values. Joined multiline values (for dwg versions lower than 2018). </para>
        /// </summary>
        public Dictionary<string, string> AttributeValuesStr
        {
            // TODO implement joining..
            get
            {
                //return _a;
                return GetAttributeValues();
            }
        }

        private Dictionary<string, string> GetAttributeValues()
        {
            var AttrtbutesInInsert = SubItems("0", "ATTRIB");

            var dxfVersion = xDXFHelperMethods.SubItems(
                xDXFHelperMethods.SubItems(hostDoc.DataValPairs, "0", "SECTION")[0],
                "9", "$ACADVER")[0][1].Value;

            Dictionary<string, string> AttributeValues = new Dictionary<string, string>();

            if (AttrtbutesInInsert.Count != 0)
            {

                foreach (List<ValPair> attr in AttrtbutesInInsert)
                {
                    var item_AcDbAttribute = xDXFHelperMethods.SubItems(attr, "100", "AcDbAttribute");
                    var item_AcDbText = xDXFHelperMethods.SubItems(attr, "100", "AcDbText");

                    if (Int32.Parse(dxfVersion.Substring(2)) > 1027)
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

                            AttributeValues.Add(item_AcDbAttribute[0].FirstOrDefault(c => c.Code.Trim() == "2").Value, attrValueFull);

                            //AttributeValues.Add(
                            //    item_AcDbAttribute[0].FirstOrDefault(c => c.Code.Trim() == "2").Value,
                            //    item_embeddedObj[0].FirstOrDefault(c => c.Code.Trim() == "3"));


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
                        AttributeValues.Add(
                                item_AcDbAttribute[0].FirstOrDefault(c => c.Code.Trim() == "2").Value,
                                item_AcDbText[0].FirstOrDefault(c => c.Code.Trim() == "1").Value);
                    }

                }


            }

            return AttributeValues;


        }

        #region Constructor stuff and privates

        public xInsert()
        {
            _a = new Dictionary<string, string>();
        }

        public Dictionary<string, string> _a;
        public string _tn;

        private Vector3 scale;
        private Vector3 postion;
        private float rotation;
        #endregion

    }




}




