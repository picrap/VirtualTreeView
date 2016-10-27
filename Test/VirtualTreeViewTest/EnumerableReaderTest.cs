// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeViewTest
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using VirtualTreeView.Collection.Reader;

    [TestClass]
    public class EnumerableReaderTest
    {
        [TestMethod]
        public void AnyOnEmpty()
        {
            var r = new EnumerableCollectionReader(new int[0]);
            Assert.IsFalse(r.Any);
        }

        [TestMethod]
        public void AnyOnNonEmpty()
        {
            var r = new EnumerableCollectionReader(new[] { 1 });
            Assert.IsTrue(r.Any);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void FirstOnEmpty()
        {
            var r = new EnumerableCollectionReader(new int[0]);
            var f = r.First;
        }

        [TestMethod]
        public void FirstOnSingleton()
        {
            var r = new EnumerableCollectionReader(new[] { 2 });
            var f = (int)r.First;
            Assert.AreEqual(2, f);
        }

        [TestMethod]
        public void FirstOnMultiple()
        {
            var r = new EnumerableCollectionReader(new[] { 3, 4, 5 });
            var f = (int)r.First;
            Assert.AreEqual(3, f);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void LastOnEmpty()
        {
            var r = new EnumerableCollectionReader(new int[0]);
            var f = r.Last;
        }

        [TestMethod]
        public void LastOnSingleton()
        {
            var r = new EnumerableCollectionReader(new[] { 6 });
            var f = (int)r.Last;
            Assert.AreEqual(6, f);
        }

        [TestMethod]
        public void LastOnMultiple()
        {
            var r = new EnumerableCollectionReader(new[] { 7, 8, 9 });
            var f = (int)r.Last;
            Assert.AreEqual(9, f);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void AtOutOfRange()
        {
            var r = new EnumerableCollectionReader(new[] { 10, 11, 12 });
            var f = r.At(3);
        }

        [TestMethod]
        public void AtLast()
        {
            var r = new EnumerableCollectionReader(new[] { 13, 14 });
            var f = (int)r.At(1);
            Assert.AreEqual(14, f);
        }


        [TestMethod]
        public void AtAny()
        {
            var r = new EnumerableCollectionReader(new[] { 15, 16, 17 });
            var f = (int)r.At(1);
            Assert.AreEqual(16, f);
        }

    }
}
