using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ATDR.Extension;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;

namespace ATDR
{
    class Program
    {
        private static ServiceClient client;
        private static XrmTable.Information[] schemas;
        public static void Main(string[] args)
        {
            Console.WriteLine($"Loaded {Loader.INSTANCE.GetTableDefinitions().Length} Schemas");
            Console.WriteLine($"Loaded {Loader.INSTANCE.GetConverters().Length} Converters");
            Console.WriteLine();

            Console.Write("Host: ");
            string host = Console.ReadLine();
            host = host.StartsWith("http") ? host : "https://" + host;
            Console.Write("User: ");
            string user = Console.ReadLine();
            Console.Write("Password: ");
            string password = Console.In.ReadPassword();

            string connectionString = $@"
            AuthType = OAuth;
            Url = {host};
            UserName = {user};
            Password = {password};
            LoginPrompt=Auto;
            RequireNewInstance = True";

            client = new ServiceClient(connectionString);
            QueryExpression qe = new QueryExpression("audit");
            qe.ColumnSet.AllColumns = true;
            qe.Criteria.AddCondition("action", ConditionOperator.Equal, 3);
            qe.TopCount = 5000;
            EntityCollection collection = client.RetrieveMultiple(qe);
            List<SelectionGroup<AuditRow>> items = new List<SelectionGroup<AuditRow>>();

            MethodInfo[] methods = Loader.INSTANCE.GetTableDefinitions();
            List<XrmTable.Information> schemas = new List<XrmTable.Information>();
            foreach (MethodInfo method in methods)
            {
                object res = method.Invoke(null, null);
                if (res.GetType() == typeof(XrmTable.Information))
                {
                    schemas.Add((XrmTable.Information)res);
                }
            }

            Program.schemas = schemas.ToArray();

            for (int i = 0; i < collection.Entities.Count; i++)
            {
                Entity entity = collection.Entities[i];

                if (entity.GetAttributeValue<OptionSetValue>("action").Value != 3)
                {
                    continue;
                }

                string raw = entity.GetAttributeValue<string>("changedata").Trim();
                ChangedData changedData = null;
                if (raw.StartsWith("{") && raw.EndsWith("}"))
                {
                    changedData = JsonConvert.DeserializeObject<ChangedData>(raw);
                }

                AuditRow row = new AuditRow()
                {
                    record = entity.GetAttributeValue<EntityReference>("objectid").Id,
                    table = entity.GetAttributeValue<string>("objecttypecode"),
                    changedData = changedData
                };

                bool skip = false;

                foreach (SelectionGroup<AuditRow> selection in items)
                {
                    if (selection.value.table != row.table)
                    {
                        continue;
                    }

                    if (selection.value.record != row.record)
                    {
                        continue;
                    }

                    skip = true;
                }

                if (skip)
                {
                    continue;
                }

                XrmTable.Information schema = schemas.FirstOrDefault(s => s.logicalName == row.table);
                if (schema == null)
                {
                    continue;
                }

                if (!EntryExists(row.table, row.record, schema.uniqueIdentifierName))
                {
                    items.Add(new SelectionGroup<AuditRow>(row));
                }
            }

            if (items.Count == 0)
            {
                Console.WriteLine("Nothing to restore");
                return;
            }

            Console.WriteLine("Select data to restore");
            Console.WriteLine("- Up/Down Arrows for navigation");
            Console.WriteLine("- Space to select an item");
            Console.WriteLine("- Tab to invert the selection");
            Console.WriteLine("- Enter to continue\n");
            for (int i = 0; i < ITEMS_TO_DISPLAY; i++)
            {
                Console.WriteLine();
            }

            while (true)
            {
                PrintOptions(items);
                ConsoleKeyInfo key = Console.ReadKey();
                if (key.Key == ConsoleKey.Spacebar)
                {
                    SelectionGroup<AuditRow> i = items[offset + cursorPosX];
                    i.selected = !i.selected;
                    items[offset + cursorPosX] = i;
                }
                else if (key.Key == ConsoleKey.Tab)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        SelectionGroup<AuditRow> tmp = items[i];
                        tmp.selected = !tmp.selected;
                        items[i] = tmp;
                    }
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    cursorPosX++;
                    if (cursorPosX + offset > items.Count - 1)
                    {
                        cursorPosX--;
                    }
                    if (cursorPosX == ITEMS_TO_DISPLAY)
                    {
                        cursorPosX = 0;
                        offset += ITEMS_TO_DISPLAY;
                    }
                }
                else if (key.Key == ConsoleKey.UpArrow)
                {
                    cursorPosX--;
                    if (cursorPosX < 0 && offset == 0)
                    {
                        cursorPosX = 0;
                    }
                    else if (cursorPosX < 0)
                    {
                        cursorPosX = ITEMS_TO_DISPLAY - 1;
                        offset -= ITEMS_TO_DISPLAY;
                    }
                }
            }
            cursorPosX = -1;
            PrintOptions(items);

            int autoSelected = 0;
            for (int i1 = 0; i1 < items.Count; i1++)
            {
                SelectionGroup<AuditRow> item = items[i1];
                if (item.selected)
                {
                    XrmTable.Information info = GetInformation(item.value.table);
                    if (info.depends == null || info.depends.Length == 0)
                    {
                        continue;
                    }

                    foreach (string dependency in info.depends)
                    {
                        ChangedItem i = item.value.changedData.GetItem(dependency);
                        Console.WriteLine(i.oldValue);
                        string[] parts = i.oldValue.Split(',');
                        string table = parts[0];
                        Guid guid = Guid.Parse(parts[1]);

                        if (EntryExists(table, guid, GetInformation(table).uniqueIdentifierName))
                        {
                            continue;
                        }

                        bool changed = false;

                        for (int i2 = 0; i2 < items.Count; i2++)
                        {
                            SelectionGroup<AuditRow> inItem = items[i2];
                            if (inItem.value.record == guid)
                            {
                                if (!inItem.selected)
                                {
                                    inItem.selected = true;
                                    items[i2] = inItem;
                                    autoSelected++;
                                    changed = true;
                                }

                                break;
                            }
                        }

                        if (!changed)
                        {
                            Console.WriteLine($"Cannot restore '{item.value.record}' cause of missing data");
                            item.selected = false;
                            items[i1] = item;
                            break;
                        }
                    }
                }
            }

            AuditRow[] rows = items.Where(item => item.selected).Select(item => item.value).ToArray();
            Console.WriteLine($"Selected {rows.Length} of {items.Count} entries");
            if (autoSelected != 0)
            {
                Console.WriteLine($"Automaticly selected {autoSelected} items because of dependencies");
            }
            if (rows.Length != 0)
            {
                Reverse reverse = new Reverse(client, schemas.ToArray());
                reverse.ReverseAll(rows);
            }
        }

        private static int offset = 0;

        private static int cursorPosX = 0;
        private const int ITEMS_TO_DISPLAY = 5;

        private static void PrintOptions(List<SelectionGroup<AuditRow>> items)
        {
            Console.CursorTop -= ITEMS_TO_DISPLAY;

            for (int pos = offset, count = 0; pos < offset + ITEMS_TO_DISPLAY; pos++, count++)
            {
                Console.CursorLeft = 0;
                if (pos >= items.Count)
                {
                    Console.WriteLine("".PadRight(Console.WindowWidth));
                    continue;
                }

                int padding = Console.WindowWidth;



                if (items[pos].selected)
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    string start = String.Format($"[{{0, {items.Count.ToString().Length}}}] + ", offset + count);
                    Console.Write(start);
                    padding -= start.Length;
                }

                if (pos - offset == cursorPosX)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                }

                if (!items[pos].selected)
                {
                    string start = String.Format($"[{{0, {items.Count.ToString().Length}}}] ", offset + count);
                    Console.Write(start);
                    padding -= start.Length;
                }

                string table = $"({items[pos].value.table}) ";
                padding -= table.Length;
                Console.Write(table);

                if (pos - offset == cursorPosX)
                {
                    Console.Write("> ");
                    padding -= "> ".Length;
                }



                Console.WriteLine(GetTitle(items[pos]).PadRight(padding));
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        private static string GetTitle(SelectionGroup<AuditRow> selection)
        {
            StringBuilder builder = new StringBuilder();

            foreach (XrmTable.Information schema in schemas)
            {
                if (schema.logicalName == selection.value.table)
                {
                    foreach (string key in schema.preview_data)
                    {
                        foreach (ChangedItem item in selection.value.changedData.changedAttributes)
                        {
                            if (key == item.logicalName)
                            {
                                builder.Append(item.oldValue).Append(" ");
                                break;
                            }
                        }
                    }

                    break;
                }
            }

            return builder.ToString();
        }

        private static bool EntryExists(string table, Guid record, string uniqueIdentifierName)
        {
            QueryExpression qe = new QueryExpression(table);
            qe.ColumnSet.AddColumn(uniqueIdentifierName);
            qe.Criteria.AddCondition(uniqueIdentifierName, ConditionOperator.Equal, record);

            EntityCollection collection = client.RetrieveMultiple(qe);

            return collection.Entities.Count != 0;
        }

        public static XrmTable.Information GetInformation(string table)
        {
            return schemas.First(s => s.logicalName == table);
        }
    }
}