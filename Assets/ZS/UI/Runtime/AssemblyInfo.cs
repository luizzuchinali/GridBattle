using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ZS.UI.Editor")]
[assembly: InternalsVisibleTo("ZS.UI.Tests")]

#if UNITY_EDITOR
[assembly: UnityEditor.UIElements.UxmlNamespacePrefix("ZS.UI", "zs")]
[assembly: UnityEditor.UIElements.UxmlNamespacePrefix("ZS.UI.Navigation", "zsnav")]
#endif
