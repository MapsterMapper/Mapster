using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mapster;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("df5bd29d-85e6-4621-b3cb-1561e98f9697")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and  Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: InternalsVisibleTo("Mapster.Tests, PublicKey=00240000048000009400000006020000002400005253413100040000010001000936c652e8d888894759aa92c40f62c30d691cb153214c6ddff550ee7b68b320eefeed3fceef9a7cea5cfce035983b4d6c22ea7a925e375116cdf8f6ea6259ebe263fbd9a1332037e5f7da63df86124223c81667c86b387372aa769a145ddadb378ba6dfe2b4f4266c89eb54b477938ba265321fa77f953f2abaacfed62e66bd")]

[assembly: TypeForwardedTo(typeof(MapContext))]
[assembly: TypeForwardedTo(typeof(MapContextScope))]
[assembly: TypeForwardedTo(typeof(MapType))]
[assembly: TypeForwardedTo(typeof(MemberSide))]
