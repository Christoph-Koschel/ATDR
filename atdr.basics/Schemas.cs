using ATDR.Extension;

namespace ATDR.Basics
{
    [XrmInformation]
    class BasicSchema
    {
        [XrmTable]
        public static XrmTable.Information GetAccount()
        {
            return new XrmTable.Information()
            {
                logicalName = "account",
                uniqueIdentifierName = "accountid",
                priority = 0,
                preview_data = new string[] { "name" },
                postProcessing = new string[] { "primarycontactid" },
                depends = new string[] { "primarycontactid" }
            };
        }

        [XrmTable]
        public static XrmTable.Information GetOpportunity()
        {
            return new XrmTable.Information()
            {
                logicalName = "opportunity",
                uniqueIdentifierName = "processid",
                priority = 1,
                preview_data = new string[] { "name" },
                postProcessing = new string[] { },
            };
        }

        [XrmTable]
        public static XrmTable.Information GetOpportunitySalesProcess()
        {
            return new XrmTable.Information()
            {
                logicalName = "opportunitysalesprocess",
                uniqueIdentifierName = "businessprocessflowinstanceid",
                priority = 2,
                preview_data = new string[] { "name" },
                postProcessing = new string[] { }
            };
        }

        [XrmTable]
        public static XrmTable.Information GetContact()
        {
            return new XrmTable.Information()
            {
                logicalName = "contact",
                uniqueIdentifierName = "contactid",
                priority = 2,
                preview_data = new string[] { "fullname" },
                postProcessing = new string[] { "accountid" },
                depends = new string[] { "accountid" }
            };
        }
    }
}