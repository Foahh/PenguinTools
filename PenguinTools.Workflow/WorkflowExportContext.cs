using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Infrastructure;
using PenguinTools.Media;

namespace PenguinTools.Workflow;

public sealed record WorkflowExportContext(
    AssetManager Assets,
    IMediaTool MediaTool,
    IEmbeddedResourceStore ResourceStore,
    IInfrastructureAssetProvider AssetProvider);
