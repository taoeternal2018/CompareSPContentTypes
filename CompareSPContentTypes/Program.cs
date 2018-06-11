using System;
using System.Collections.Generic;
using System.Configuration;
using System.Xml.Linq;

namespace CompareSPContentTypes
{
    class Program
    {
        static NLog.Logger NLogger = NLog.LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {
            ShowHelp();

            var k = Console.ReadKey();
            while (true)
            {
                if (k.KeyChar == '1')
                {
                    Console.WriteLine();
                    NLogger.Info("========Validating ContentTypes========");
                    ValidateContenTypesIntegrity();
                    NLogger.Info("========End of Validating ContentTypes========");
                    k = Console.ReadKey();
                }
                else if (k.KeyChar == '2')
                {
                    Console.WriteLine();
                    NLogger.Info("========Validating Lists========");
                    ValidateListsIntegrity();
                    NLogger.Info("========End of Validating Lists========");
                    k = Console.ReadKey();
                }
                else
                {
                    break;
                }
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Preparations:\n"+ 
                "1. Copy ContentType & Lists xml files(each has two versions to compare) to the executable program path\n"+
                "2. Set value of desiredContentTypes/actualContentTypes/desiredLists/actualLists in config file according to the file names\n"+
                "Log files can be found under logs directory\n"+
                "-----------------------------------------------------\n"+
                "\nPress 1 to validate ContentTypes\nPress 2 to validate Lists\nPress any key else to quit");
        }

        static void ValidateContenTypesIntegrity() {
            string desiredContentTypes = ConfigurationManager.AppSettings["desiredContentTypes"];
            string actualContentTypes = ConfigurationManager.AppSettings["actualContentTypes"];

            List<ContentType> contentTypesOrigin = GetContentTypesFromFile(desiredContentTypes);
            List<ContentType> contentTypesNew = GetContentTypesFromFile(actualContentTypes);

            if (contentTypesOrigin==null || contentTypesOrigin.Count<=0) {
                NLogger.Fatal("Error reading file:"+desiredContentTypes+", please check");
                return;
            }
            if (contentTypesNew == null || contentTypesNew.Count <= 0) {
                NLogger.Fatal("Error reading file:"+actualContentTypes+", please check");
                return;
            }

            foreach (var cto in contentTypesOrigin) {
                bool found = false;
                foreach (var ctn in contentTypesNew) {
                    if (cto.Name.Trim().Equals(ctn.Name.Trim()))
                    {
                        CompareContentType(cto, ctn);
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    NLogger.Error("ContentType [" + cto.Name + "] is missing ");
                }
            }
        }
        static void CompareContentType(ContentType ctOrigin, ContentType ctNew){
            bool ret = true;
            ret &= Compare("ContentType ["+ctOrigin.Name+"]", "Column [Description]", ctOrigin.Description, ctNew.Description);
            ret &= Compare("ContentType ["+ctOrigin.Name+"]", "Column [Group]", ctOrigin.Group, ctNew.Group);

            foreach(var fo in ctOrigin.Fields){
                bool found = false;
                foreach (var fn in ctNew.Fields){
                    if (fo.StaticName.Equals(fn.StaticName))
                    {
                        ret &= Compare("ContentType ["+ctOrigin.Name + "]", "Field [Type]", fo.Name, fn.Name);
                        ret &= Compare("ContentType ["+ctOrigin.Name + "]", "Field [Type]", fo.Type, fn.Type);
                        ret &= Compare("ContentType ["+ctOrigin.Name + "]", "Field [DisplayName]", fo.DisplayName, fn.DisplayName);
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    NLogger.Error("Field [" + fo.Name + "] of ContentType [" + ctOrigin.Name + "] is missing");
                }
                ret &= found;
            }
            if (ret) {
                NLogger.Trace("ContentType ["+ctOrigin.Name+"] validation complete.");
            }
        }
        static void ValidateListsIntegrity() {
            string desiredLists = ConfigurationManager.AppSettings["desiredLists"];
            string actualLists = ConfigurationManager.AppSettings["actualLists"];

            List<ListInstance> listsOrigin = GetListsFromFile(desiredLists);
            List<ListInstance> listsNew = GetListsFromFile(actualLists);

            if (listsOrigin==null || listsOrigin.Count<=0) {
                NLogger.Fatal("Error reading file:"+desiredLists+", please check");
                return;
            }
            if (listsNew == null || listsNew.Count <= 0) {
                NLogger.Fatal("Error reading file:"+actualLists+", please check");
                return;
            }

            foreach (var lio in listsOrigin) {
                bool found = false;
                foreach (var lin in listsNew){
                    if (lio.Title.Equals(lin.Title))
                    {
                        CompareList(lio, lin);
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    NLogger.Error("List ["+lio.Title+"] is missing.");
                }
            }
        }
        
        static void CompareList(ListInstance liOrigin, ListInstance liNew) {
            bool ret = true;
            ret &= Compare("List ["+liOrigin.Title+"]", "Column [DocumentTemplate]", liOrigin.DocumentTemplate, liNew.DocumentTemplate);

            foreach (var cto in liOrigin.ContentTypeBindings)
            {
                bool found = false;
                foreach (var ctn in liNew.ContentTypeBindings) {
                    if (cto.Equals(ctn))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    NLogger.Error("ContentTypeId [" + cto + "] of List [" + liOrigin.Title + "] is missing");
                }
                ret &= found;
            }

            foreach (var fo in liOrigin.ListFields) {
                bool found = false;
                foreach (var fn in liNew.ListFields)
                {
                    if (fo.Name.Equals(fn.Name)) {
                        ret &= Compare("List ["+liOrigin.Title +"]","Field [DisplayName]", fo.DisplayName, fn.DisplayName);
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    NLogger.Error("Field ["+fo.Name+"] of List ["+liOrigin.Title+"] is missing");
                }
                ret &= found;
            }

            if (ret) {
                NLogger.Trace("List ["+liOrigin.Title+"] validation complete.");
            }
        }

        static List<ListInstance> GetListsFromFile(string fileName)
        {
            List<ListInstance> lists = new List<ListInstance>();
            try
            {
                var xDoc = XDocument.Load(fileName);
                XNamespace pnp = @"http://schemas.dev.office.com/PnP/2017/05/ProvisioningSchema";

                XElement rootNode = xDoc.Element(pnp + "Provisioning").Element(pnp + "Templates").Element(pnp + "ProvisioningTemplate").Element(pnp + "Lists");
                foreach (var node in rootNode.Elements(pnp + "ListInstance"))
                {
                    ListInstance li = new ListInstance();

                    li.Title = node.Attribute("Title") == null ? "" : node.Attribute("Title").Value;
                    li.Url = node.Attribute("Url") == null ? "" : node.Attribute("Url").Value;
                    li.Description = node.Attribute("Description") == null ? "" : node.Attribute("Description").Value;
                    li.DocumentTemplate = node.Attribute("DocumentTemplate") == null ? "" : node.Attribute("DocumentTemplate").Value;
                    li.TemplateType = node.Attribute("TemplateType") == null ? "" : node.Attribute("TemplateType").Value;

                    foreach (XElement fieldNode in node.Element(pnp + "FieldRefs").Elements(pnp + "FieldRef"))
                    {
                        ListField lf = new ListField();
                        lf.Id = fieldNode.Attribute("ID") == null ? "" : fieldNode.Attribute("ID").Value;
                        lf.Name = fieldNode.Attribute("Name") == null ? "" : fieldNode.Attribute("Name").Value;
                        lf.DisplayName = fieldNode.Attribute("DisplayName") == null ? "" : fieldNode.Attribute("DisplayName").Value;
                        lf.Hidden = fieldNode.Attribute("Hidden") == null ? "" : fieldNode.Attribute("Hidden").Value;
                        if (lf.Hidden.ToUpper().Equals("TRUE"))
                            continue;
                        li.ListFields.Add(lf);
                    }

                    foreach (XElement fieldNode in node.Element(pnp + "ContentTypeBindings").Elements(pnp + "ContentTypeBinding"))
                    {
                        string ctb = fieldNode.Attribute("ContentTypeID") == null ? "" : fieldNode.Attribute("ContentTypeID").Value;
                        li.ContentTypeBindings.Add(ctb);
                    }

                    lists.Add(li);
                    //Console.ReadKey();
                }
            }
            catch (Exception ex) {
                NLogger.Fatal(ex.Message);
            }
            return lists;
        }
        static List<ContentType> GetContentTypesFromFile(string fileName)
        {
            List<ContentType> contents = new List<ContentType>();
            try
            {
                var xDoc = XDocument.Load(fileName);
                XElement rootNode = xDoc.Element("ContentTypes");
                foreach (XElement node in rootNode.Elements("ContentType"))
                {
                    ContentType ct = new ContentType();
                    ct.Id = node.Attribute("ID").Value;
                    ct.Name = node.Attribute("Name").Value;
                    ct.Group = node.Attribute("Group").Value;
                    ct.Description = node.Attribute("Description").Value;

                    foreach (XElement fieldNode in node.Element("Fields").Elements("Field"))
                    {
                        ContentTypeField field = new ContentTypeField();
                        field.Id = fieldNode.Attribute("ID").Value;
                        field.DisplayName = fieldNode.Attribute("DisplayName").Value;
                        field.Name = fieldNode.Attribute("Name").Value;
                        field.StaticName = fieldNode.Attribute("StaticName").Value;
                        field.Hidden = (fieldNode.Attribute("Hidden") == null) ? "" : fieldNode.Attribute("Hidden").Value;
                        field.Type = (fieldNode.Attribute("Type") == null) ? "" : fieldNode.Attribute("Type").Value;
                        ct.Fields.Add(field);
                    }
                    contents.Add(ct);
                }
            }
            catch (Exception ex) {
                NLogger.Fatal(ex.Message);
            }

            return contents;
        }

        static bool Compare(string src, string field, string str1, string str2) {
            if (!str1.Equals(str2))
            {
                NLogger.Error(field +" of " + src + " doesn't match, expected ["+str1+"], actually get ["+str2+"]");
                return false;
            }
            return true;
        }
    }
}
