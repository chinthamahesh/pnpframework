﻿using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;

namespace PnP.Framework.Utilities.CanvasControl.Processors
{
    /// <summary>
    /// Updates list id for List web part, to allow provision based on URL in a dynamic provisioning scenario
    /// </summary>
    public class ListControlPostProcessor : ICanvasControlPostProcessor
    {
        private readonly IDictionary<string, object> _properties;

        /// <summary>
        /// Constructor for ListControlPostProcessor class
        /// </summary>
        /// <param name="control">Client control</param>
        public ListControlPostProcessor(Framework.Provisioning.Model.CanvasControl control)
        {
            _properties = JsonUtility.Deserialize<Dictionary<string, dynamic>>(control.JsonControlData);
        }


        /// <summary>
        /// Method for processing canvas control
        /// </summary>
        /// <param name="canvasControl">Canvas control object</param>
        /// <param name="context">ClientContext to use</param>
        public void Process(Framework.Provisioning.Model.CanvasControl canvasControl, ClientContext context)
        {
            var web = GetWeb(context);
            var list = GetList(web);

            if (list == null)
            {
                return;
            }

            list.EnsureProperties(l => l.Id, l => l.RootFolder, l => l.RootFolder.Name, l => l.RootFolder.ServerRelativeUrl);

            SetProperty("selectedListId", list.Id);
            SetProperty("selectedListUrl", list.RootFolder.ServerRelativeUrl);

            canvasControl.JsonControlData = JsonUtility.Serialize(_properties);
        }

        private List GetList(Web web)
        {
            // grab list based on URL
            var listUrlProperty = GetProperty("selectedListUrl") as string;
            if (!string.IsNullOrWhiteSpace(listUrlProperty))
            {
                if (!listUrlProperty.StartsWith("/"))
                {
                    return web.GetListByUrl(listUrlProperty);
                }

                var list = web.GetList(listUrlProperty);
                web.Context.Load(list);
                web.Context.ExecuteQueryRetry();
                return list;
            }

            // grab list based on list id
            var listIdProperty = GetProperty("selectedListId") as string;
            Guid listId;
            if (TryParseGuidProperty(listIdProperty, out listId))
            {
                var list = web.Lists.GetById(listId);
                web.Context.Load(list);
                web.Context.ExecuteQueryRetry();
                return list;
            }

            // grab list based on list title
            var listDisplayName = GetProperty("listTitle") as string;
            if (!string.IsNullOrWhiteSpace(listDisplayName))
            {
                return web.GetListByTitle(listDisplayName);
            }

            return null;
        }

        private Web GetWeb(ClientContext context)
        {
            return context.Web;
        }

        private bool TryParseGuidProperty(string guid, out Guid id)
        {
            if (!string.IsNullOrWhiteSpace(guid) && Guid.TryParse(guid, out id) && !id.Equals(Guid.Empty))
            {
                return true;
            }

            id = Guid.Empty;
            return false;
        }

        private object GetProperty(string name)
        {
            object value;
            return _properties.TryGetValue(name, out value) ? value : null;
        }

        private void SetProperty(string name, object value)
        {
            _properties[name] = value;
        }
    }
}