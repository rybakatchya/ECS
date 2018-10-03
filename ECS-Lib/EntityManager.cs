using ECS.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace ECS
{
    public class EntityManager
    {
        //List of active prototypes
        private static readonly List<Stack<IComponent>> prototypes = new List<Stack<IComponent>>();

        //List of active worlds. 
        private static readonly List<World> s_worlds = new List<World>();

        //Used to keep track of weather or not the needed references are in place for dependency injection
        private static bool s_isInit = false;

        //List of tuples. this is used to inject dependencies to componentsystems.
        private static readonly List<Tuple<ComponentSystem, List<Type>, PropertyInfo>> s_injectorList = new List<Tuple<ComponentSystem, List<Type>, PropertyInfo>>();

        public static byte CreateEntityPrototype()
        {
            var comps = new Stack<IComponent>();
            prototypes.Add(comps);
            byte returnVal;
            if (byte.TryParse(prototypes.IndexOf(comps).ToString(), out returnVal))
            {
                return returnVal;
            }
            else
            {
                throw new Exception("Maximum prototypes reached.");
            }
        }

        public static void AddComponent<T>(byte protoID, ref T value) where T : struct, IComponent
        {
            prototypes[protoID].Push(value);
        }

        public static bool HasComponent<T>(byte protoID)
        {
            return prototypes[protoID].OfType<T>().Any();
        }

        public static T GetComponent<T>(byte protoID)
        {
            foreach (var c in prototypes[protoID])
            {
                if (c.GetType() == typeof(T))
                {
                    return (T)c;
                }
            }
            throw new Exception("Prototype does not have component. Always check first with EntityManager.HasComponent");
        }


        public static void RemoveComponent<T>(byte protoID) where T : struct, IComponent
        {
            prototypes[protoID].Remove(typeof(T));
        }
        /// <summary>
        /// Initializes the entity system internally. This is only when the very first world is created.
        /// Looks for ComponentSystems and caches any properties within that have the appropriate [Inject] 
        /// attribute to it.
        /// </summary>
        private static void Init()
        {

            var a = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in a)
            {

                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (type.BaseType == typeof(ComponentSystem))
                    {
                        //Create an instance of the system so its update method can start being called.
                        object o = Activator.CreateInstance(type, true);
                        //finds a list of properties
                        var properties = o.GetType().GetProperties();
                        //loops through the said properties.
                        foreach (var property in properties)
                        {
                            //Gets its custom attribute
                            var i = property.GetCustomAttribute<Inject>();
                            //if the attribute is null that means it doesn't have it so don't cach the property.
                            if (i != null)
                            {
                                //Give the property a new list.
                                property.SetValue(o, new List<ushort>());

                                //cache the object instance of said component system, the list of types passed with the inject attribute and any properties to inject to.
                                s_injectorList.Add(new Tuple<ComponentSystem, List<Type>, PropertyInfo>((ComponentSystem)o, i.types.ToList(), property));
                            }
                        }
                    }
                }
            }
            s_isInit = true;
        }

        /// <summary>
        /// Creates a new world for entities.
        /// </summary>
        /// <returns></returns>
        public static World CreateWorld()
        {
            if (!s_isInit)
            {
                Init();
            }
            var world = new World();
            s_worlds.Add(world);
            return world;
        }


        /// <summary>
        /// Destroys a world and all entities inside of it.
        /// </summary>
        /// <param name="world">The world to destroy.</param>
        public static void DestroyWorld(World world)
        {
            world.Dispose();
            s_worlds.Remove(world);
        }

        public static void DestroyPrototypes()
        {
            prototypes.Clear();
        }
        /// <summary>
        /// Create an entity in the given world.
        /// </summary>
        /// <param name="world">The world to create the entity in.</param>
        /// <returns>entity id</returns>
        public static ushort CreateEntity(World world)
        {
            return world.Entities.Pop();
        }


        /// <summary>
        /// Destroys an entity in the given world
        /// </summary>
        /// <param name="world">The world to destroy the entity in.</param>
        /// <param name="entityID">The id of the entity to destroy.</param>
        public static void DestroyEntity(World world, ushort entityID)
        {
            if (world.Entities.Contains(entityID))
            {
                return;
            }
            world.Components[entityID].Clear();
            world.Entities.Push(entityID);
        }

        /// <summary>
        /// Updates all component systems in  
        /// </summary>
        public static void Update()
        {
            foreach (var v in s_injectorList)
            {
                foreach (var w in s_worlds)
                {
                    v.Item1.Update(w);
                }
            }
        }
        public static void AddComponent<T>(World world, ushort entityID, ref T value) where T : struct, IComponent
        {
            world.Components[entityID].Push(value);
            foreach (var v in s_injectorList)
            {
                foreach (var type in v.Item2)
                {
                    if (typeof(T) == type)
                    {
                        if (((List<ushort>)v.Item3.GetValue(v.Item1)).Contains(entityID) == false)
                        {
                            ((List<ushort>)v.Item3.GetValue(v.Item1)).Add(entityID);
                        }
                    }
                }
            }
        }

        public static void RemoveComponent<T>(World world, ushort entityID) where T : struct, IComponent
        {
            if (world.Entities.Contains(entityID))
            {
                throw new Exception("Entity does not exist");
            }
            world.Components[entityID] = (Stack<IComponent>)world.Components[entityID].Remove(typeof(T));
            foreach (var v in s_injectorList)
            {
                foreach (var type in v.Item2)
                {
                    if (typeof(T) == type)
                    {
                        ((List<IComponent>)v.Item3.GetValue(v.Item1)).Remove(type);
                    }
                }
            }
        }

        public static bool HasComponent<T>(World world, ushort entityID)
        {
            return world.Components[entityID].OfType<T>().Any();
        }

        public static T GetComponent<T>(World world, ushort entityID)
        {
            if (world.Entities.Contains(entityID))
            {
                throw new Exception("Entity does not exist");
            }

            foreach (var c in world.Components[entityID])
            {
                if (c.GetType() == typeof(T))
                {
                    return (T)c;
                }
            }
            throw new Exception("Entity does not have component. Always check first with EntityManager.HasComponent");
        }


        public static ushort[] GetEntities(World world, params Type[] types)
        {
            List<ushort> ents = new List<ushort>();
            foreach (var kvp in world.Components)
            {

                foreach (var c in kvp.Value)
                {
                    foreach (var t in types)
                    {

                        if (c.GetType() == t)
                        {
                            ents.Add(kvp.Key);
                        }
                    }
                }

            }
            return ents.ToArray();
        }


    }

    internal static class utility
    {
        internal static IEnumerable<T> Remove<T>(this IEnumerable<T> items, Type removeThese)
        {
            return items.Where(i => !removeThese.IsInstanceOfType(i));
        }

        public static Stack<T> Clone<T>(this Stack<T> stack)
        {
            var arr = new T[stack.Count];
            stack.CopyTo(arr, 0);
            return new Stack<T>(arr);
        }

    }
}
