using System.Threading.Tasks;
using DAT10.StarModelComponents;

namespace DAT10.Modules.Generation
{
    public interface IGeneration : IModule
    {
        Task Generate(StarModel starModel, string resultPath);
    }
}