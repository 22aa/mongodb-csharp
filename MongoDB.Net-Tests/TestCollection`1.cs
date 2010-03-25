﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MongoDB.Driver.Attributes;

using NUnit.Framework;

namespace MongoDB.Driver
{
    [TestFixture]
    public class TestCollection_1 : MongoTestBase
    {
        const string POUND = "\u00a3";

        private class FindsEntity
        {
            public int x { get; set; }

            [MongoName("h")]
            public string Text { get; set; }

            [MongoName("j")]
            public int Index { get; set; }
        }

        private class CharReadsEntity
        {
            public string test { get; set; }
        }

        private class InsertsEntity
        {
            [MongoName("song")]
            public string Song { get; set; }

            [MongoName("artist")]
            public string Artist { get; set; }

            [MongoName("year")]
            public int Year { get; set; }
        }

        private class CountsEntity
        {
            public string Last { get; set; }

            public string First { get; set; }

            [MongoName("cnt")]
            public int Count { get; set; }
        }

        public override string TestCollections
        {
            get { return "inserts,updates,counts,counts_spec,finds,charreads,saves"; }
        }

        public override void OnInit()
        {
            var finds = DB["finds"];
            for (var j = 1; j < 100; j++)
                finds.Insert(new Document { { "x", 4 }, { "h", "hi" }, { "j", j } });
            for (var j = 100; j < 105; j++)
                finds.Insert(new Document { { "x", 4 }, { "n", 1 }, { "j", j } });
            var charreads = DB["charreads"];
            charreads.Insert(new Document { { "test", "1234" + POUND + "56" } });
        }

        [Test]
        public void TestFindAttributeLimit()
        {
            var query = new { Index = 10 };
            var fields = new { x = 1 };
            var c = DB.GetCollection<FindsEntity>("finds").Find(query, -1, 0, fields);
            foreach (var result in c.Documents)
            {
                Assert.IsNotNull(result);
                Assert.AreEqual(4, result.x);
                Assert.AreEqual(0, result.Index);
            }
        }

        [Test]
        public void TestFindGTRange()
        {
            var query = new { Index = Op.GreaterThan(20) };
            var c = DB.GetCollection<FindsEntity>("finds").Find(query);
            foreach (var result in c.Documents)
            {
                Assert.IsNotNull(result);
                Assert.Greater(result.Index, 20);
            }
        }

        [Test]
        public void TestFindNulls()
        {
            var query = new { n = (string)null };
            var numnulls = DB.GetCollection<FindsEntity>("finds").Count(query);
            Assert.AreEqual(99, numnulls);
        }

        [Test]
        public void TestFindOne()
        {
            var query = new { Index = 10 };
            var result = DB.GetCollection<FindsEntity>("finds").FindOne(query);
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.x);
            Assert.AreEqual(10, result.Index);
        }

        [Test]
        public void TestFindOneNotThere()
        {
            var query = new { not_there = 10 };
            var result = DB.GetCollection<FindsEntity>("finds").FindOne(query);
            Assert.IsNull(result);
        }

        [Test]
        public void TestFindOneObjectContainingUKPound()
        {
            var query = new Document();
            var result = DB.GetCollection<CharReadsEntity>("charreads").FindOne(query);
            Assert.IsNotNull(result);
            Assert.AreEqual("1234£56", result.test);
        }

        [Test]
        public void TestFindWhereEquivalency()
        {
            var col = DB.GetCollection<FindsEntity>("finds");
            var lt = new { Index = Op.LessThan(5) };
            var where = "this.j < 5";
            var explicitWhere = new Document().Add("$where", new Code(where));
            var func = new CodeWScope("function() { return this.j < 5; }", new Document());
            var funcDoc = new Document().Add("$where", func);

            Assert.AreEqual(4, col.Find(lt).Documents.Count(), "Basic find didn't return 4 docs");
            Assert.AreEqual(4, col.Find(where).Documents.Count(), "String where didn't return 4 docs");
            Assert.AreEqual(4, col.Find(explicitWhere).Documents.Count(), "Explicit where didn't return 4 docs");
            Assert.AreEqual(4, col.Find(funcDoc).Documents.Count(), "Function where didn't return 4 docs");
        }

        [Test]
        public void TestArrayInsert()
        {
            var inserts = DB.GetCollection<InsertsEntity>("inserts");
            var indoc1 = new { Song = "The Axe", Artist = "Tinsley Ellis", Year = 2006 };
            var indoc2 = new { Song = "The Axe2", Artist = "Tinsley Ellis2", Year = 2008 };

            inserts.Insert(new[] { indoc1, indoc2 });

            var result = inserts.FindOne(new Document().Add("Song", "The Axe"));
            Assert.IsNotNull(result);
            Assert.AreEqual(2006, result.Year);

            result = inserts.FindOne(new Document().Add("Song", "The Axe2"));
            Assert.IsNotNull(result);
            Assert.AreEqual(2008, result.Year);
        }

        [Test]
        public void TestCount()
        {
            var counts = DB.GetCollection<CountsEntity>("counts");
            var top = 100;
            for (var i = 0; i < top; i++)
                counts.Insert(new Document().Add("Last", "Cordr").Add("First", "Sam").Add("Count", i));
            var cnt = counts.Count();
            Assert.AreEqual(top, cnt, "Count not the same as number of inserted records");
        }

        [Test]
        public void TestCountInvalidCollection()
        {
            var counts = DB.GetCollection<CountsEntity>("counts_wtf");
            Assert.AreEqual(0, counts.Count());
        }

        [Test]
        public void TestCountWithSpec()
        {
            var counts = DB.GetCollection<CountsEntity>("counts_spec");
            counts.Insert(new { Last = "Cordr", First = "Sam", Count = 1 });
            counts.Insert(new { Last = "Cordr", First = "Sam", Count = 2 });
            counts.Insert(new { Last = "Corder", First = "Sam", Count = 3 });

            Assert.AreEqual(2, counts.Count(new { Last = "Cordr" }));
            Assert.AreEqual(1, counts.Count(new { Last = "Corder" }));
            Assert.AreEqual(0, counts.Count(new { Last = "Brown" }));
        }
    }
}