using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using FakeEF.Data;
using FakeEF.EFInterception;
using FakeEF.Tests.TestDatabase;
using NUnit.Framework;

namespace FakeEF.Tests
{
    [TestFixture]
    public class Mytests
    {
        [SetUp]
        public void Setup()
        {
            InMemoryDatabase.Instance.Clear();
        }

        [Test]
        public void QueryNestedPropertyWithProxy()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();

                var p1 = new Person { Name = "Hallo" };
                p1.Addresses.Add(new Adresse()
                {
                    Name = "Myaddress"
                });
                ctx.Persons.Add(p1);
                ctx.SaveChanges();
            }

            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();
                var p = ctx.Persons.FirstOrDefault();
                Assert.That(p.Addresses.Count, Is.EqualTo(1));
            }
        }
        [Test]
        public void EnsureOrderByAndWhereIsWorking()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();
                ctx.Persons.Add(new Person { Name = "C" });
                ctx.Persons.Add(new Person { Name = "A" });
                ctx.Persons.Add(new Person { Name = "B" });
                ctx.SaveChanges();
            }

            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();
                var p = ctx.Persons.OrderBy(x => x.Name).ToList();
                Assert.That(p[0].Name, Is.EqualTo("A"));
                Assert.That(p[1].Name, Is.EqualTo("B"));
                Assert.That(p[2].Name, Is.EqualTo("C"));
            }
        }

        [Test]
        public void QueryTwoDifferentTables()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();

                var p1 = new Person { Name = "Hallo" };
                p1.Addresses.Add(new Adresse()
                {
                    Name = "Myaddress"
                });
                ctx.Persons.Add(p1);
                ctx.SaveChanges();
            }

            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();
                var p = ctx.Persons.FirstOrDefault();
                var adr = ctx.Adresses.FirstOrDefault();
                Assert.That(p.Addresses.First() == adr);
            }
        }
        [Test]
        public void FirstOrDefaultNestedProperty()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();

                ctx.TestData.Add(new TestData()
                {
                    NestedData = new TestData()
                    {
                        Number = 42
                    }
                });
                ctx.SaveChanges();
            }

            using (var ctx = new MyTestDbContext())
            {
                ctx.Configuration.ProxyCreationEnabled = false;
                ctx.SetupAsTestDbContext();
                var data = ctx.TestData.FirstOrDefault(x => x.NestedData.Number == 42);
                Assert.That(data, Is.Not.Null);
            }
        }


        [Test]
        public void QueryNestedPropertyWithNoProxy()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();

                var p1 = new Person { Name = "Hallo" };
                p1.Addresses.Add(new Adresse()
                {
                    Name = "Myaddress"
                });
                ctx.Persons.Add(p1);
                ctx.SaveChanges();
            }

            using (var ctx = new MyTestDbContext())
            {
                ctx.Configuration.ProxyCreationEnabled = false;
                ctx.SetupAsTestDbContext();
                var p = ctx.Persons.FirstOrDefault();
                Assert.That(p.Addresses.Count, Is.EqualTo(0));
            }
        }

        [Test]
        public void AddPersonWithAddressesSelectMany()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();

                var p1 = new Person { Name = "Hallo" };
                for (int i = 0; i < 3; i++)
                {
                    p1.Addresses.Add(new Adresse()
                    {
                        Name = "Myaddress" + i
                    });
                }
                ctx.Persons.Add(p1);
                ctx.SaveChanges();
            }

            using (var ctx = new MyTestDbContext())
            {
                ctx.Configuration.ProxyCreationEnabled = false;
                ctx.SetupAsTestDbContext();
                var adr = ctx.Persons
                    .AsNoTracking()
                    .SelectMany(x => x.Addresses)
                    .Where(x => x.Id > 0)
                    .Where(x => x.Id < 1000)
                    .FirstOrDefault(x => x.Id == 2);
                Assert.That(adr, Is.Not.Null);
            }
        }

        [Test]
        public void SetInternalProperty()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();
                ctx.TestData.Add(new TestData());
                ctx.SaveChanges();
            }

            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();
                var p = ctx.TestData.FirstOrDefault();
                Assert.That(p.Id, Is.EqualTo(1));
            }
        }
        [Test]
        public void ChangePropertyAndSave()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();
                ctx.Persons.Add(new Person()
                {
                    Name = "Name"
                });
                ctx.SaveChanges();
            }

            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();
                var p = ctx.Persons.FirstOrDefault();
                p.Name = p.Name.ToUpper();
                ctx.SaveChanges();
            }

            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();
                var p = ctx.Persons.FirstOrDefault();
                Assert.That(p.Name, Is.EqualTo("NAME"));
            }
        }


        [Test]
        public void QueryNoProxyEnsureId()
        {
            int id = 0;
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();

                var p1 = new Person { Name = "Hallo" };
                p1.Addresses.Add(new Adresse()
                {
                    Name = "Myaddress"
                });
                ctx.Persons.Add(p1);
                ctx.SaveChanges();
                id = p1.Addresses.FirstOrDefault().Id;
            }

            using (var ctx = new MyTestDbContext())
            {
                ctx.Configuration.ProxyCreationEnabled = false;
                ctx.SetupAsTestDbContext();
                var p = ctx.Persons.Include(x => x.Addresses).FirstOrDefault();
                Assert.That(p.Addresses.FirstOrDefault().Id, Is.EqualTo(id));
            }
        }

        [Test]
        public void QueryNestedPropertyWithNoProxyAndInclude()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();

                var p1 = new Person { Name = "Hallo" };
                p1.Addresses.Add(new Adresse()
                {
                    Name = "Myaddress"
                });
                ctx.Persons.Add(p1);
                ctx.SaveChanges();
            }

            using (var ctx = new MyTestDbContext())
            {
                ctx.Configuration.ProxyCreationEnabled = false;
                ctx.SetupAsTestDbContext();

                var p = ctx.Persons.Include(x => x.Addresses).FirstOrDefault();
                Assert.That(p.Addresses.Count, Is.EqualTo(1));
            }
        }
        [Test]
        public void QueryNestedPropertyWithNoProxyAndIncludeOneLevel()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();

                var p1 = new Person
                {
                    Name = "Hallo1",
                    ChildPerson = new Person
                    {
                        Name = "Hallo2",
                        ChildPerson = new Person
                        {
                            Name = "Hallo3"
                        }
                    }
                };
                ctx.Persons.Add(p1);
                ctx.SaveChanges();
            }

            using (var ctx = new MyTestDbContext())
            {
                ctx.Configuration.ProxyCreationEnabled = false;
                ctx.SetupAsTestDbContext();

                var p = ctx.Persons.Include(x => x.ChildPerson).FirstOrDefault();
                Assert.That(p.ChildPerson, Is.Not.Null);
            }
        }
        [Test]
        public void QueryNestedPropertyWithNoProxyAndIncludeTwoLevel()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();

                var p1 = new Person
                {
                    Name = "Hallo1",
                    ChildPerson = new Person
                    {
                        Name = "Hallo2",
                        Addresses = new List<Adresse>()
                        {
                          new Adresse()
                          {
                              Name = "Adr"
                          }  
                        },
                        ChildPerson = new Person
                        {
                            Name = "Hallo3"
                        }
                    }
                };
                ctx.Persons.Add(p1);
                ctx.SaveChanges();
            }

            using (var ctx = new MyTestDbContext())
            {
                ctx.Configuration.ProxyCreationEnabled = false;
                ctx.SetupAsTestDbContext();

                var p = ctx.Persons.Include(x => x.ChildPerson.Addresses).FirstOrDefault();
                Assert.That(p.ChildPerson.Addresses.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void AddTwoUniqueEntries()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();

                var p1 = new Person { Name = "Hallo" };
                ctx.Persons.Add(p1);
                ctx.SaveChanges();

                var p2 = new Person { Name = "J�rgen" };
                ctx.Persons.Add(p2);
                ctx.SaveChanges();

                Assert.That(p1.Id, Is.EqualTo(1));
                Assert.That(p2.Id, Is.EqualTo(2));
            }
        }

        [Test]
        public void UseTwoDifferentContexts()
        {
            using (var ctx1 = new MyTestDbContext())
            {
                ctx1.SetupAsTestDbContext();

                var p1 = new Person { Name = "J�rgen" };
                p1.Addresses.Add(new Adresse { Name = "AlterWert1" });
                p1.Addresses.Add(new Adresse { Name = "AlterWert2" });
                ctx1.Persons.Add(p1);
                ctx1.SaveChanges();
            }

            using (var ctx2 = new MyTestDbContext())
            {
                ctx2.SetupAsTestDbContext();
                var persons = ctx2.Persons;

                Assert.AreEqual(2, persons.First().Addresses.Count());
            }

            Person p2;
            using (var ctx3 = new MyTestDbContext())
            {
                ctx3.SetupAsTestDbContext();
                p2 = ctx3.Persons.FirstOrDefault(x => x.Id == 1);
                p2.Name = "�berschriebenerWert";
                p2.Addresses.First().Name = "Hallo";
            }

            using (var ctx4 = new MyTestDbContext())
            {
                ctx4.SetupAsTestDbContext();
                ctx4.Entry(p2).State = EntityState.Modified;
                ctx4.SaveChanges();
            }

            using (var ctx5 = new MyTestDbContext())
            {
                ctx5.SetupAsTestDbContext();
                var p3 = ctx5.Persons.FirstOrDefault(x => x.Id == 1);


                Assert.That(p2, Is.Not.EqualTo(p3));
                Assert.That(p3.Name, Is.EqualTo("�berschriebenerWert"));
                Assert.That(p3.Addresses.First().Name, Is.EqualTo("AlterWert1"));
            }
        }

        [Test]
        public void Performance()
        {
            for (int i = 0; i < 100; i++)
            {
                using (var ctx5 = new MyTestDbContext())
                {
                    ctx5.SetupAsTestDbContext();
                    var p3 = ctx5.Persons.FirstOrDefault(x => x.Id == 1);
                }
            }
        }

        [Test]
        public void FindMethod()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();

                var p1 = new Person { Name = "J�rgen" };
                ctx.Persons.Add(p1);
                ctx.SaveChanges();

                var p = ctx.Persons.Find(1);
                Assert.That(p.Name, Is.EqualTo(p1.Name));
            }
        }

        [Test]
        public void DeleteItem()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();

                var p1 = new Person { Name = "J�rgen" };
                ctx.Persons.Add(p1);
                ctx.SaveChanges();

                var p = ctx.Persons.FirstOrDefault(x => x.Id == 1);
                ctx.Persons.Remove(p);
                ctx.SaveChanges();

                Assert.That(ctx.Persons.Count(), Is.EqualTo(0));
            }
        }

        [Test]
        public void AddItemWithRequiredParentAndExpectException()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();
                ctx.Adresses.Add(new Adresse
                {
                    Name = "test",
                    Person = null //Das dar nicht sein!
                });
                Assert.Throws<DbEntityValidationException>(() => ctx.SaveChanges());
            }
        }

        [Test]
        public void AsNoTrackingDifferentInstances()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();
                var p1 = new Person { Name = "J�rgen" };
                ctx.Persons.Add(p1);
                ctx.SaveChanges();
            }
            using (var ctx = new MyTestDbContext())
            {
                ctx.Configuration.ProxyCreationEnabled = false;
                ctx.SetupAsTestDbContext();

                var p1 = ctx.Persons.AsNoTracking().FirstOrDefault();
                var p2 = ctx.Persons.AsNoTracking().FirstOrDefault();
                Assert.That(p1 != p2);
            }
        }

        [Test]
        public void SameInstancesInContext()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();
                var p1 = new Person { Name = "J�rgen" };
                ctx.Persons.Add(p1);
                ctx.SaveChanges();
            }
            using (var ctx = new MyTestDbContext())
            {
                ctx.Configuration.ProxyCreationEnabled = false;
                ctx.SetupAsTestDbContext();

                var p1 = ctx.Persons.FirstOrDefault();
                var p2 = ctx.Persons.FirstOrDefault();
                Assert.That(p1 == p2);
            }
        }

        [Test]
        public void TestJoinOperation()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();
                var person = new Person
                {
                    Name = "Eintrag"
                };

                foreach (var adr in Enumerable.Range(0, 10).Select(x => new Adresse { Name = x.ToString() }))
                    person.Addresses.Add(adr);

                ctx.Persons.Add(person);
                ctx.SaveChanges();

                var query = from p in ctx.Persons
                            from a in ctx.Adresses
                            where p.Id == a.Id
                            select a;

                Assert.That(ctx.Persons.Count(), Is.EqualTo(1));
                Assert.That(ctx.Adresses.Count(), Is.EqualTo(10));
                Assert.That(query.Count(), Is.EqualTo(1));
            }
        }

        [Test]
        public void GetItemTwice()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();

                var person = new Person
                {
                    Name = "Eintrag"
                };
                ctx.Persons.Add(person);
                ctx.SaveChanges();

                var dbEntry = ctx.Persons.FirstOrDefault(x => x.Id == 1);
                Assert.That(person, Is.SameAs(dbEntry));
                Assert.That(person.Id, Is.EqualTo(dbEntry.Id));
            }
        }
    }
}