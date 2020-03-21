using DAT10.StarModelComponents;

namespace DAT10.Modules.StarRefinement
{
    public interface IStarRefinement : IModule
    {
        StarModel Refine(StarModel starModel);
    }
}