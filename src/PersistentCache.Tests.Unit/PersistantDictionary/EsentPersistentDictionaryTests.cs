using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PersistentCache.DiskCache;

namespace PersistentCache.Tests.Unit.PersistantDictionary
{
    [TestFixture]
    public class EsentPersistentDictionaryTests
    {
        [Test]
        public void PersistantDictionary_DisposesEsentDatabaseCorrectly()
        {
            // Arrange & Act
            var dic = new EsentPersistentDictionary(".\\test");
            dic.Put("key", "value");
            dic.Dispose();

            // Assert
            Assert.IsFalse(Directory.Exists(".\\test"));
        }

        [Test]
        public void PersistantDictionary_CanStoreAndRetriveStrings()
        {
            // Arrange & Act
            var dic = new EsentPersistentDictionary(".\\test");
            dic.Put("key", "value");

            //Assert
            Assert.AreEqual("value", dic.Get<string>("key"));
            dic.Dispose();
        }


        [Test]
        public void PersistantDictionary_CanStoreAndRetriveSimpleclass()
        {
            // Arrange & Act
            var dic = new EsentPersistentDictionary(".\\test");
            dic.Put("key", new SimpleClass { Email = "andy@theouteredge.co.uk", Name = "Andy Long" });

            var result = dic.Get<SimpleClass>("key");

            //Assert
            Assert.AreEqual(typeof(SimpleClass), result.GetType());
            Assert.AreEqual("andy@theouteredge.co.uk", result.Email);
            Assert.AreEqual("Andy Long", result.Name);

            dic.Dispose();
        }

        [Test]
        public void PersistantDictionary_CanStoreAndRetriveComplexClass()
        {
            // Arrange & Act
            var dic = new EsentPersistentDictionary(".\\test");
            dic.Put("key", new ComplexClass
                {
                    Email = "andy@theouteredge.co.uk", Name = "Andy Long", 
                    Person = new SimpleClass { Email = "andyx@theouteredge.co.uk", Name = "x Andy Long" },
                    ListOfPeople = new List<SimpleClass>()
                        {
                            new SimpleClass { Email = "andy1@theouteredge.co.uk", Name = "1 Andy Long" },
                            new SimpleClass { Email = "andy2@theouteredge.co.uk", Name = "2 Andy Long" }
                        }
                }); 

            var result = dic.Get<ComplexClass>("key");

            //Assert
            Assert.AreEqual(typeof(ComplexClass), result.GetType());
            Assert.AreEqual("andy@theouteredge.co.uk", result.Email);
            Assert.AreEqual("Andy Long", result.Name);

            Assert.AreEqual(result.Person.Name, "x Andy Long");
            Assert.AreEqual(result.ListOfPeople.Count, 2);

            dic.Dispose();
        }


        [Test]
        [Ignore]
        public void PersistantDictionary_DisposesEsentDatabaseCorrectly_WeCanCreateStoreDisposeWithMoreThan2048Instances()
        {
            for (var i = 0; i < 2048; i++)
            {
                var dic = new EsentPersistentDictionary(string.Format(".\\{0}", i));
                dic.Put("key", "value");

                Assert.AreEqual("value", dic.Get<string>("key"));

                dic.Dispose();
            }
        }
    }
}