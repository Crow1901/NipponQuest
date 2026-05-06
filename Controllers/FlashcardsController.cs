using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NipponQuest.Data;
using NipponQuest.Models;
using NipponQuest.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.IO;
using System.Collections.Generic;

namespace NipponQuest.Controllers
{
    public class FlashcardsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        // --- GAMIFIED COLOR ECONOMY ---
        public static readonly List<string> FreeColors = new List<string> { "#ffffff" };

        public static readonly List<string> PremiumColors = new List<string>
        {
            "#f87171", "#fb923c", "#fbbf24", "#a3e635",
            "#4ade80", "#34d399", "#2dd4bf", "#22d3ee", "#38bdf8",
            "#60a5fa", "#818cf8", "#a78bfa", "#c084fc", "#e879f9",
            "#f472b6", "#fb7185", "#94a3b8", "#a8a29e", "#eab308"
        };

        public FlashcardsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // --- DASHBOARD ---
        public async Task<IActionResult> Index(string searchString)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var today = DateTime.UtcNow.Date;
            var reviewedToday = await _context.Flashcards
                .Where(f => f.Deck!.ApplicationUserId == userId && f.LastReviewed >= today)
                .CountAsync();

            ViewBag.ReviewedToday = reviewedToday;

            var query = _context.Decks
                .Where(d => d.ApplicationUserId == userId)
                .Select(d => new DeckViewModel
                {
                    Id = d.Id,
                    Title = d.Title ?? "Untitled Deck",
                    Description = d.Description ?? "",
                    CardCount = d.Flashcards.Count(),
                    DueCount = d.Flashcards.Count(f => f.NextReview <= DateTime.UtcNow),
                    NewCount = d.Flashcards.Count(f => f.SuccessCount == 0),
                    LearningCount = d.Flashcards.Count(f => f.Interval == 1),
                    IsPublic = d.IsPublic,
                    Price = d.Price,
                    IsCommunityClone = d.IsCommunityClone,
                    ThemeColor = d.ThemeColor
                });

            if (!string.IsNullOrEmpty(searchString))
                query = query.Where(s => s.Title.Contains(searchString) || s.Description.Contains(searchString));

            return View(await query.ToListAsync());
        }

        // --- DECK MANAGEMENT (CRUD) ---
        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Deck deck)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            deck.ApplicationUserId = userId;
            deck.IsCommunityClone = false;
            if (string.IsNullOrEmpty(deck.ThemeColor)) deck.ThemeColor = "#ffffff";

            _context.Decks.Add(deck);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var deck = await _context.Decks
                .Include(d => d.Flashcards)
                .FirstOrDefaultAsync(m => m.Id == id && m.ApplicationUserId == userId);

            if (deck == null) return NotFound();

            if (deck.IsCommunityClone || deck.Description.StartsWith("(Community Deck)"))
            {
                TempData["Message"] = "Community decks are read-only and cannot be edited.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _context.Users.OfType<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == userId);
            ViewBag.UserLevel = user?.Level ?? 1;
            ViewBag.UserGold = user?.Gold ?? 0;

            var purchasedColors = await _context.UserColorPurchases
                .Where(c => c.ApplicationUserId == userId)
                .Select(c => c.ColorHex)
                .ToListAsync();

            ViewBag.FreeColors = FreeColors;
            ViewBag.PremiumColors = PremiumColors;
            ViewBag.PurchasedColors = purchasedColors;

            return View(deck);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,ApplicationUserId,IsPublic,Price,ThemeColor")] Deck inputDeck)
        {
            if (id != inputDeck.Id) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (inputDeck.ApplicationUserId != userId) return Forbid();

            var dbDeck = await _context.Decks.FindAsync(id);
            if (dbDeck == null) return NotFound();

            if (dbDeck.IsCommunityClone || dbDeck.Description.StartsWith("(Community Deck)"))
            {
                return BadRequest("Action not allowed on Community Decks.");
            }

            var appUser = await _context.Users.OfType<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == userId);

            if (inputDeck.IsPublic && (appUser == null || appUser.Level < 5))
            {
                inputDeck.IsPublic = false;
            }

            dbDeck.AuthorName = User.Identity?.Name ?? "Anonymous Questor";

            dbDeck.Title = inputDeck.Title;
            dbDeck.Description = inputDeck.Description;
            dbDeck.IsPublic = inputDeck.IsPublic;
            dbDeck.Price = inputDeck.Price;
            dbDeck.ThemeColor = string.IsNullOrEmpty(inputDeck.ThemeColor) ? "#ffffff" : inputDeck.ThemeColor;

            try
            {
                _context.Update(dbDeck);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Decks.Any(e => e.Id == inputDeck.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // --- MARKET PURCHASES FOR COLORS ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PurchaseColor(string colorHex, int deckId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.OfType<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return Unauthorized();

            int colorPrice = 50;

            if (user.Gold >= colorPrice)
            {
                var alreadyOwns = await _context.UserColorPurchases.AnyAsync(c => c.ApplicationUserId == userId && c.ColorHex == colorHex);
                if (!alreadyOwns)
                {
                    user.Gold -= colorPrice;
                    _context.UserColorPurchases.Add(new UserColorPurchase
                    {
                        ApplicationUserId = userId,
                        ColorHex = colorHex
                    });
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "New deck theme color unlocked!";
                }
            }
            else
            {
                TempData["Error"] = "Not enough Gold to purchase this color.";
            }

            return RedirectToAction(nameof(Edit), new { id = deckId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PurchaseAllColors(int deckId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.OfType<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return Unauthorized();

            int bundlePrice = 500;

            if (user.Gold >= bundlePrice)
            {
                var existingPurchases = await _context.UserColorPurchases
                    .Where(c => c.ApplicationUserId == userId)
                    .Select(c => c.ColorHex)
                    .ToListAsync();

                var colorsToAdd = PremiumColors.Except(existingPurchases).ToList();

                if (colorsToAdd.Any())
                {
                    user.Gold -= bundlePrice;
                    foreach (var hex in colorsToAdd)
                    {
                        _context.UserColorPurchases.Add(new UserColorPurchase
                        {
                            ApplicationUserId = userId,
                            ColorHex = hex
                        });
                    }
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Master Painter Bundle Unlocked!";
                }
            }
            else
            {
                TempData["Error"] = "Not enough Gold to purchase the bundle.";
            }

            return RedirectToAction(nameof(Edit), new { id = deckId });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var deck = await _context.Decks
                .FirstOrDefaultAsync(m => m.Id == id && m.ApplicationUserId == userId);

            if (deck == null) return NotFound();
            return View(deck);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var deck = await _context.Decks.FindAsync(id);

            if (deck != null && deck.ApplicationUserId == userId)
            {
                _context.Decks.Remove(deck);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // --- INDIVIDUAL CARD ACTIONS ---
        [HttpGet]
        public IActionResult AddCard(int deckId)
        {
            return View(new Flashcard { DeckId = deckId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCard(Flashcard card, IFormFile imageFile, IFormFile audioFile)
        {
            var deck = await _context.Decks.FindAsync(card.DeckId);
            if (deck != null && (deck.IsCommunityClone || deck.Description.StartsWith("(Community Deck)")))
                return BadRequest("Cannot add cards to a Community Deck.");

            card.NextReview = DateTime.UtcNow;
            card.EaseFactor = 2.5;
            card.Interval = 0;
            card.SuccessCount = 0;

            var permanentMediaDir = Path.Combine(_environment.WebRootPath, "uploads", "media");
            if (!Directory.Exists(permanentMediaDir)) Directory.CreateDirectory(permanentMediaDir);

            if (imageFile != null && imageFile.Length > 0)
            {
                var filename = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                using (var stream = new FileStream(Path.Combine(permanentMediaDir, filename), FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                card.ImageFilePath = filename;
            }

            if (audioFile != null && audioFile.Length > 0)
            {
                var filename = Guid.NewGuid().ToString() + Path.GetExtension(audioFile.FileName);
                using (var stream = new FileStream(Path.Combine(permanentMediaDir, filename), FileMode.Create))
                {
                    await audioFile.CopyToAsync(stream);
                }
                card.AudioFilePath = filename;
            }

            _context.Flashcards.Add(card);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Edit), new { id = card.DeckId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCard(Flashcard card, IFormFile imageFile, IFormFile audioFile)
        {
            var existingCard = await _context.Flashcards.Include(f => f.Deck).FirstOrDefaultAsync(f => f.Id == card.Id);
            if (existingCard == null) return NotFound();

            if (existingCard.Deck != null && (existingCard.Deck.IsCommunityClone || existingCard.Deck.Description.StartsWith("(Community Deck)")))
                return BadRequest("Cannot edit cards in a Community Deck.");

            existingCard.FrontText = card.FrontText;
            existingCard.BackText = card.BackText;

            var mediaDir = Path.Combine(_environment.WebRootPath, "uploads", "media");
            if (!Directory.Exists(mediaDir)) Directory.CreateDirectory(mediaDir);

            if (imageFile != null && imageFile.Length > 0)
            {
                var filename = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                using (var stream = new FileStream(Path.Combine(mediaDir, filename), FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                existingCard.ImageFilePath = filename;
            }

            if (audioFile != null && audioFile.Length > 0)
            {
                var filename = Guid.NewGuid().ToString() + Path.GetExtension(audioFile.FileName);
                using (var stream = new FileStream(Path.Combine(mediaDir, filename), FileMode.Create))
                {
                    await audioFile.CopyToAsync(stream);
                }
                existingCard.AudioFilePath = filename;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Edit), new { id = card.DeckId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCard(int cardId, int deckId)
        {
            var card = await _context.Flashcards.Include(f => f.Deck).FirstOrDefaultAsync(f => f.Id == cardId);
            if (card != null)
            {
                if (card.Deck != null && (card.Deck.IsCommunityClone || card.Deck.Description.StartsWith("(Community Deck)")))
                    return BadRequest("Cannot delete cards from a Community Deck.");

                _context.Flashcards.Remove(card);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Edit), new { id = deckId });
        }

        // --- ANKI IMPORT ---
        [HttpPost]
        [RequestSizeLimit(262144000)]
        public async Task<IActionResult> ImportAnki(IFormFile ankiFile)
        {
            if (ankiFile == null || ankiFile.Length == 0) return BadRequest("No file uploaded.");

            var tempDirName = "temp_anki_" + Guid.NewGuid();
            var tempDirPath = Path.Combine(_environment.WebRootPath, "uploads", tempDirName);
            Directory.CreateDirectory(tempDirPath);

            var packagePath = Path.Combine(tempDirPath, ankiFile.FileName);

            using (var stream = new FileStream(packagePath, FileMode.Create))
            {
                await ankiFile.CopyToAsync(stream);
                await stream.FlushAsync();
            }

            try
            {
                System.Threading.Thread.Sleep(1200);
                var cards = AnkiProcessor.GetCardsFromPackage(packagePath, tempDirPath, 0);
                ViewBag.TempFolder = tempDirName;
                ViewBag.DeckName = Path.GetFileNameWithoutExtension(ankiFile.FileName);
                return View("ConfirmImport", cards);
            }
            catch (Exception ex)
            {
                if (Directory.Exists(tempDirPath)) Directory.Delete(tempDirPath, true);
                return StatusCode(500, $"Import failed: {ex.Message}");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(ValueCountLimit = 30000)]
        public async Task<IActionResult> FinalizeImport([FromForm] string deckTitle, [FromForm] List<Flashcard> cards, [FromForm] string tempFolder)
        {
            if (string.IsNullOrEmpty(tempFolder)) return BadRequest("Context lost.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var newDeck = new Deck
            {
                Title = string.IsNullOrWhiteSpace(deckTitle) ? "Imported Deck" : deckTitle,
                ApplicationUserId = userId,
                Description = "Anki Import",
                ThemeColor = "#ffffff"
            };

            _context.Decks.Add(newDeck);
            await _context.SaveChangesAsync();

            var sourceMediaDir = Path.Combine(_environment.WebRootPath, "uploads", tempFolder, "media_files");
            var destMediaDir = Path.Combine(_environment.WebRootPath, "uploads", "media");
            if (!Directory.Exists(destMediaDir)) Directory.CreateDirectory(destMediaDir);

            if (Directory.Exists(sourceMediaDir))
            {
                foreach (var card in cards)
                {
                    card.DeckId = newDeck.Id;
                    if (!string.IsNullOrEmpty(card.ImageFilePath))
                    {
                        var sPath = Path.Combine(sourceMediaDir, card.ImageFilePath);
                        if (System.IO.File.Exists(sPath))
                        {
                            var targetPath = Path.Combine(destMediaDir, card.ImageFilePath);
                            System.IO.File.Copy(sPath, targetPath, true);
                        }
                    }
                    if (!string.IsNullOrEmpty(card.AudioFilePath))
                    {
                        var sPath = Path.Combine(sourceMediaDir, card.AudioFilePath);
                        if (System.IO.File.Exists(sPath))
                        {
                            var targetPath = Path.Combine(destMediaDir, card.AudioFilePath);
                            System.IO.File.Copy(sPath, targetPath, true);
                        }
                    }
                }
            }

            foreach (var card in cards)
            {
                card.FrontText = card.FrontText ?? "";
                card.BackText = card.BackText ?? "";
            }

            _context.Flashcards.AddRange(cards);
            await _context.SaveChangesAsync();

            var fullTempPath = Path.Combine(_environment.WebRootPath, "uploads", tempFolder);
            if (Directory.Exists(fullTempPath))
            {
                System.Threading.Thread.Sleep(500);
                Directory.Delete(fullTempPath, true);
            }

            return RedirectToAction(nameof(Index));
        }

        // --- GLOBAL STUDY LOGIC ---
        [HttpGet]
        public async Task<IActionResult> StudyAllDue()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var now = DateTime.UtcNow;
            var cardIds = await _context.Flashcards
                .Where(f => f.Deck!.ApplicationUserId == userId && f.NextReview <= now)
                .OrderBy(r => Guid.NewGuid())
                .Select(f => f.Id)
                .ToListAsync();

            if (!cardIds.Any()) return RedirectToAction(nameof(Index));

            TempData["SessionCards"] = string.Join(",", cardIds);
            TempData["CurrentIndex"] = 0;
            TempData["TotalInSession"] = cardIds.Count;

            return RedirectToAction("StudyNext", new { deckId = 0 });
        }

        // --- QUEST SYSTEM LOGIC ---
        [HttpGet]
        public async Task<IActionResult> StartSession(int id, int length = 10)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cardIds = await _context.Flashcards
                .Where(f => f.DeckId == id && f.Deck!.ApplicationUserId == userId)
                .OrderBy(r => Guid.NewGuid())
                .Take(length)
                .Select(f => f.Id)
                .ToListAsync();

            if (!cardIds.Any()) return RedirectToAction(nameof(Index));

            TempData["SessionCards"] = string.Join(",", cardIds);
            TempData["CurrentIndex"] = 0;
            TempData["TotalInSession"] = cardIds.Count;

            return RedirectToAction("StudyNext", new { deckId = id });
        }

        [HttpGet]
        public async Task<IActionResult> StudyNext(int deckId)
        {
            var cardIdsStr = TempData["SessionCards"]?.ToString();
            if (string.IsNullOrEmpty(cardIdsStr)) return RedirectToAction(nameof(Index));

            var cardIds = cardIdsStr.Split(',').Select(int.Parse).ToList();
            int currentIndex = (int)(TempData["CurrentIndex"] ?? 0);

            if (currentIndex >= cardIds.Count) return CompleteSession(cardIds.Count);

            var card = await _context.Flashcards.Include(f => f.Deck)
                .FirstOrDefaultAsync(f => f.Id == cardIds[currentIndex]);

            if (card == null) return RedirectToAction(nameof(Index));

            TempData.Keep();
            ViewBag.CurrentIndex = currentIndex + 1;
            ViewBag.TotalInSession = cardIds.Count;

            return View("Study", card);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitResult(int cardId, int quality)
        {
            var card = await _context.Flashcards.FindAsync(cardId);
            if (card == null) return NotFound();

            if (quality >= 3)
            {
                if (card.Interval == 0) card.Interval = 1;
                else if (card.Interval == 1) card.Interval = 6;
                else card.Interval = (int)Math.Round(card.Interval * card.EaseFactor);

                card.EaseFactor += (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02));
                card.SuccessCount++;
            }
            else { card.Interval = 1; }

            card.LastReviewed = DateTime.UtcNow;
            card.NextReview = DateTime.UtcNow.AddDays(card.Interval);
            await _context.SaveChangesAsync();

            int currentIndex = (int)(TempData["CurrentIndex"] ?? 0);
            TempData["CurrentIndex"] = currentIndex + 1;
            TempData.Keep();

            return RedirectToAction("StudyNext", new { deckId = card.DeckId });
        }

        private IActionResult CompleteSession(int count)
        {
            ViewBag.TotalCards = count;
            ViewBag.ExpReward = count;
            ViewBag.GoldReward = 5;
            return View("QuestComplete");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClaimRewards(int expReward, int goldReward)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.OfType<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == userId);

            if (user != null)
            {
                user.CurrentXP += expReward;
                user.TotalEXP += expReward;
                user.WeeklyXP += expReward;
                user.Gold += goldReward;
                user.LessonsCompleted += 1;

                while (user.CurrentXP >= user.RequiredXP)
                {
                    user.CurrentXP -= user.RequiredXP;
                    user.Level++;
                }

                // ── REWARD LEDGER ──
                _context.RewardLedgers.Add(new RewardLedger
                {
                    ApplicationUserId = userId,
                    ExpDelta = expReward,
                    GoldDelta = goldReward,
                    Source = "flashcards"
                });

                await _context.SaveChangesAsync();
                TempData["Message"] = $"Quest Complete! +{expReward} EXP and +{goldReward} Gold earned.";
            }

            return RedirectToAction(nameof(Index));
        }

        // --- DISCOVER & COMMUNITY LOGIC ---
        [HttpGet]
        public async Task<IActionResult> Discover(string searchString, string sortOrder)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.CurrentUserId = userId;

            var appUser = await _context.Users.OfType<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == userId);
            ViewBag.UserGold = appUser?.Gold ?? 0;

            ViewBag.ActiveParentDeckIds = string.IsNullOrEmpty(userId) ? new List<int>() :
                await _context.Decks
                    .Where(d => d.ApplicationUserId == userId && d.ParentDeckId != null)
                    .Select(d => d.ParentDeckId.Value)
                    .ToListAsync();

            ViewBag.PurchasedDeckIds = string.IsNullOrEmpty(userId) ? new List<int>() :
                await _context.DeckPurchases
                    .Where(p => p.ApplicationUserId == userId)
                    .Select(p => p.DeckId)
                    .ToListAsync();

            var query = _context.Decks
                .Include(d => d.Flashcards)
                .Where(d => d.IsPublic);

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(s => s.Title.Contains(searchString) || s.Description.Contains(searchString));
            }

            ViewBag.CurrentSort = sortOrder;
            switch (sortOrder)
            {
                case "likes":
                    query = query.OrderByDescending(d => d.Likes);
                    break;
                case "downloads":
                default:
                    query = query.OrderByDescending(d => d.Downloads);
                    break;
            }

            var discoverDecks = await query.ToListAsync();
            ViewBag.SearchString = searchString;
            return View(discoverDecks);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DownloadDeck(int originalDeckId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var appUser = await _context.Users.OfType<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == userId);
            if (appUser == null) return Unauthorized();

            var originalDeck = await _context.Decks
                .Include(d => d.Flashcards)
                .FirstOrDefaultAsync(d => d.Id == originalDeckId && d.IsPublic);

            if (originalDeck == null) return NotFound();

            var isActive = await _context.Decks.AnyAsync(d =>
                d.ApplicationUserId == userId && d.ParentDeckId == originalDeck.Id);

            if (isActive) return BadRequest("You already have this deck active in your dashboard.");

            var isAlreadyOwned = await _context.DeckPurchases.AnyAsync(p => p.ApplicationUserId == userId && p.DeckId == originalDeck.Id);

            if (!isAlreadyOwned)
            {
                if (originalDeck.Price > 0)
                {
                    if (appUser.Gold < originalDeck.Price)
                    {
                        TempData["Error"] = $"Not enough Gold! You need {originalDeck.Price} G to purchase this deck.";
                        return RedirectToAction(nameof(Discover));
                    }

                    appUser.Gold -= originalDeck.Price;

                    int tax = 0;
                    if (originalDeck.Price <= 50) tax = Math.Max(1, (int)(originalDeck.Price * 0.05));
                    else if (originalDeck.Price <= 250) tax = (int)(originalDeck.Price * 0.10);
                    else tax = (int)(originalDeck.Price * 0.20);

                    int profit = originalDeck.Price - tax;

                    var creator = await _context.Users.OfType<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == originalDeck.ApplicationUserId);
                    if (creator != null)
                    {
                        creator.Gold += profit;
                    }
                }

                _context.DeckPurchases.Add(new DeckPurchase
                {
                    ApplicationUserId = userId,
                    DeckId = originalDeck.Id
                });
            }

            originalDeck.Downloads += 1;

            var newDeck = new Deck
            {
                Title = originalDeck.Title,
                Description = "(Community Deck) " + (originalDeck.Description ?? ""),
                ApplicationUserId = userId,
                IsPublic = false,
                IsCommunityClone = true,
                ParentDeckId = originalDeck.Id,
                Price = 0,
                ThemeColor = originalDeck.ThemeColor
            };

            _context.Decks.Add(newDeck);
            await _context.SaveChangesAsync();

            var newCards = originalDeck.Flashcards.Select(card => new Flashcard
            {
                DeckId = newDeck.Id,
                FrontText = card.FrontText,
                BackText = card.BackText,
                ImageFilePath = card.ImageFilePath,
                AudioFilePath = card.AudioFilePath,
                Interval = 0,
                EaseFactor = 2.5,
                NextReview = DateTime.UtcNow
            }).ToList();

            _context.Flashcards.AddRange(newCards);
            await _context.SaveChangesAsync();

            if (isAlreadyOwned) TempData["Message"] = "Deck successfully restored to your dashboard!";
            else TempData["Message"] = $"Deck successfully purchased for {originalDeck.Price} G!";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VoteDeck(int deckId, bool isUpvote)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var deck = await _context.Decks.FindAsync(deckId);
            if (deck == null || !deck.IsPublic) return NotFound();

            var existingVote = await _context.DeckVotes
                .FirstOrDefaultAsync(v => v.DeckId == deckId && v.ApplicationUserId == userId);

            if (existingVote != null)
            {
                if (existingVote.IsUpvote == isUpvote)
                {
                    if (isUpvote) deck.Likes--; else deck.Dislikes--;
                    _context.DeckVotes.Remove(existingVote);
                }
                else
                {
                    if (isUpvote) { deck.Likes++; deck.Dislikes--; }
                    else { deck.Likes--; deck.Dislikes++; }
                    existingVote.IsUpvote = isUpvote;
                }
            }
            else
            {
                if (isUpvote) deck.Likes++; else deck.Dislikes++;
                _context.DeckVotes.Add(new DeckVote
                {
                    DeckId = deckId,
                    ApplicationUserId = userId,
                    IsUpvote = isUpvote
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Discover));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisibility(int deckId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var deck = await _context.Decks.FirstOrDefaultAsync(d => d.Id == deckId && d.ApplicationUserId == userId);

            if (deck != null)
            {
                if (deck.IsCommunityClone || deck.Description.StartsWith("(Community Deck)"))
                    return BadRequest("Community decks cannot be published.");

                deck.IsPublic = !deck.IsPublic;
                await _context.SaveChangesAsync();
                TempData["Message"] = deck.IsPublic ? "Deck published to Community Hub!" : "Deck removed from Community Hub.";
            }

            return Redirect(Request.Headers["Referer"].ToString());
        }
    }
}