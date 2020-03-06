using System.Collections.Generic;

namespace DWStar.Modules.Common
{
    public interface IDepends
    {
        ISet<string> Requires { get; }
        ISet<string> Provides { get; }
    }
}