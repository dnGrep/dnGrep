using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnGREP.WPF;
using MbUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class UITest
    {
        MainForm mainForm;

        [FixtureSetUp]
        public void Initialize()
        {
            mainForm = new MainForm();
        }

        [FixtureTearDown]
        public void TestCleanup()
        {
            mainForm = null;
        }
    }
}
