using ATDR.Extension;

namespace ATDR.Basics.Converters {
    [XrmInformation]
    public class Primitve {
        [XrmConversion(typeof(int))]
        public static int ConvertInt(string text) {
            return int.Parse(text);
        }

        [XrmConversion(typeof(bool))]
        public static bool ConvertBool(string text) {
            return bool.Parse(text);
        }

        [XrmConversion(typeof(decimal))]
        public static decimal ConvertDecimal(string text) {
            return decimal.Parse(text);
        }

        [XrmConversion(typeof(string))]
        public static string ConvertString(string text) {
            return text;
        }
    }
}