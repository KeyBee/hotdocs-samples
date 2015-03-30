﻿using System;
using HotDocs.Sdk;
using HotDocs.Sdk.Server.Contracts;
using HotDocs.Sdk.Server.OnPremise;

namespace SdkExample13
{
    /// <summary>
    /// This shows how to read variables and dialogs out of a component file
    /// using the on premise web API
    /// 
    ///This will test getting the component info from a package. It assumes relevant package exists in on premise API package folder (TempFiles).
    ///1. On line 20 and 21 set your unique packageID (Should be the GUID of your package on the server) 
    ///And also the address of the webapi as the HostAddress ("http://localhost:52948/HDSWebAPI/api/HDCS").
    ///2. The variables and dialogs should be returned in the console variables first then dialogs.
    /// </summary>
    internal class Example13
    {
        private static void Main()
        {
            var componentInfo = GetComponentInfo();

            //Component Information
            Console.WriteLine("Components:");
            foreach (var variable in componentInfo.Variables)
            {
                Console.WriteLine("Name: " + variable.Name + ", Type: " + variable.Type);                
            }

            //Dialog Information
            Console.WriteLine("Dialogs:");
            foreach (var dialog in componentInfo.Dialogs)
            {
                Console.WriteLine("Name: " + dialog.Name);
            }

            Console.ReadLine();
        }

        private static ComponentInfo GetComponentInfo()
        {
            var template = GetTemplate();
            var service = new OnPremiseServices("http://localhost:80/HDSWebAPI/api/HDCS");

            var componentInfo = service.GetComponentInfo(template, true, "logref");
            return componentInfo;
        }

        private static Template GetTemplate()
        {
            var templateLocation = new WebServiceTemplateLocation("7A7BF8B9-C895-4BC9-BC1A-44E61D6008A2", "http://localhost:80/HDSWebAPI/api/HDCS");
            var template = new Template(templateLocation);
            return template;
        }   
    }
}