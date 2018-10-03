using System;
using System.Collections.Generic;
using System.Text;

namespace ECS
{
    public abstract class ComponentSystem
    {
        public abstract void Update(World world);
    }
}
