using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Viperinius.Plugin.SpotifyImport.Sync;
using Viperinius.Plugin.SpotifyImport.Tests.TestHelpers;
using Xunit;

namespace Viperinius.Plugin.SpotifyImport.Tests.Sync
{
    public class StringMatchFinderTests
    {
        [Theory]
        [InlineData("Daryl Hall & John Oates", "daryl")]
        [InlineData("✝✝✝ (Crosses)", "cross")]
        [InlineData("$uicideboy$", "uicid")]
        [InlineData("AC/DC", "ac dc")]
        [InlineData("Mötley Crüe", "motle")]
        [InlineData("Muse", "muse")]
        [InlineData("!!!", "!!!")]
        public void GetArtistSearchTerm_SkipsUnsearchableCharacters(string artistName, string expected)
        {
            Assert.Equal(expected, StringMatchFinder.GetArtistSearchTerm(artistName));
        }

        [Fact]
        public void FindTrack_QueriesArtistsOncePerProviderArtist()
        {
            var prov = TrackHelper.CreateProviderItem("Track", "Album", new List<string> { "✝✝✝ (Crosses)" }, new List<string> { "✝✝✝ (Crosses)" });
            var jfArtists = Enumerable.Range(0, 10)
                .Select(i => ((BaseItem)ArtistHelper.CreateJfItem($"Other Artist {i}", new List<MusicAlbum>()), new ItemCounts()))
                .ToList();

            var libManagerMock = Substitute.For<ILibraryManager>();
            libManagerMock
                .GetArtists(Arg.Any<InternalItemsQuery>())
                .Returns(_ => new QueryResult<(BaseItem, ItemCounts)>(jfArtists));
            libManagerMock
                .GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(_ => []);

            var finder = new StringMatchFinder(Substitute.For<ILogger>(), libManagerMock);

            Assert.Null(finder.FindTrack(string.Empty, prov));
            libManagerMock.Received(1).GetArtists(Arg.Any<InternalItemsQuery>());
            libManagerMock.Received(1).GetArtists(Arg.Is<InternalItemsQuery>(q => q.SearchTerm == "cross"));
        }
    }
}
