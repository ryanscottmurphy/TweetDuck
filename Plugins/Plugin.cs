﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TweetDck.Plugins.Enums;

namespace TweetDck.Plugins{
    class Plugin{
        public string Identifier { get { return identifier; } }
        public string Name { get { return metadata["NAME"]; } }
        public string Description { get { return metadata["DESCRIPTION"]; } }
        public string Author { get { return metadata["AUTHOR"]; } }
        public string Version { get { return metadata["VERSION"]; } }
        public string Website { get { return metadata["WEBSITE"]; } }
        public string ConfigFile { get { return metadata["CONFIGFILE"]; } }
        public string ConfigDefault { get { return metadata["CONFIGDEFAULT"]; } }
        public string RequiredVersion { get { return metadata["REQUIRES"]; } }
        public PluginGroup Group { get; private set; }
        public PluginEnvironment Environments { get; private set; }

        public bool CanRun{
            get{
                return canRun ?? (canRun = CheckRequiredVersion(RequiredVersion)).Value;
            }
        }

        public bool HasConfig{
            get{
                return ConfigFile.Length > 0 && GetFullPathIfSafe(PluginFolder.Data, ConfigFile).Length > 0;
            }
        }

        public string ConfigPath{
            get{
                return HasConfig ? Path.Combine(GetPluginFolder(PluginFolder.Data), ConfigFile) : string.Empty;
            }
        }

        public bool HasDefaultConfig{
            get{
                return ConfigDefault.Length > 0 && GetFullPathIfSafe(PluginFolder.Root, ConfigDefault).Length > 0;
            }
        }

        public string DefaultConfigPath{
            get{
                return HasDefaultConfig ? Path.Combine(GetPluginFolder(PluginFolder.Root), ConfigDefault) : string.Empty;
            }
        }

        private readonly string pathRoot;
        private readonly string pathData;
        private readonly string identifier;
        private readonly Dictionary<string, string> metadata = new Dictionary<string, string>(4){
            { "NAME", "" },
            { "DESCRIPTION", "" },
            { "AUTHOR", "(anonymous)" },
            { "VERSION", "(unknown)" },
            { "WEBSITE", "" },
            { "CONFIGFILE", "" },
            { "CONFIGDEFAULT", "" },
            { "REQUIRES", "*" }
        };

        private bool? canRun;

        private Plugin(string path, PluginGroup group){
            string name = Path.GetFileName(path);
            System.Diagnostics.Debug.Assert(name != null);

            this.pathRoot = path;
            this.pathData = Path.Combine(Program.PluginDataPath, group.GetIdentifierPrefix(), name);

            this.identifier = group.GetIdentifierPrefix()+name;
            this.Group = group;
            this.Environments = PluginEnvironment.None;
        }

        private void OnMetadataLoaded(){
            string configPath = ConfigPath, defaultConfigPath = DefaultConfigPath;

            if (configPath.Length > 0 && defaultConfigPath.Length > 0 && !File.Exists(configPath) && File.Exists(defaultConfigPath)){
                string dataFolder = GetPluginFolder(PluginFolder.Data);

                if (!Directory.Exists(dataFolder)){ // config migration
                    string originalFile = Path.Combine(GetPluginFolder(PluginFolder.Root), ConfigFile);

                    if (File.Exists(originalFile)){
                        try{
                            Directory.CreateDirectory(dataFolder);
                            File.Copy(originalFile, configPath, false);
                            File.Delete(originalFile); // will fail without write perms in program folder, ignore if so
                        }catch{
                            // ignore
                        }

                        return;
                    }
                }

                try{
                    Directory.CreateDirectory(dataFolder);
                    File.Copy(defaultConfigPath, configPath, false);
                }catch(Exception e){
                    Program.Reporter.HandleException("Plugin Loading Error", "Could not generate a configuration file for '"+identifier+"' plugin.", true, e);
                }
            }
        }

        public string GetScriptPath(PluginEnvironment environment){
            if (Environments.HasFlag(environment)){
                string file = environment.GetScriptFile();
                return file != null ? Path.Combine(pathRoot, file) : string.Empty;
            }
            else{
                return string.Empty;
            }
        }

        public string GetPluginFolder(PluginFolder folder){
            switch(folder){
                case PluginFolder.Root: return pathRoot;
                case PluginFolder.Data: return pathData;
                default: return string.Empty;
            }
        }

        public string GetFullPathIfSafe(PluginFolder folder, string relativePath){
            string rootFolder = GetPluginFolder(folder);
            string fullPath = Path.Combine(rootFolder, relativePath);

            try{
                string folderPathName = new DirectoryInfo(rootFolder).FullName;
                DirectoryInfo currentInfo = new DirectoryInfo(fullPath);

                while(currentInfo.Parent != null){
                    if (currentInfo.Parent.FullName == folderPathName){
                        return fullPath;
                    }
                    
                    currentInfo = currentInfo.Parent;
                }
            }
            catch{
                // ignore
            }

            return string.Empty;
        }

        public override string ToString(){
            return Identifier;
        }

        public override int GetHashCode(){
            return identifier.GetHashCode();
        }

        public override bool Equals(object obj){
            Plugin plugin = obj as Plugin;
            return plugin != null && plugin.identifier.Equals(identifier);
        }

        public static Plugin CreateFromFolder(string path, PluginGroup group, out string error){
            Plugin plugin = new Plugin(path, group);

            if (!LoadMetadata(path, plugin, out error)){
                return null;
            }

            if (!LoadEnvironments(path, plugin, out error)){
                return null;
            }

            error = string.Empty;
            return plugin;
        }

        private static bool LoadEnvironments(string path, Plugin plugin, out string error){
            foreach(string file in Directory.EnumerateFiles(path, "*.js", SearchOption.TopDirectoryOnly).Select(Path.GetFileName)){
                plugin.Environments |= PluginEnvironmentExtensions.Values.FirstOrDefault(env => file.Equals(env.GetScriptFile(), StringComparison.Ordinal));
            }

            if (plugin.Environments == PluginEnvironment.None){
                error = "Plugin has no script files.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static readonly string[] endTag = { "[END]" };

        private static bool LoadMetadata(string path, Plugin plugin, out string error){
            string metaFile = Path.Combine(path, ".meta");

            if (!File.Exists(metaFile)){
                error = "Missing .meta file.";
                return false;
            }

            string[] lines = File.ReadAllLines(metaFile, Encoding.UTF8);
            string currentTag = null, currentContents = "";

            foreach(string line in lines.Concat(endTag).Select(line => line.TrimEnd()).Where(line => line.Length > 0)){
                if (line[0] == '[' && line[line.Length-1] == ']'){
                    if (currentTag != null){
                        plugin.metadata[currentTag] = currentContents;
                    }

                    currentTag = line.Substring(1, line.Length-2).ToUpperInvariant();
                    currentContents = "";

                    if (line.Equals(endTag[0])){
                        break;
                    }

                    if (!plugin.metadata.ContainsKey(currentTag)){
                        error = "Invalid metadata tag: "+currentTag;
                        return false;
                    }
                }
                else if (currentTag != null){
                    currentContents = currentContents.Length == 0 ? line : currentContents+"\r\n"+line;
                }
                else{
                    error = "Missing metadata tag before value: "+line;
                    return false;
                }
            }

            if (plugin.Name.Length == 0){
                error = "Plugin is missing a name in the .meta file.";
                return false;
            }

            Version ver;

            if (plugin.RequiredVersion.Length == 0 || !(plugin.RequiredVersion.Equals("*") || System.Version.TryParse(plugin.RequiredVersion, out ver))){
                error = "Plugin contains invalid version: "+plugin.RequiredVersion;
                return false;
            }

            plugin.OnMetadataLoaded();

            error = string.Empty;
            return true;
        }

        private static bool CheckRequiredVersion(string requires){
            return requires.Equals("*", StringComparison.Ordinal) || Program.Version >= new Version(requires);
        }
    }
}
