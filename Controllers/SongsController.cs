using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Dawn;

namespace NCTSongApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SongsController : ControllerBase
    {
        private const string BestListUrl = "https://www.nhaccuatui.com/playlist/top-100-nhac-tre-hay-nhat-va.m3liaiy6vVsF.html";
        private readonly ILogger<SongsController> _logger;

        public SongsController(ILogger<SongsController> logger)
        {
            _logger = Guard.Argument(logger, nameof(logger)).NotNull().Value;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<Song>>> Get()
        {
            dynamic playlist = null;

            using (var httpClient = new HttpClient())
            {
                string playlistUrl = null;

                using (var response = await httpClient.GetStreamAsync(BestListUrl))
                {
                    using (StreamReader reader = new StreamReader(response))
                    {
                        while (!reader.EndOfStream)
                        {
                            string currentLine = await reader.ReadLineAsync();
                            if (currentLine.Contains("key2"))
                            {
                                // extract the call to get the list
                                string[] parts = currentLine.Split('"');
                                if (parts.Length == 3)
                                {
                                    playlistUrl = parts[1];
                                    _logger.LogInformation($"Found URL for playlist: {playlistUrl}");

                                }
                            }
                        }
                    }
                }

                if (playlistUrl == null)
                {
                    _logger.LogError("Could not find playlist url in the page.");
                    return NotFound();
                }

                using (var playlistResponse = await httpClient.GetAsync(playlistUrl))
                {
                    XDocument doc = XDocument.Parse(await playlistResponse.Content.ReadAsStringAsync());
                    string jsonText = JsonConvert.SerializeXNode(doc);
                    playlist = JsonConvert.DeserializeObject<ExpandoObject>(jsonText);
                }
            }

            var songs = new List<Song>();
            foreach (var track in playlist.tracklist.track)
            {
                var song = new Song
                {
                    Title = track.title.GetValue<string>("#cdata-section"),
                    Author = track.creator.GetValue<string>("#cdata-section"),
                    Url = track.location.GetValue<string>("#cdata-section")
                };

                songs.Add(song);
            }

            _logger.LogInformation($"Returning {songs.Count} songs");
            return Ok(songs);
        }
    }
}
