using System;

namespace ATDR.Extension
{
    [AttributeUsage(AttributeTargets.Class)]
    public class XrmInformation : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Method)]
    public class XrmConversion : Attribute
    {
        public Type type;

        public XrmConversion(Type type)
        {
            this.type = type;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class XrmTable : Attribute
    {
        public XrmTable()
        {
        }

        public class Information
        {
            public bool exists;
            public string logicalName;
            public string uniqueIdentifierName;
            public int priority;
            public string[] preview_data;
            public string[] postProcessing;
            public string[] depends; // TODO write dependends to automatic select other items that are required to restore this data
        }
    }
}