using ECS.interfaces;
using System.Collections.Generic;


namespace ECS
{
    using Entity = System.UInt16;
    public class World
    {

        private readonly Stack<Entity> _entities = new Stack<Entity>();
        internal Stack<Entity> Entities { get { return _entities; } }

        private Dictionary<ushort, Stack<IComponent>> _components = new Dictionary<ushort, Stack<IComponent>>();
        internal Dictionary<Entity, Stack<IComponent>> Components { get { return _components; } set { _components = value; } }

        internal World()
        {
            for (ushort i = 0; i < Entity.MaxValue; i++)
            {
                _components.Add(i, new Stack<IComponent>());
            }
            for (ushort i = Entity.MaxValue; i > 0; i--)
            {
                _entities.Push(i);
            }
        }


        internal void Dispose()
        {
            _components.Clear();
        }

    }
}
