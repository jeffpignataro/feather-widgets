﻿using ContentBlock.Mvc.Models;
using ContentBlock.Mvc.StringResources;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Web.Mvc;
using Telerik.Sitefinity.Frontend.Mvc.Infrastructure.Controllers;
using Telerik.Sitefinity.Frontend.Mvc.Infrastructure.Controllers.Attributes;
using Telerik.Sitefinity.Frontend.Resources;
using Telerik.Sitefinity.Localization;
using Telerik.Sitefinity.Modules.GenericContent;
using Telerik.Sitefinity.Mvc;
using Telerik.Sitefinity.Services;
using Telerik.Sitefinity.Utilities.TypeConverters;
using Telerik.Sitefinity.Web;
using Telerik.Sitefinity.Web.UI;

namespace ContentBlock.Mvc.Controllers
{
    /// <summary>
    /// The content block controller
    /// </summary>
    [ControllerToolboxItem(Name = "ContentBlock", Title = "ContentBlock", SectionName = "MvcWidgets")]
    [Localization(typeof(ContentBlockResources))]
    public class ContentBlockController : Controller, IZoneEditorReloader, ICustomWidgetVisualization, ICustomWidgetTitlebar, IHasEditCommands, IContentItemControl
    {
        #region Properties

        /// <summary>
        /// Gets or sets the HTML content to be displayed by the ContentBlock control.
        /// </summary>
        [DynamicLinksContainer]
        public virtual string Content
        {
            get
            {
                return this.content;
            }
            set
            {
                this.content = value;
                this.IsEmpty = string.IsNullOrEmpty(this.content);
            }
        }

        /// <summary>
        /// Gets or sets the ID of the ContentBlockItem if the Content is shared across multiple controls
        /// </summary>
        public Guid SharedContentID
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the provider.
        /// </summary>
        /// <value>The name of the provider.</value>
        public string ProviderName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the current content version.
        /// </summary>
        /// <value>The version.</value>
        [Browsable(false)]
        public int ContentVersion
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the current mode of the control.
        /// </summary>
        public bool EnableSocialSharing
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the content block model.
        /// </summary>
        /// <value>
        /// The model.
        /// </value>
        protected IContentBlockModel Model
        {
            get
            {
                if (this.model == null)
                {
                    this.model = this.InitializeModel();
                    this.isEmpty = string.IsNullOrEmpty(this.model.Content);
                    this.SharedContentID = this.model.SharedContentID;
                }
                return this.model;
            }
        }

        #endregion

        #region Actions

        /// <summary>
        /// This is the default Action.
        /// </summary>
        public ActionResult Index()
        {
            this.Commands = this.InitializeCommands();

            if (SystemManager.CurrentHttpContext != null)
                this.AddCacheDependencies(this.Model.GetKeysOfDependentObjects());

            return View("Default", this.Model);
        }

        #endregion

        #region IZoneEditorReloader

        /// <summary>
        /// Defines whether controls of same key will be
        /// reloaded when this control is updated in the ZoneEditor
        /// </summary>
        /// <returns>True if a reload is required</returns>
        [Browsable(false)]
        bool IZoneEditorReloader.ShouldReloadControlsWithSameKey()
        {
            return this.SharedContentID != Guid.Empty;
        }

        /// <summary>
        /// Gets unique reload data (i.e. all controls with the same key will get reloaded)
        /// </summary>
        /// <value></value>
        [Browsable(false)]
        string IZoneEditorReloader.Key
        {
            get
            {
                var id = this.SharedContentID.ToString("N", CultureInfo.InvariantCulture);
                var key = string.Format(IZoneEditorReloaderKeyStringFormat, id);
                return key;
            }
        }

        #endregion

        #region ICustomWidgetVisualization

        /// <summary>
        /// Gets or sets a value indicating whether this instance is empty.
        /// </summary>
        /// <value><c>true</c> if this instance is empty; otherwise, <c>false</c>.</value>
        [Browsable(false)]
        public bool IsEmpty
        {
            get
            {
                return this.isEmpty;
            }
            protected set
            {
                this.isEmpty = value;
            }
        }

        /// <summary>
        /// Gets the empty link text.
        /// </summary>
        /// <value>The empty link text.</value>
        [Browsable(false)]
        public string EmptyLinkText
        {
            get
            {
                return Res.Get<ContentBlockResources>().CreateContent;
            }
        }

        #endregion

        #region ICustomWidgetTitlebar Members

        /// <summary>
        /// Gets a list with custom messages which will be applied to dock title bar.
        /// </summary>
        /// <value>The custom messages.</value>
        [Browsable(false)]
        [TypeConverter(typeof(StringListConverter))]
        public virtual IList<string> CustomMessages
        {
            get
            {
                var messages = new List<string>();
                if (this.SharedContentID != Guid.Empty)
                {
                    messages.Add(Res.Get<ContentBlockResources>().Shared);
                }
                return messages;
            }
        }


        #endregion

        #region IHasEditCommands Members

        /// <summary>
        /// Gets or sets the widget commands.
        /// </summary>
        /// <value>
        /// The commands.
        /// </value>
        [Browsable(false)]
        public IList<WidgetMenuItem> Commands
        {
            get;
            set;

        }

        /// <summary>
        /// Initializes the widget commands.
        /// </summary>
        protected virtual IList<WidgetMenuItem> InitializeCommands()
        {
            var packageManager = new PackageManager();
            var commandsList = new List<WidgetMenuItem>(5);

            commandsList.Add(new WidgetMenuItem() { Text = Res.Get<Labels>().Delete, CommandName = "beforedelete", CssClass = "sfDeleteItm" });
            commandsList.Add(new WidgetMenuItem() { Text = Res.Get<Labels>().Duplicate, CommandName = "duplicate", CssClass = "sfDuplicateItm" });

            if (this.SharedContentID == Guid.Empty)
            {
                var shareActionLink = packageManager.EnhanceUrl(RouteHelper.ResolveUrl(string.Format(ContentBlockController.DesignerTemplate, "Share"), UrlResolveOptions.Rooted));
                commandsList.Add(new WidgetMenuItem() { Text = Res.Get<ContentBlockResources>().Share, ActionUrl = shareActionLink, NeedsModal = true });
            }
            else
            {
                var unshareActionLink = packageManager.EnhanceUrl(RouteHelper.ResolveUrl(string.Format(ContentBlockController.DesignerTemplate, "Unshare"), UrlResolveOptions.Rooted));
                commandsList.Add(new WidgetMenuItem() { Text = Res.Get<ContentBlockResources>().Unshare, ActionUrl = unshareActionLink, NeedsModal = true });
            }

            var useSharedActionLink = packageManager.EnhanceUrl(RouteHelper.ResolveUrl(string.Format(ContentBlockController.DesignerTemplate, "UseShared"), UrlResolveOptions.Rooted));
            commandsList.Add(new WidgetMenuItem() { Text = Res.Get<ContentBlockResources>().UseShared, ActionUrl = useSharedActionLink, NeedsModal = true });
            commandsList.Add(new WidgetMenuItem() { Text = Res.Get<Labels>().Permissions, CommandName = "permissions", CssClass = "sfPermItm" });
            return commandsList;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Initializes the content block model.
        /// </summary>
        private IContentBlockModel InitializeModel()
        {
            var constructorParameters = new Dictionary<string, object> 
                        {
                           {"providerName", this.ProviderName},
                           {"content", this.content},
                           {"enableSocialSharing", this.EnableSocialSharing},
                           {"sharedContentId", this.SharedContentID}
                        };

            return ControllerModelFactory.GetModel<IContentBlockModel>(this.GetType(), constructorParameters);
        }

        #endregion

        #region Private fields

        private bool isEmpty = true;
        private string content;
        private IContentBlockModel model;

        private const string DesignerTemplate = "Telerik.Sitefinity.Frontend/Designer/Master/ContentBlock?view={0}";
        private const string IZoneEditorReloaderKeyStringFormat = "ContentBlock_{0}";

        #endregion
    }
}
