﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Reflection;
using System.Xml;
using System.Xaml;
using System.IO;
using System.Windows.Resources;
using BioLink.Client.Utilities;
using BioLink.Data;

namespace BioLink.Client.Extensibility {

    public abstract class BiolinkPluginBase : IBioLinkPlugin {

        private ResourceDictionary _resourceDictionary;

        public User User { get; set; }
        public PluginManager PluginManager { get; set; }

        public BiolinkPluginBase() {
        }

        public virtual void InitializePlugin(User user, PluginManager pluginManager, Window parentWindow) {

            this.User = user;
            this.PluginManager = pluginManager;
            this.ParentWindow = parentWindow;
            
            string assemblyName = this.GetType().Assembly.GetName().Name;
            string packUri = String.Format("pack://application:,,,/{0};component/StringResources.xaml", assemblyName);
            Logger.Debug("Attempting resource discovery for {0} ({1})", assemblyName, packUri);

            try {
                Uri uri = new Uri(packUri, UriKind.Absolute);
                StreamResourceInfo info = Application.GetResourceStream(uri);
                if (info != null) {
                    _resourceDictionary = XamlServices.Load(info.Stream) as ResourceDictionary;
                    Logger.Debug("{0} resource keys loaded (Ok).", _resourceDictionary.Count);
                } else {
                    Logger.Debug("No resource stream found for assembly {0} - message keys will be used instead", assemblyName);
                    _resourceDictionary = new ResourceDictionary();
                }
            } catch (Exception ex) {
                Logger.Debug("Failed to read resources for {0} : {1}", assemblyName, ex.ToString());
                _resourceDictionary = new ResourceDictionary();
            }
        }

        protected String _R(string key, params object[] args) {
            Object res = _resourceDictionary[key];
            if (res == null) {
                return key;
            } else {
                if (args != null) {
                    return String.Format(res.ToString(), args);
                } else {
                    return res.ToString();
                }
            }            
        }

        public ResourceDictionary ResourceDictionary {
            get { return _resourceDictionary; }
        }

        public string GetCaption(string key, params object[] args) {
            return _R(key, args);
        }

        public abstract string Name { get; }

        public abstract List<IWorkspaceContribution> GetContributions();

        public abstract bool RequestShutdown();

        public abstract List<Command> GetCommandsForObject(ViewModelBase obj);
        
        public virtual void Dispose() {            
        }

        public virtual ViewModelBase CreatePinnableViewModel(PinnableObject pinnable) {
            return null;
        }


        public Window ParentWindow {
            get; set;
        }

        public virtual bool CanSelect(Type t) {
            return false;
        }

        public virtual void Select(Type t, Action<SelectionResult> success) {
            throw new NotImplementedException();
        }

        public virtual void Dispatch(string command, Action<object> callback, params object[] args) {
        }



        public virtual bool CanEditObjectType(LookupType type) {
            return false;
        }

        public virtual void EditObject(LookupType type, int objectID) {
        }

    }
}
