# Audit Table Data Restorer 

ATDR is a tool for restoring deleted table entries. Initialize a new connection for your password using your host (`organization.crm.dynamics.com`), a user, and their user secret. If a schema for a deleted entry already exists, it will appear in the list when running the tool. Dynamic post-processing systems avoid worrying about opposite references.

The complete tool is extensible with table schemas and type converters. For more information, see [Plugin](#plugin). 

## Manual 

1. Download the latest release
2. Run the executable
3. Authenticate with your information
4. Select the data to restore

## Plugin 

In this section, I will shortly demonstrate how to program and use a plugin. Make sure that the ATDR is installed on your system.

First, create a new .NET class library: 

```
dotnet new classlib
```

Make sure that the `TargetFramework` is set to `.net4.6.2`. Then, add a reference to the `ATDR.Extension.dll` file located at the ATDR executable: 

```xml
<!-- MyPlugin.csproj -->
<ItemGroup>
  <Reference Include="path/to/ATDR.Extension.dll" />
</ItemGroup>
```

Last but not least, when you want to use the plugin, copy its compiled DLLs and its dependencies DLL (not the `Microsoft.PowerPlatform.Dataverse.Client` library and `ATDR.Extension.dll`) into the `ext` folder of the ATDR executable location.
Now the setup for a new plugin is finished. 

---

## Converters 

The goal of converters is to translate a string to a specific Dataverse type managed by ATDR. Every class must have the `XrmInformation` attribute to be able to be found by the system. Additionally, every static method that represents a converter must contain the `XrmConversion` attribute.

First, create a new class in your extension: 

```cs
using ATDR.Extension;

[XrmInformation]
class MyConverter {
  
}
```

Then, create a new static method with the `XrmConversion` attribute and its target type as its attribute. The method must return a value and have a string parameter as its first parameter. 

```cs
using ATDR.Extension;

[XrmInformation]
class MyConverter {
  [XrmConversion(typeof(MyTargetType))]
  public static object MyConverter(string text) {
      ...
  }
}
```

For example, a method that targets `OptionSetValue`s would look like: 

```cs
using ATDR.Extension;
using Microsoft.Xrm.Sdk;

[XrmInformation]
class MyConverter {
  [XrmConversion(typeof(OptionSetValue))]
  public static object ConvertOptionSetValue(string text) {
      return new OptionSetValue(int.Parse(text));
  }
}
```

## Schemas

Schemas describe the needed structure of a table. Also, here every class must have the `XrmInformation` attribute to be able to be found by the system. Additionally, every static method that represents a schema must contain the `XrmTable` attribute and must return a `XrmTable.Information` object.

| Datatype | Name                 | Description                                                                                                                                                                                           |
|----------|----------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| string   | `logicalName`        | Represents the logicalName of your Table                                                                                                                                                              |
| string   | `uniqueIdentifierName` | Represents the primary key (e.g. `accountid`) of the Table                                                                                                                                                 |
| int      | priority             | Terminates the queue of the building process. Tables with a low value are restored first                                                                                                            |
| string[] | `preview_data`         | Represents logicalNames that values are used in the selection list                                                                                                                                    |
| string[] | `postProccesing`       | Represents `logicalNames` that are restored after all selected Tables are restored. Must be used when a lookup field targets a field that cannot exist at the point of the main restore process |
| string[] | `depends`              | A list of `logicalNames` of lookup fields that are required to restore the data (e.g. table `account` the `contactid`)                                                                                         |


An example with the Contact table: 

```cs
using ATDR.Extension;

[XrmInformation]
class MyConverter {
  [XrmTable]
  public static XrmTable.Information GetAccount(string text) {
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
```
