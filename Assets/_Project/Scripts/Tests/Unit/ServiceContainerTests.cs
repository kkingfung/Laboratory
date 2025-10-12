using System;
using NUnit.Framework;
using UnityEngine;
using Laboratory.Core.Infrastructure;

namespace Laboratory.Core.Tests.Unit
{
    public class ServiceContainerTests
    {
        private ServiceContainer _container;
        private GameObject _containerGameObject;

        [SetUp]
        public void SetUp()
        {
            _containerGameObject = new GameObject("TestServiceContainer");
            _container = _containerGameObject.AddComponent<ServiceContainer>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_containerGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_containerGameObject);
            }
            _container = null;
        }

        [Test]
        public void RegisterService_InterfaceImplementation_RegistersSuccessfully()
        {
            var testService = new TestServiceContainer();
            _container.RegisterService<ITestServiceContainer>(testService);
            Assert.IsTrue(_container.IsServiceRegistered<ITestServiceContainer>());
        }

        [Test]
        public void ResolveService_RegisteredService_ReturnsInstance()
        {
            var testService = new TestServiceContainer();
            _container.RegisterService<ITestServiceContainer>(testService);
            var service = _container.ResolveService<ITestServiceContainer>();
            Assert.IsNotNull(service);
            Assert.IsInstanceOf<TestServiceContainer>(service);
            Assert.AreSame(testService, service);
        }

        [Test]
        public void ResolveService_UnregisteredService_ReturnsNull()
        {
            var service = _container.ResolveService<ITestServiceContainer>();
            Assert.IsNull(service);
        }
    }

    public interface ITestServiceContainer
    {
        string GetValue();
    }

    public class TestServiceContainer : ITestServiceContainer
    {
        public string GetValue() => "Test";
    }
}
