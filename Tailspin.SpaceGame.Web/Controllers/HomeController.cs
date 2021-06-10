using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tailspin.SpaceGame.Web.Models;
using TailSpin.SpaceGame.Web.Models;

namespace TailSpin.SpaceGame.Web.Controllers
{
    public class HomeController : Controller
    {
        // High score repository.
        private readonly IDocumentDBRepository<Score> _scoreRepository;
        // User profile repository.
        private readonly IDocumentDBRepository<Profile> _profileRespository;
        private ConfigOptions _config;

        public HomeController(
            IDocumentDBRepository<Score> scoreRepository,
            IDocumentDBRepository<Profile> profileRespository,
            IOptions<ConfigOptions> config
            )
        {
            _scoreRepository = scoreRepository;
            _profileRespository = profileRespository;
            _config = config.Value;
        }

        public async Task<IActionResult> Index(
            int page = 1, 
            int pageSize = 10, 
            string mode = "",
            string region = ""
            )
        {
            /*string value;

            // Check whether the environment variable exists.
            value = Environment.GetEnvironmentVariable("ConfigOptions__EnvVar", EnvironmentVariableTarget.Machine);
            // If necessary, create it.
            if (value == null)
            {
                Environment.SetEnvironmentVariable("ConfigOptions__EnvVar", "No env var with that name");
                // Now retrieve it.
                value = Environment.GetEnvironmentVariable("ConfigOptions__EnvVar");
            }
            _config.EnvVar = value;*/

            // Create the view model with initial values we already know.
            var vm = new LeaderboardViewModel
            {
                Config = _config,
                Page = page,
                PageSize = pageSize,
                SelectedMode = mode,
                SelectedRegion = region,

                GameModes = new List<string>()
                {
                    "Solo",
                    "Duo",
                    "Trio"
                },

                    GameRegions = new List<string>()
                {
                    "Milky Way",
                    "Andromeda",
                    "Pinwheel",
                    "NGC 1300",
                    "Messier 82",
                }
            };

            try
            {
                // Form the query predicate.
                // This expression selects all scores that match the provided game 
                // mode and region (map).
                // Select the score if the game mode or region is empty.
                Expression<Func<Score, bool>> queryPredicate = score =>
                    (string.IsNullOrEmpty(mode) || score.GameMode == mode) &&
                    (string.IsNullOrEmpty(region) || score.GameRegion == region);

                // Fetch the total number of results in the background.
                var countItemsTask = _scoreRepository.CountItemsAsync(queryPredicate);

                // Fetch the scores that match the current filter.
                IEnumerable<Score> scores = await _scoreRepository.GetItemsAsync(
                    queryPredicate, // the predicate defined above
                    score => score.HighScore, // sort descending by high score
                    page - 1, // subtract 1 to make the query 0-based
                    pageSize
                  );

                // Wait for the total count.
                vm.TotalResults = await countItemsTask;

                // Set previous and next hyperlinks.
                if (page > 1)
                {
                    vm.PrevLink = $"/?page={page - 1}&pageSize={pageSize}&mode={mode}&region={region}#leaderboard";
                }
                if (vm.TotalResults > page * pageSize)
                {
                    vm.NextLink = $"/?page={page + 1}&pageSize={pageSize}&mode={mode}&region={region}#leaderboard";
                }

                // Fetch the user profile for each score.
                // This creates a list that's parallel with the scores collection.
                var profiles = new List<Task<Profile>>();
                foreach (var score in scores)
                {
                    profiles.Add(_profileRespository.GetItemAsync(score.ProfileId));
                }
                Task<Profile>.WaitAll(profiles.ToArray());

                // Combine each score with its profile.
                vm.Scores = scores.Zip(profiles, (score, profile) => new ScoreProfile { Score = score, Profile = profile.Result });

                return View(vm);
            }
            catch (Exception ex)
            {
                return View(vm);
            }
        }

        [Route("/profile/{id}")]
        public async Task<IActionResult> Profile(string id, string rank="")
        {
            try
            {
                // Fetch the user profile with the given identifier.
                return View(new ProfileViewModel { Profile = await _profileRespository.GetItemAsync(id), Rank = rank });
            }
            catch (Exception ex)
            {
                return RedirectToAction("/");
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
