// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Configuration;

namespace osu.Game.Tests.NonVisual
{
    /// <summary>
    /// The server moved from rinarii.de to rinarii.xyz, and a saved config keeps whatever URL it was last given.
    /// </summary>
    [TestFixture]
    public class CustomApiUrlMigrationTest
    {
        private static string loadCustomApiUrl(string? savedValue)
        {
            using var storage = new TemporaryNativeStorage("custom-api-url-migration-test");

            if (savedValue != null)
            {
                using var stream = storage.GetStream("game.ini", FileAccess.Write, FileMode.Create);
                using var writer = new StreamWriter(stream);
                writer.WriteLine($"CustomApiUrl = {savedValue}");
            }

            using var config = new OsuConfigManager(storage);
            return config.Get<string>(OsuSetting.CustomApiUrl);
        }

        [Test]
        public void TestFreshConfigDefaultsToTheNewDomain()
        {
            Assert.That(loadCustomApiUrl(null), Is.EqualTo("lazer-api.rinarii.xyz"));
        }

        [TestCase("lazer-api.rinarii.de", "lazer-api.rinarii.xyz")]
        [TestCase("https://lazer-api.rinarii.de", "https://lazer-api.rinarii.xyz")]
        [TestCase("https://lazer-api.rinarii.de/", "https://lazer-api.rinarii.xyz/")]
        [TestCase("Lazer-API.Rinarii.DE", "Lazer-API.rinarii.xyz")]
        [TestCase("rinarii.de", "rinarii.xyz")]
        public void TestSavedOldDomainIsMovedToTheNewOne(string saved, string expected)
        {
            Assert.That(loadCustomApiUrl(saved), Is.EqualTo(expected));
        }

        [TestCase("lazer-api.rinarii.xyz")]
        [TestCase("https://lazer-api.rinarii.xyz")]
        [TestCase("my-own-server.example.com")]
        [TestCase("http://localhost:8080")]
        public void TestOtherUrlsAreLeftAlone(string saved)
        {
            Assert.That(loadCustomApiUrl(saved), Is.EqualTo(saved));
        }

        [TestCase("notrinarii.de")]
        [TestCase("rinarii.debug.example.com")]
        public void TestOnlyTheActualDomainIsRewritten(string saved)
        {
            // similar looking hosts which are not the old domain must not be touched.
            Assert.That(loadCustomApiUrl(saved), Is.EqualTo(saved));
        }
    }
}
