using System.ComponentModel;

namespace PenguinTools.Workflow;

/// <summary>How chart files are discovered when scanning a song folder for the option bundle.</summary>
public enum ChartFileDiscoveryMode
{
    [Description("mgxc only")] MgxcOnly = 0,

    [Description("ugc only")] UgcOnly = 1,

    [Description("mgxc first")] MgxcFirst = 2,

    [Description("ugc first")] UgcFirst = 3
}