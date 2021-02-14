using System;
using System.Collections.Generic;
using System.Text;
using Aura.Data;
using NUnit.Framework;

namespace Aura.Tests
{
	[TestFixture]
	public class ContentLicenseTests
	{
		[Test]
		public void Name()
		{
			const string name = "name";
			var license = ContentLicense.GetLicense (name);
			Assert.That (license.Name, Is.EqualTo (name));
			Assert.That (license.Url, Is.Null);
		}

		[Test]
		public void RandomUrl()
		{
			const string url = "http://google.com";
			var license = ContentLicense.GetLicense (url);
			Assert.That (license.Name, Is.Null);
			Assert.That (license.Url, Is.EqualTo (url));
		}

		[Test]
		public void CreativeCommonsPublicDomain()
		{
			const string url = "https://creativecommons.org/publicdomain/zero/1.0/";
			var license = ContentLicense.GetLicense (url);
			Assert.That (license.Name, Is.EqualTo ("CC0 1.0"));
			Assert.That (license.Url, Is.EqualTo (url));
		}

		[Test]
		public void CreativeCommonsLicense ()
		{
			const string url = "https://creativecommons.org/licenses/by-nd/4.0/";
			var license = ContentLicense.GetLicense (url);
			Assert.That (license.Name, Is.EqualTo ("CC BY-ND 4.0"));
			Assert.That (license.Url, Is.EqualTo (url));
		}
	}
}
