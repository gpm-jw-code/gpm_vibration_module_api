using Microsoft.VisualStudio.TestTools.UnitTesting;
using JMAlgorithm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JMAlgorithm.Tests
{
    [TestClass()]
    public class ExtensionTests
    {
        [TestMethod()]
        public void RootMeanSquareTest()
        {
            double[] dataLs = new double[2] { 1,3};
            double rms = dataLs.RootMeanSquare();
            Assert.IsTrue(Math.Round(rms,2)== 2.24);
        }
    }
}