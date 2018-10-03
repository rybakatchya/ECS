using System;
using System.Collections.Generic;
using System.Text;

namespace ECS
{
    public class Inject : Attribute
    {
        public Type[] types;

        public Inject(params Type[] _types)
        {
            types = _types;
        }
    }
}
