using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using NUnit.Framework;
using ECS;
using ECS.interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ECSTests
{
    public struct TestComponent : IComponent
    {
        public byte value;
    }

    public struct OtherComponent : IComponent
    {
        public byte value;
    }


    [MemoryDiagnoser]
    public class ECSBenchmark
    {
        public World world;
        public List<ushort> entities = new List<ushort>();
        public byte proto;
        [IterationSetup]
        public void IterationSetup()
        {
            world = EntityManager.CreateWorld();
            proto = EntityManager.CreateEntityPrototype();
            var cc = new TestComponent() { value = 232 };
            var occ = new OtherComponent() { value = 123 };
            EntityManager.AddComponent<TestComponent>(proto, ref cc);
            EntityManager.AddComponent<OtherComponent>(proto, ref occ);
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            EntityManager.DestroyWorld(world);
            EntityManager.DestroyPrototypes();
            world = null;
        }

        [Benchmark]
        public void Create()
        {
            for (int i = 0; i < ushort.MaxValue; i++)
            {
                entities.Add(EntityManager.CreateEntity(world));
            }
        }



        [Benchmark]
        public void AddComponent()
        {
            foreach (var e in entities)
            {
                var c = new TestComponent() { value = 232 };
                var oc = new OtherComponent() { value = 123 };
                EntityManager.AddComponent<TestComponent>(world, e, ref c);
                EntityManager.AddComponent<OtherComponent>(world, e, ref oc);
            }
        }

        [Benchmark]
        public void SublimeGetComponent()
        {
            foreach (var e in entities)
            {
                TestComponent c = EntityManager.GetComponent<TestComponent>(world, e);
                EntityManager.GetComponent<OtherComponent>(world, e);
            }
        }

        [Benchmark]
        public void SublimeRemoveComponent()
        {
            foreach (var e in entities)
            {
                EntityManager.RemoveComponent<TestComponent>(world, e);
                EntityManager.RemoveComponent<OtherComponent>(world, e);

            }
        }

        [Benchmark]
        public void SublimeDestroyEntity()
        {
            foreach (var e in entities)
            {
                EntityManager.DestroyEntity(world, e);
            }
        }
    }
    public class Program
    {
        public static bool isActive = true;
        private static void Main(string[] args)
        {
            Console.WriteLine("Type benchmark to run a benchmark or test to run unit tests");
            string command = Console.ReadLine();
            if (command.Equals("benchmark", StringComparison.InvariantCultureIgnoreCase))
            {
                var summary = BenchmarkRunner.Run<ECSBenchmark>();
            }
            else if (command.Equals("test", StringComparison.InvariantCultureIgnoreCase))
            {
                RunTests();
            }
            Console.ReadLine();
        }

        private static void RunTests()
        {
            var world = EntityManager.CreateWorld();
            var entity = EntityManager.CreateEntity(world);
            var c = new TestComponent() { value = 232 };
            var oc = new OtherComponent() { value = 123 };
            EntityManager.AddComponent<TestComponent>(world, entity, ref c);
            Assert.That(EntityManager.HasComponent<TestComponent>(world, entity), Is.EqualTo(true));
            EntityManager.AddComponent<OtherComponent>(world, entity, ref oc);
            Assert.That(EntityManager.HasComponent<OtherComponent>(world, entity), Is.EqualTo(true));
            Console.WriteLine("Test stage 1 passed");

            while (isActive == true)
            {
                EntityManager.Update();
                Thread.Sleep(1000 / 60);
            }
            Console.WriteLine("All tests passed!");
            Console.ReadLine();

        }
    }


    public class TestSystem : ComponentSystem
    {
        public struct Data
        {
            public TestComponent test;
            public OtherComponent other;
        };

        [Inject(typeof(TestComponent), typeof(OtherComponent))]
        public List<ushort> entities { get; set; }

        public override void Update(World world)
        {
            Assert.That(entities.Count, Is.EqualTo(1));
            foreach (var e in entities)
            {
                Assert.That(EntityManager.GetComponent<TestComponent>(world, e).value, Is.EqualTo(232));
                Assert.That(EntityManager.GetComponent<OtherComponent>(world, e).value, Is.EqualTo(123));
            }
            Console.WriteLine("Test stage 2 passed");
            Program.isActive = false;
        }
    }
}
