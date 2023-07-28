using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using ATDR.Extension;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace ATDR
{
    public class Reverse
    {
        private ServiceClient client;
        private readonly XrmTable.Information[] schemas;

        public Reverse(ServiceClient client, XrmTable.Information[] schemas)
        {
            this.client = client;
            this.schemas = schemas;
        }

        public void ReverseAll(AuditRow[] rows)
        {
            Console.WriteLine("Start restore data...");
            Console.WriteLine();

            uint max_prio = 0;
            foreach (XrmTable.Information schema in schemas)
            {
                if ((uint)schema.priority > max_prio)
                {
                    max_prio = (uint)schema.priority;
                }
            }

            int progress = 0;
            for (uint i = 0; i <= max_prio; i++)
            {
                foreach (AuditRow row in rows)
                {
                    if (row.changedData == null)
                    {
                        continue;
                    }


                    foreach (XrmTable.Information schema in schemas)
                    {
                        if (schema.logicalName != row.table)
                        {
                            continue;
                        }

                        if (schema.priority != i)
                        {
                            continue;
                        }

                        Console.CursorTop--;
                        progress++;
                        Console.WriteLine($"[{progress}|{rows.Length}] Restore {row.record.ToString()} from {row.table}");
                        ReverseItem(row, schema);
                    }
                }
            }

            Thread.Sleep(1000);

            Console.WriteLine();
            progress = 0;
            for (uint i = 0; i <= max_prio; i++)
            {
                foreach (AuditRow row in rows)
                {
                    if (row.changedData == null)
                    {
                        continue;
                    }


                    foreach (XrmTable.Information schema in schemas)
                    {
                        if (schema.logicalName != row.table)
                        {
                            continue;
                        }

                        if (schema.priority != i)
                        {
                            continue;
                        }

                        Console.CursorTop--;
                        progress++;
                        Console.WriteLine($"[{progress}|{rows.Length}] Post-Restore {row.record.ToString()} from {row.table}");
                        PostReverseItem(row, schema);
                    }
                }
            }
        }

        private void ReverseItem(AuditRow row, XrmTable.Information schema)
        {
            Entity schemaTypes = GetSchema(row.table);
            Entity req = new Entity(row.table);


            req[schema.uniqueIdentifierName] = row.record;
            List<string> keys = schemaTypes.Attributes.Keys.ToList();

            MethodInfo[] methods = Loader.INSTANCE.GetConverters();

            for (int i = 0; i < keys.Count; i++)
            {
                if (schema.postProcessing.Contains(keys[i]))
                {
                    continue;
                }

                Type type = schemaTypes.GetAttributeValue<object>(keys[i]).GetType();
                ChangedItem data = row.changedData.changedAttributes.FirstOrDefault(a => a.logicalName == keys[i]);

                if (data == null || data.oldValue == null || data.oldValue == "")
                {
                    continue;
                }

                foreach (MethodInfo info in methods)
                {
                    if (((XrmConversion)info.GetCustomAttribute(typeof(XrmConversion), false)).type == type)
                    {
                        req[data.logicalName] = info.Invoke(null, new object[] {
                        data.oldValue
                    });
                        break;
                    }
                }
            }

            CreateResponse res = (CreateResponse)client.Execute(new CreateRequest() { Target = req });
        }

        private void PostReverseItem(AuditRow row, XrmTable.Information schema)
        {
            Entity schemaTypes = GetSchema(row.table);
            Entity req = new Entity(row.table);

            req.Id = row.record;
            List<string> keys = schemaTypes.Attributes.Keys.ToList();

            MethodInfo[] methods = Loader.INSTANCE.GetConverters();

            for (int i = 0; i < keys.Count; i++)
            {
                if (!schema.postProcessing.Contains(keys[i]))
                {
                    continue;
                }

                Type type = schemaTypes.GetAttributeValue<object>(keys[i]).GetType();
                ChangedItem data = row.changedData.changedAttributes.FirstOrDefault(a => a.logicalName == keys[i]);

                if (data == null || data.oldValue == null || data.oldValue == "")
                {
                    continue;
                }

                foreach (MethodInfo info in methods)
                {
                    if (((XrmConversion)info.GetCustomAttribute(typeof(XrmConversion), false)).type == type)
                    {
                        req[data.logicalName] = info.Invoke(null, new object[] {
                        data.oldValue
                    });
                        break;
                    }
                }
            }

            client.Update(req);
        }

        private Entity GetSchema(string logicalName)
        {
            QueryExpression qe = new QueryExpression(logicalName);
            qe.ColumnSet.AllColumns = true;
            qe.TopCount = 1;

            Entity entity = client.RetrieveMultiple(qe).Entities[0];

            return entity;
        }
    }
}