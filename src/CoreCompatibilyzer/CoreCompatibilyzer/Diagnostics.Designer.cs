﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CoreCompatibilyzer {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Diagnostics {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Diagnostics() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("CoreCompatibilyzer.Diagnostics", typeof(Diagnostics).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The underlined API is not portable to .Net Core 2.2 runtime because it is not present there. You need to change your code to eliminate its usage..
        /// </summary>
        public static string CoreCompat1001Description {
            get {
                return ResourceManager.GetString("CoreCompat1001Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &quot;{0}&quot; is missing in .Net Core 2.2 runtime.
        /// </summary>
        public static string CoreCompat1001Title {
            get {
                return ResourceManager.GetString("CoreCompat1001Title", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The underlined API is not portable to .Net Core 2.2 runtime because it is obsolete. Call to this API will throw PlatformNotSupportedException. You need to change your code to eliminate its usage..
        /// </summary>
        public static string CoreCompat1002Description {
            get {
                return ResourceManager.GetString("CoreCompat1002Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &quot;{0}&quot; is obsolete in .Net Core 2.2 runtime. Call to this API will throw &quot;PlatformNotSupportedException&quot;..
        /// </summary>
        public static string CoreCompat1002Title {
            get {
                return ResourceManager.GetString("CoreCompat1002Title", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Suppress the {0} diagnostic with a CoreCompatibilyzer suppression comment.
        /// </summary>
        public static string SuppressDiagnosticWithCommentCodeActionTitle {
            get {
                return ResourceManager.GetString("SuppressDiagnosticWithCommentCodeActionTitle", resourceCulture);
            }
        }
    }
}
