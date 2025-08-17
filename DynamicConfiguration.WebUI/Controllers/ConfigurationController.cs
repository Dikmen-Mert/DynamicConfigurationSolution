using Microsoft.AspNetCore.Mvc;
using DynamicConfiguration.Core.Interfaces;
using DynamicConfiguration.Core.Models;
using DynamicConfiguration.Core.Repositories;
using DynamicConfiguration.WebUI.Models;

namespace DynamicConfiguration.WebUI.Controllers
{
    public class ConfigurationController : Controller
    {
        private readonly IConfigurationRepository _repository;
        private readonly string _connectionString = "mongodb://admin:password123@localhost:27017";

        public ConfigurationController()
        {
            _repository = new MongoConfigurationRepository(_connectionString);
        }

        // GET: Configuration
        public async Task<IActionResult> Index(string? filterName, string? filterApplication)
        {
            var allConfigs = await _repository.GetAllActiveConfigurationsAsync();

            // Filtreleme
            if (!string.IsNullOrWhiteSpace(filterName))
            {
                allConfigs = allConfigs.Where(x => x.Name.Contains(filterName, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filterApplication))
            {
                allConfigs = allConfigs.Where(x => x.ApplicationName.Equals(filterApplication, StringComparison.OrdinalIgnoreCase));
            }

            var viewModel = new ConfigurationListViewModel
            {
                Configurations = allConfigs.Select(MapToViewModel).ToList(),
                FilterName = filterName,
                FilterApplication = filterApplication,
                Applications = (await _repository.GetAllActiveConfigurationsAsync())
                    .Select(x => x.ApplicationName)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList()
            };

            return View(viewModel);
        }

        // GET: Configuration/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var config = await _repository.GetByIdAsync(id);
            if (config == null)
                return NotFound();

            return View(MapToViewModel(config));
        }

        // GET: Configuration/Create
        public IActionResult Create()
        {
            var viewModel = new ConfigurationViewModel();
            ViewBag.Types = ConfigurationTypes.Types;
            return View(viewModel);
        }

        // POST: Configuration/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ConfigurationViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Aynı isimde ve uygulamada kayıt var mı kontrol et
                var existing = await _repository.GetByNameAndApplicationAsync(viewModel.Name, viewModel.ApplicationName);
                if (existing != null)
                {
                    ModelState.AddModelError("Name", "Bu isimde bir konfigürasyon bu uygulama için zaten mevcut.");
                    ViewBag.Types = ConfigurationTypes.Types;
                    return View(viewModel);
                }

                var configItem = MapToConfigurationItem(viewModel);
                var result = await _repository.InsertAsync(configItem);

                if (result)
                {
                    TempData["SuccessMessage"] = "Konfigürasyon başarıyla eklendi.";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", "Konfigürasyon eklenirken bir hata oluştu.");
            }

            ViewBag.Types = ConfigurationTypes.Types;
            return View(viewModel);
        }

        // GET: Configuration/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var config = await _repository.GetByIdAsync(id);
            if (config == null)
                return NotFound();

            ViewBag.Types = ConfigurationTypes.Types;
            return View(MapToViewModel(config));
        }

        // POST: Configuration/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ConfigurationViewModel viewModel)
        {
            if (id != viewModel.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                // Aynı isimde başka kayıt var mı kontrol et (kendisi hariç)
                var existing = await _repository.GetByNameAndApplicationAsync(viewModel.Name, viewModel.ApplicationName);
                if (existing != null && existing.Id != id)
                {
                    ModelState.AddModelError("Name", "Bu isimde bir konfigürasyon bu uygulama için zaten mevcut.");
                    ViewBag.Types = ConfigurationTypes.Types;
                    return View(viewModel);
                }

                var configItem = MapToConfigurationItem(viewModel);
                var result = await _repository.UpdateAsync(configItem);

                if (result)
                {
                    TempData["SuccessMessage"] = "Konfigürasyon başarıyla güncellendi.";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", "Konfigürasyon güncellenirken bir hata oluştu.");
            }

            ViewBag.Types = ConfigurationTypes.Types;
            return View(viewModel);
        }

        // GET: Configuration/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var config = await _repository.GetByIdAsync(id);
            if (config == null)
                return NotFound();

            return View(MapToViewModel(config));
        }

        // POST: Configuration/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var result = await _repository.DeleteAsync(id);

            if (result)
            {
                TempData["SuccessMessage"] = "Konfigürasyon başarıyla silindi.";
            }
            else
            {
                TempData["ErrorMessage"] = "Konfigürasyon silinirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Index));
        }

        // AJAX endpoint for client-side filtering
        [HttpGet]
        public async Task<IActionResult> GetFilteredConfigurations(string? filterName, string? filterApplication)
        {
            var allConfigs = await _repository.GetAllActiveConfigurationsAsync();

            if (!string.IsNullOrWhiteSpace(filterName))
            {
                allConfigs = allConfigs.Where(x => x.Name.Contains(filterName, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filterApplication))
            {
                allConfigs = allConfigs.Where(x => x.ApplicationName.Equals(filterApplication, StringComparison.OrdinalIgnoreCase));
            }

            var result = allConfigs.Select(MapToViewModel).ToList();
            return Json(result);
        }

        private static ConfigurationViewModel MapToViewModel(ConfigurationItem item)
        {
            return new ConfigurationViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Type = item.Type,
                Value = item.Value,
                IsActive = item.IsActive,
                ApplicationName = item.ApplicationName,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            };
        }

        private static ConfigurationItem MapToConfigurationItem(ConfigurationViewModel viewModel)
        {
            return new ConfigurationItem
            {
                Id = viewModel.Id ?? string.Empty,
                Name = viewModel.Name,
                Type = viewModel.Type,
                Value = viewModel.Value,
                IsActive = viewModel.IsActive,
                ApplicationName = viewModel.ApplicationName,
                CreatedAt = viewModel.CreatedAt == default ? DateTime.UtcNow : viewModel.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}