using System;
using System.Globalization;
using ATDR.Extension;

namespace ATDR.Basics.Converters {
    [XrmInformation]
    public class Complex {
        [XrmConversion(typeof(DateTime))]
        public static DateTime ConvertDateTime(string text)
        {
            return DateTime.ParseExact(text, "MM/d/yyyy H:mm:ss", CultureInfo.InvariantCulture);
        }
    }
}