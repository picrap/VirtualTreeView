// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Markup;

//In order to begin building localizable applications, set
//<UICulture>CultureYouAreCodingWith</UICulture> in your .csproj file
//inside a <PropertyGroup>.  For example, if you are using US english
//in your source files, set the <UICulture> to en-US.  Then uncomment
//the NeutralResourceLanguage attribute below.  Update the "en-US" in
//the line below to match the UICulture setting in the project file.

//[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.Satellite)]


[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
                                     //(used if a resource is not found in the page,
                                     // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
                                              //(used if a resource is not found in the page,
                                              // app, or any theme specific resource dictionaries)
)]


[assembly: AssemblyTitle("VirtualTreeView")]

[assembly: AssemblyDescription("A WPF TreeView that actually supports virtualization. It is based on an ItemsControl with VirtualizingStackPanel.")]
[assembly: AssemblyCompany("openstore.craponne.fr")]
[assembly: AssemblyProduct("VirtualTreeView")]
[assembly: AssemblyCopyright("MIT license http://opensource.org/licenses/mit-license.php")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: AssemblyVersion("0.1")]

[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]

[assembly: XmlnsDefinition("urn:VirtualTreeView", "VirtualTreeView")]

[assembly: XmlnsPrefix("urn:VirtualTreeView", "vtv")]
