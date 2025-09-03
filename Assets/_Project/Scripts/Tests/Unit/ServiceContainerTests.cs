using System;
using NUnit.Framework;
using Laboratory.Core.DI;

namespace Laboratory.Core.Tests.Unit
{
    public class ServiceContainerTests
    {
        private ServiceContainer _container;

        [SetUp]
        public void SetUp()
        {
            _container = new ServiceContainer();
        }

        [TearDown]
        public void TearDown()
        {
            _container?.Dispose();
            _container = null;
        }

        [Test]
        public void Register_InterfaceImplementation_RegistersSuccessfully()
        {
            _container.Register<ITestServiceContainer, TestServiceContainer>();
            Assert.IsTrue(_container.IsRegistered<ITestServiceContainer>());
        }

        [Test]
        public void Resolve_RegisteredService_ReturnsInstance()
        {
            _container.Register<ITestServiceContainer, TestServiceContainer>();
            var service = _container.Resolve<ITestServiceContainer>();
            Assert.IsNotNull(service);
            Assert.IsInstanceOf<TestServiceContainer>(service);
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
