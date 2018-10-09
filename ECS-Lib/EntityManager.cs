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

        private static Stack<ComponentSystem> s_systems = new Stack<ComponentSystem>();

        //Used to keep track of weather or not the needed references are in place for dependency injection
        private static bool s_isInit = false;

        private static Dictionary<object, List<FieldInfo>> s_injectorList = new Dictionary<object, List<FieldInfo>>();

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

                        
                        //finds a list of fields
                        var fields = o.GetType().GetFields();
                        //loops through the said properties.
                        foreach (var field in fields)
                        {
                            //Gets its custom attribute
                            var i = field.GetCustomAttribute<Inject>();
                            if(field.GetType().GetInterfaces().Contains(typeof(IComponentData)))
                            {
                                throw new InvalidOperationException("Value must be a list of IComponentData");
                            }
                            //if the attribute is null that means it doesn't have it so don't cache the property.
                            if (i != null)
                            {
                                var injectedObject = Activator.CreateInstance(field.FieldType);
                               
                                FieldInfo[] injectableFields = injectedObject.GetType().GetFields();
                                s_injectorList.Add(injectedObject, null);
                                foreach (FieldInfo injectedField in injectableFields)
                                {
                                    var fieldType = injectedField.FieldType.GetGenericArguments()[0].GetInterfaces();
                                    if (fieldType.Contains(typeof(IComponent)))
                                    {
                                        var typeInstance = Activator.CreateInstance(injectedField.FieldType);
                                       
                                        
                                        if (s_injectorList[injectedObject] == null)
                                            s_injectorList[injectedObject] = new List<FieldInfo>();

                                        s_injectorList[injectedObject].Add(field);
                                        injectedField.SetValue(injectedObject, typeInstance);
                                    }
                                    else
                                    {
                                        throw new InvalidOperationException("Value must be a list of IComponent");
                                    }


                                }
                                field.SetValue(o, injectedObject);
                                s_systems.Push((ComponentSystem)o); 

                                
                            /*
                            var val = property.GetValue(o);
                            var reff = __makeref(val);
                            var injectedObject = [rp]

                            FieldInfo[] fields = injectedObject.GetType().GetFields();
                            s_injectorList.Add(injectedObject.GetValue(o), null);
                            foreach (FieldInfo field in fields)
                            {

                                var fieldType = field.FieldType.GetGenericArguments()[0].GetInterfaces();
                                if (fieldType.Contains(typeof(IComponent)))
                                {
                                    var typeInstance = Activator.CreateInstance(field.FieldType);
                                    field.SetValue(injectedObject.GetValue(o), typeInstance);
                                    if (s_injectorList[injectedObject.GetValue(o)] == null)
                                        s_injectorList[injectedObject.GetValue(o)] = new List<FieldInfo>();

                                    s_injectorList[injectedObject.GetValue(0)].Add( field);


                                }
                                else
                                {
                                    throw new InvalidOperationException("Value must be a list of IComponent");
                                }


                            }
                            property.SetValue(o, injectedObject.GetValue(o));
                            s_systems.Push((ComponentSystem)o);*/

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
            if (!s_isInit)
                return;
            foreach (World world in s_worlds)
            {
                foreach (var system in s_systems)
                {
                    system.Update(world);
                }
            }
        }
        public static void AddComponent<T>(World world, ushort entityID, ref T value) where T : struct, IComponent
        {
            world.Components[entityID].Push(value);
            foreach(var kvp in s_injectorList)
            {
                foreach(var field in kvp.Value)
                {
                    if(field.GetType() == typeof(T))
                    {
                        ((List<IComponent>)field.GetValue(kvp.Key)).Add(value); 
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
