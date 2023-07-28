using System;
using ATDR.Extension;
using Microsoft.Xrm.Sdk;

namespace ATDR.Basics.Converters
{
    [XrmInformation]
    public class Dataverse
    {
        [XrmConversion(typeof(EntityReference))]
        public static EntityReference ConvertEntityReference(string text)
        {
            string[] parts = text.Split(',');
            return new EntityReference(parts[0], Guid.Parse(parts[1]));
        }

        [XrmConversion(typeof(OptionSetValue))]
        public static OptionSetValue ConvertOptionSetValue(string text)
        {
            return new OptionSetValue(int.Parse(text));
        }

        [XrmConversion(typeof(Money))]
        public static Money ConvertMoney(string text)
        {
            return new Money(decimal.Parse(text));
        }
    }
}